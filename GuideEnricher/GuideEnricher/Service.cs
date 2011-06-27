namespace GuideEnricher
{
    using System;
    using System.Reflection;
    using System.ServiceModel;
    using System.Threading;
    using System.Timers;
    using ForTheRecord.Entities;
    using ForTheRecord.ServiceAgents;
    using GuideEnricher.Config;
    using GuideEnricher.tvdb;
    using log4net;
    using Timer = System.Timers.Timer;

    public class Service
    {
        private const string MODULE = "GuideEnricher";
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly IConfiguration config = Config.Config.GetInstance();
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
                enrichTimer = new Timer(TimeSpan.FromHours(waitTime).TotalMilliseconds) { AutoReset = false };
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

        private void SetupFTRConnection(Object state, ElapsedEventArgs eventArgs)
        {
            try
            {
                if (ServiceChannelFactories.IsInitialized)
                {
                    using (var agent = new ForTheRecordServiceAgent())
                    {
                        if (agent.Ping(0) > 0)
                        {
                            log.Debug("Ping");
                        }
                    }

                    return;
                }

                log.Debug("Trying to connect to FTR");
                ServerSettings serverSettings = this.GetServerSettings();
                if (ServiceChannelFactories.Initialize(serverSettings, true))
                {
                    ftrlogAgent = new LogServiceAgent();
                    ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, "GuideEnricher successfully connected");
                    log.Info("Successfully connected to ForTheRecordService");

                    using (var agent = new ForTheRecordServiceAgent())
                    {
                        ForTheRecordEventGroup eventGroupsToListenTo = ForTheRecordEventGroup.ScheduleEvents | ForTheRecordEventGroup.GuideEvents;
                        agent.EnsureEventListener(eventGroupsToListenTo, config.getProperty("serviceUrl"), Constants.EventListenerApiVersion);
                    }
                }
                else
                {
                    log.Fatal("Unable to connect to ForTheRecordService, check your settings.  Will try again later");
                }
            }
            catch(ForTheRecordNotFoundException notFoundException)
            {
                log.Error(notFoundException.Message);
            }
            catch(EndpointNotFoundException)
            {
                log.Error("Connection to FTR lost, make sure the FTR service is running");
            }
            catch(ForTheRecordException ftrException)
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

                using (var agent = new ForTheRecordServiceAgent())
                {
                    log.DebugFormat("Ping {0}", agent.Ping(0));
                }

                using (var tvGuideServiceAgent = new TvGuideServiceAgent())
                {
                    
                    using (var tvSchedulerServiceAgent = new TvSchedulerServiceAgent())
                    {
                        var matchMethods = EpisodeMatchMethodLoader.GetMatchMethods();
                        using (var tvdbLibAccess = new TvdbLibAccess(config, matchMethods))
                        {
                            var enricher = new Enricher(config, ftrlogAgent, tvGuideServiceAgent, tvSchedulerServiceAgent, tvdbLibAccess, matchMethods);
                            enricher.EnrichUpcomingPrograms();
                        }
                    }
                }
            }
            catch (Exception)
            {
                log.Error("Error enriching");
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

        private ServerSettings GetServerSettings()
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