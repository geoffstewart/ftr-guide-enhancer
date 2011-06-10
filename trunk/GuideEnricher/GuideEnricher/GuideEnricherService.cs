/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 28/09/2010
 * Time: 9:12 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace GuideEnricher
{
    using System;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceProcess;
    using System.Threading;
    using ForTheRecord.Entities;
    using ForTheRecord.ServiceAgents;
    using GuideEnricher.Config;
    using log4net;

    /**
     * Class that defines the service start/stop behaviours
     */
    public class GuideEnricherService : ServiceBase
    {
        public const string MyServiceName = "GuideEnricher";
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IConfiguration config;
        private LogServiceAgent ftrlogAgent;
        private const string MODULE = "GuideEnricher";

        public static EventWaitHandle waitHandle = new AutoResetEvent(false);

        private Thread worker;
        private Thread sleeper;
        private bool workerLoop = true;

        public GuideEnricherService(IConfiguration configuration)
        {
            this.config = configuration;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.ServiceName = MyServiceName;
            worker = new Thread(this.EnrichGuideDataJob);
            sleeper = new Thread(enrichTimer);
        }

        // Added this to allow us to debug from Main method (no need to run the service to debug)
        public void Start()
        {
            this.OnStart(null);    
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                this.CreateWebService();
                var connector = new Thread(SetupFTRConnection);
                connector.Start();
            }
            catch (Exception ex)
            {
                log.Fatal("Error on starting service", ex);
                throw;
            }
        }

        private void CreateWebService()
        {
            ServiceHost serviceHost = ForTheRecordListener.CreateServiceHost(this.config.getProperty("serviceUrl"));

            try
            {
                serviceHost.Open();
            }
            catch (System.ServiceProcess.TimeoutException ex)
            {
                log.Fatal("Timeout on creating the ServiceHost", ex);
                throw;
            }
            catch (Exception ex)
            {
                log.Fatal("Error on creating ServiceHost", ex);
                throw;
            }
        }

        /// <summary>
        /// Stop this service.
        /// </summary>
        protected override void OnStop()
        {
            log.Debug("Ending the worker thread...");
            workerLoop = false;
            waitHandle.Set();
            sleeper.Abort();
            log.Info("Stopping the GuideEnricher");
        }

        /**
         * Main worker thread method
         * 
         * Loop that will find all upcoming recordings, enrich data, update in database
         * if the episode information changed
         */

        public void SetupFTRConnection()
        {
            var serverSettings = new ServerSettings();
            serverSettings.ServerName = config.getProperty("ftrUrlHost");
            serverSettings.Transport = ServiceTransport.NetTcp;
            serverSettings.Port = Convert.ToInt32(config.getProperty("ftrUrlPort"));
            var password = config.getProperty("ftrUrlPassword");

            if(!string.IsNullOrEmpty(password))
            {
                serverSettings.Password = password;
            }

            bool connected = false;

            while (!connected)
            {
                connected = ServiceChannelFactories.Initialize(serverSettings, false);
                if(!connected)
                {
                    log.Fatal("Unable to connect to ForTheRecordService, check your settings.  Sleeping for 1 minute");
                    Thread.Sleep(60000);
                }
            }
            
            // We need to connect to FTR before getting log agent especially when it's not on localhost
            ftrlogAgent = new LogServiceAgent();
            ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, "GuideEnricher successfully connected");
            log.Info("Connected to ForTheRecordService");

            using(var agent = new ForTheRecordServiceAgent())
            {
                ForTheRecordEventGroup eventGroupsToListenTo = ForTheRecordEventGroup.ScheduleEvents | ForTheRecordEventGroup.GuideEvents;
                agent.EnsureEventListener(eventGroupsToListenTo, this.config.getProperty("serviceUrl"), Constants.EventListenerApiVersion);
            }

            log.Debug("Starting GuideEnricher...");

            // start worker threads
            worker.Start();
            sleeper.Start();

            // Call the enrich once when we first start
            this.CallEnrich();            
        }

        public void EnrichGuideDataJob()
        {
            try
            {
                while (workerLoop)
                {

                    log.Debug("Thread waiting for events...");

                    // wait until the listener thread signals us to update the guide data
                    waitHandle.WaitOne();
                    if (!workerLoop)
                    {
                        break;
                    }

                    CallEnrich();
                    
                    log.InfoFormat("{0}: Done enriching guide data", MODULE);
                }
            }
            catch (Exception topEx)
            {
                log.Error("The main loop for GuideEnricher received an exception", topEx);
            }
                    
        }

        private void CallEnrich()
        {
            using (var tvGuideServiceAgent = new TvGuideServiceAgent())
            {
                using (var tvSchedulerServiceAgent = new TvSchedulerServiceAgent() )
                {
                    var enricher = new Enricher(this.config, this.ftrlogAgent, tvGuideServiceAgent, tvSchedulerServiceAgent);
                    enricher.EnrichUpcomingPrograms();
                }
            }
        }

        public void enrichTimer()
        {
            string waittime = this.config.getProperty("sleepTimeInHours");
            if (waittime == null)
            {
                waittime = "12";
            }
            if ("0".Equals(waittime))
            {
                // sleeper thread is disabled
                ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, "Sleeper thread is disabled");
                return;
            }
            int waittimeHoursInt = Convert.ToInt32(waittime);
            int waittimeInt = waittimeHoursInt * 60 * 60 * 1000;

            while (workerLoop)
            {
                ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, "The Guide Enricher thread will pause for " + waittime + " hours");
                Thread.Sleep(waittimeInt);
                ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, "Wait thread is awake and calling EnrichGuideDataJob...");
                waitHandle.Set();
            }
        }
    }
}
