namespace GuideEnricher
{
    using System;
    using System.Reflection;
    using System.ServiceModel;
    using System.Timers;
    using ArgusTV.DataContracts;
    using ArgusTV.ServiceAgents;
    using GuideEnricher.Config;
    using GuideEnricher.tvdb;
    using log4net;
    using Timer = System.Timers.Timer;

    public class Service
    {
        private const string MODULE = "GuideEnricher";

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly IConfiguration config = Config.Config.Instance;
        private static LogServiceAgent ftrlogAgent;
        private static Timer ftrConnectionTimer;
        private static Timer enrichTimer;
        public static bool BusyEnriching;
        private static object lockThis = new object();
        private static int waitTime;

        public void Start()
        {
            try
            {
                log.Info("Starting");
                ForTheRecordListener.CreateServiceHost(config.getProperty("serviceUrl")).Open();
                ftrConnectionTimer = new Timer(500) { AutoReset = false };
                ftrConnectionTimer.Elapsed += this.SetupFTRConnection;
                ftrConnectionTimer.Start();

                if (!int.TryParse(config.getProperty("sleepTimeInHours"), out waitTime))
                {
                    waitTime = 12;
                }
                enrichTimer = new Timer(TimeSpan.FromSeconds(15).TotalMilliseconds) { AutoReset = false };
                enrichTimer.Elapsed += Enrich;
                enrichTimer.Start();
            }
            catch (Exception ex)
            {
                log.Fatal("Error on starting service", ex);
                throw;
            }
        }

        public void Stop()
        {
            log.Info("Service stopping");
        }

        public void SetupFTRConnection(Object state, ElapsedEventArgs eventArgs)
        {
            try
            {
                if (ServiceChannelFactories.IsInitialized)
                {
                    using (var agent = new CoreServiceAgent())
                    {
                        if (agent.Ping(0) > 0)
                        {
                            log.Debug("Ping");
                        }
                    }

                    return;
                }

                log.Debug("Trying to connect to Argus TV");
                ServerSettings serverSettings = GetServerSettings();
                if (ServiceChannelFactories.Initialize(serverSettings, true))
                {
                    ftrlogAgent = new LogServiceAgent();
                    ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, "GuideEnricher successfully connected");
                    log.Info("Successfully connected to Argus TV");

                    using (var agent = new CoreServiceAgent())
                    {
                        EventGroup eventGroupsToListenTo = EventGroup.ScheduleEvents | EventGroup.GuideEvents;
                        agent.EnsureEventListener(eventGroupsToListenTo, config.getProperty("serviceUrl"), Constants.EventListenerApiVersion);
                    }
                }
                else
                {
                    log.Fatal("Unable to connect to Argus TV, check your settings.  Will try again later");
                }
            }
            catch(ArgusTVNotFoundException notFoundException)
            {
                log.Error(notFoundException.Message);
            }
            catch(EndpointNotFoundException)
            {
                log.Error("Connection to Argus TV lost, make sure the Argus TV service is running");
            }
            catch(ArgusTVException ftrException)
            {
                log.Fatal(ftrException.Message);
            }
            finally
            {
                ftrConnectionTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
                ftrConnectionTimer.Start();
            }
        }

        public static void Enrich(Object state, ElapsedEventArgs eventArgs)
        {
            try
            {
                lock (lockThis)
                {
                    BusyEnriching = true;
                }

                using (var agent = new CoreServiceAgent())
                {
                    log.DebugFormat("Ping {0}", agent.Ping(0));
                }

                using (var tvGuideServiceAgent = new GuideServiceAgent())
                {
                    
                    using (var tvSchedulerServiceAgent = new SchedulerServiceAgent())
                    {
                        var matchMethods = EpisodeMatchMethodLoader.GetMatchMethods();
                        var tvDbApi = new TvDbService(config.CacheFolder, config.ApiKey);
                        var tvdbLibAccess = new TvdbLibAccess(config, matchMethods, tvDbApi);
                        var enricher = new Enricher(config, ftrlogAgent, tvGuideServiceAgent, tvSchedulerServiceAgent, tvdbLibAccess, matchMethods);
                        enricher.EnrichUpcomingPrograms();
                    }
                }
            }
            catch (Exception exception)
            {
                log.Error("Error enriching", exception);
            }
            finally
            {
                lock (lockThis)
                {
                    BusyEnriching = false;
                }

                if (!enrichTimer.Enabled)
                {
                    enrichTimer.Interval = TimeSpan.FromHours(waitTime).TotalMilliseconds;
                    enrichTimer.Start();
                }
            }
        }

        public static ServerSettings GetServerSettings()
        {
            var serverSettings = new ServerSettings();
            serverSettings.ServerName = config.getProperty("ftrUrlHost");
            serverSettings.Transport = ServiceTransport.NetTcp;
            serverSettings.Port = Convert.ToInt32(config.getProperty("ftrUrlPort"));
            var password = config.getProperty("ftrUrlPassword");

            if (!string.IsNullOrEmpty(password))
            {
                serverSettings.Password = password;
            }

            return serverSettings;
        }
    }
}