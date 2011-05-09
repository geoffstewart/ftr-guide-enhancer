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
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceProcess;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Exceptions;
    using ForTheRecord.Entities;
    using ForTheRecord.ServiceAgents;
    using log4net;

    /**
     * Class that defines the service start/stop behaviours
     */
    public class GuideEnricherService : ServiceBase
    {
        public const string MyServiceName = "GuideEnricher";
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IConfiguration config;
        private readonly LogServiceAgent ftrlogAgent;
        private const string MODULE = "GuideEnricher";

        public static EventWaitHandle waitHandle = new AutoResetEvent(false);

        private Thread worker;
        private Thread sleeper;
        private bool workerLoop = true;

        private static int[] intMatcher = new int[9];

        private static int intAlreadyEnriched;
        
        public GuideEnricherService(IConfiguration configuration)
        {
            this.config = configuration;
            InitializeComponent();
            ftrlogAgent = new LogServiceAgent();
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
            //         Thread.Sleep(10000);
            try
            {
                IConfiguration config = Config.GetInstance();
                ServerSettings serverSettings = new ServerSettings();
                serverSettings.ServerName = config.getProperty("ftrUrlHost");
                serverSettings.Transport = ServiceTransport.NetTcp;
                serverSettings.Port = Convert.ToInt32(config.getProperty("ftrUrlPort"));
                string pass = config.getProperty("ftrUrlPassword");

                if (pass != null && pass.Length > 0)
                {
                    serverSettings.Password = pass;
                }

                log.DebugFormat("Just about to call ServiceChannelFactories.Initialize()");

                if (!ServiceChannelFactories.Initialize(serverSettings, false))
                {
                    log.Fatal("Unable to connect to ForTheRecordService, check your settings.");
                    throw new Exception("Unable to connect to ForTheRecordService, check your settings.");
                }

                ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, "GuideEnricher successfully connected");
                log.Info("Connected to ForTheRecordService");

                ServiceHost sh = ForTheRecordListener.CreateServiceHost(config.getProperty("serviceUrl"));

                try
                {
                    sh.Open();

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

                // verify it's listening

                using (ForTheRecordServiceAgent agent = new ForTheRecordServiceAgent())
                {
                    ForTheRecordEventGroup eventGroupsToListenTo = ForTheRecordEventGroup.ScheduleEvents | ForTheRecordEventGroup.GuideEvents;
                    agent.EnsureEventListener(eventGroupsToListenTo, this.config.getProperty("serviceUrl"), Constants.EventListenerApiVersion);

                }
            }
            catch (Exception ex)
            {
                log.Fatal("Error on starting service", ex);
                throw;
            }

            log.Debug("Starting GuideEnricher...");

            // start worker threads
            worker.Start();
            sleeper.Start();
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

        public void EnrichGuideDataJob()
        {

            intAlreadyEnriched = 0;
            for(int number = 0; number<9; number++)
            {
                intMatcher[number]= 0;
            }
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

                    Hashtable enrichedPrograms = new Hashtable();

                    UpcomingRecording[] upcomingRecs = GetUpcomingRecordings();

                    ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, "Starting process to enrich guide data.  Processing " + Convert.ToString(upcomingRecs.Length) + " upcoming shows");
                    log.InfoFormat("{0}: Starting the process to enrich guide data.  Processing {1} upcoming shows", MODULE, Convert.ToString(upcomingRecs.Length));

                    // lists to keep track of failed searches for series and episodes
                    // use these to prevent repeated searches that you know will fail

                    ArrayList noSeriesMatchList = new ArrayList();
                    ArrayList noEpisodeMatchList = new ArrayList();
                    List<Guid> uniqueProgramsOnly = new List<Guid>();

                    foreach (UpcomingRecording prgToRecord in upcomingRecs)
                    {
                        int intPreEnrichLength = enrichedPrograms.Count;

                        GuideProgram prog;
                        using (TvGuideServiceAgent tgsa = new TvGuideServiceAgent())
                        {
                            prog = tgsa.GetProgramById((Guid)prgToRecord.Program.GuideProgramId);
                        }

                        this.EnrichSubroutine(prog, ref uniqueProgramsOnly, ref noSeriesMatchList, ref noEpisodeMatchList, ref enrichedPrograms);

                        // find other programs that are part of this schedule and enrich them
                        // too so they have the same episode information
                        // otherwise, FTR thinks they are different and needs to record them, 
                        // often times creating a conflict.
                        if (intPreEnrichLength!=enrichedPrograms.Count)
                        {
                            Schedule scdProgramsSchedule;
                            UpcomingProgram[] upPrograms;
                            using (TvSchedulerServiceAgent tssa = new TvSchedulerServiceAgent())
                            {
                                scdProgramsSchedule = tssa.GetScheduleById(prgToRecord.Program.ScheduleId);
                                upPrograms = tssa.GetUpcomingPrograms(scdProgramsSchedule, true);
                            }

                            foreach (UpcomingProgram scdProgram in upPrograms)
                            {
                                GuideProgram prog2;
                                using (TvGuideServiceAgent tgsa = new TvGuideServiceAgent())
                                {
                                    prog2 = tgsa.GetProgramById((Guid)scdProgram.GuideProgramId);
                                }
                                this.EnrichSubroutine(prog2, ref uniqueProgramsOnly, ref noSeriesMatchList, ref noEpisodeMatchList, ref enrichedPrograms);
                            }
                        }
                    }

                    if (enrichedPrograms.Count > 0)
                    {
                        UpdateFTRGuideData(enrichedPrograms);
                    }
                    else
                    {
                        ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, "No programs were enriched");
                        log.Debug("No programs were enriched");
                    }

                    #region Condense multiple series if they exist
                    string s = "";
                    int intUniqueUnmatchedSeries = 0;
                    foreach (string se in noSeriesMatchList)
                    {
                        string currentSeries = se + " \\((?<timesfound>[0-9]+)\\)";
                        int tiFound = 0;
                        Regex regexObj = new Regex(currentSeries);
                        if (regexObj.IsMatch(s))
                        {
                            tiFound = Convert.ToInt32(regexObj.Match(s).Groups["timesfound"].Value);
                            int intNowFound = tiFound + 1;
                            Regex rgx = new Regex(se + " \\(" + tiFound + "\\)");
                            string replacePtrn = se + " (" + intNowFound + ")";
                            s = rgx.Replace(s, replacePtrn);

                            // Successful match
                        }
                        else// Match attempt failed
                        {
                            intUniqueUnmatchedSeries++;
                            if (s.Equals(""))
                            {
                                s += se + " (1)"; //First Unmatche episode
                            }
                            else
                            {
                                s += ", " + se + " (1)"; //Subsequent episodes 
                            }
                        }
                    } 
                    #endregion

                    #region Condense multiple episodes if they exist
                    string episodeName = string.Empty;
                    int intUniqueUnmatchedEpisodes = 0;
                    foreach (string ep in noEpisodeMatchList)
                    {
                        string currentEpisode = ep + " \\((?<timesfound>[0-9]+)\\)";
                        int tiFound;
                        Regex regexObj = new Regex(currentEpisode);
                        if (regexObj.IsMatch(episodeName))
                        {
                            tiFound = Convert.ToInt32(regexObj.Match(episodeName).Groups["timesfound"].Value);
                            int intNowFound = tiFound + 1;
                            Regex rgx = new Regex(ep + " \\(" + tiFound + "\\)");
                            string replacePtrn = ep + " (" + intNowFound + ")";
                            episodeName = rgx.Replace(episodeName, replacePtrn);
                            // Successful match
                        }
                        else// Match attempt failed
                        {
                            intUniqueUnmatchedEpisodes++;
                            if (episodeName.Equals(""))
                            {
                                episodeName += ep + " (1)"; //First Unmatche episode
                            }
                            else
                            {
                                episodeName += ", " + ep + " (1)"; //Subsequent episodes 
                            }
                        }
                    } 
                    #endregion

                    s = s.TruncateString(32000);
                    episodeName = episodeName.TruncateString(32000);

                    if (intUniqueUnmatchedSeries > 0)
                    {
                        ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, string.Format("There were {0} series that could not be matched.  See log for details.", intUniqueUnmatchedSeries));
                        log.ErrorFormat("The following series could not be matched: {0}", s);
                    }
                    if (intUniqueUnmatchedEpisodes > 0)
                    {
                        ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, string.Format("There were {0} episodes that could not be matched.  See log for details", intUniqueUnmatchedEpisodes));
                        log.ErrorFormat("The following episodes could not be matched: {0}", episodeName);                    
                    }

                    for (int x = 0; x < 9; x++)
                    {
                        log.DebugFormat("Matching Function {0} matched a total of {1} episodes", x + 1, intMatcher[x]);
                    }

                    log.DebugFormat("{0} episodes already contained data, and were not matched.", intAlreadyEnriched);
                    log.InfoFormat("{0}: Done enriching guide data", MODULE);
                }
            }
            catch (Exception topEx)
            {
                log.Error("The main loop for GuideEnricher received an exception", topEx);
            }
                    
        }

        private UpcomingRecording[] GetUpcomingRecordings()
        {
            UpcomingRecording[] upcomingRecs;
            using(TvControlServiceAgent tcsa = new TvControlServiceAgent())
            {
                upcomingRecs = tcsa.GetAllUpcomingRecordings(UpcomingRecordingsFilter.Recordings, false);
            }
            return upcomingRecs;
        }

        private void UpdateFTRGuideData(Hashtable enrichedPrograms)
        {
            using (TvGuideServiceAgent tgsa = new TvGuideServiceAgent())
            {
                String entryentries = "entries";
                string waswere = "were";
                if (enrichedPrograms.Count == 1)
                {
                    entryentries = "entry";
                    waswere = "was";
                }

                log.DebugFormat("{0}: About to commit enriched guide data. {1} {2} {3} enriched", MODULE, Convert.ToString(enrichedPrograms.Count), entryentries, waswere);
                this.ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, string.Format("About to commit enriched guide data. {0} entries were enriched.", enrichedPrograms.Count));

                // update "windowSize" programs at a time to prevent the webservice from timing out
                int windowBase = 0;
                int windowSize = int.Parse(this.config.getProperty("maxShowNumberPerUpdate"));
                int loopCount;

                ArrayList list = new ArrayList();
                list.AddRange(enrichedPrograms.Values);

                loopCount = list.Count / windowSize;
                for (int j = 0; j < loopCount + 1; j++)
                {
                    int currentWindowSize = windowSize;
                    if (list.Count < windowSize)
                    {
                        currentWindowSize = list.Count;
                    }
                    int k = 0;
                    GuideProgram[] progArray = new GuideProgram[currentWindowSize];

                    log.DebugFormat("Importing shows from {0} to {1} (zero-based)", Convert.ToString(windowBase), Convert.ToString(windowBase + currentWindowSize));

                    foreach (GuideProgram gp in list.GetRange(0, currentWindowSize))
                    {

                        progArray[k] = gp;
                        k++;
                    }

                    tgsa.ImportPrograms(progArray, GuideSource.XmlTv);
                    list.RemoveRange(0, currentWindowSize);
                    windowBase += currentWindowSize;
                }
            }
        }

        public void EnrichSubroutine(GuideProgram prgProgram, ref List<Guid> uniqueProgramsOnly, ref ArrayList noSeriesMatchList, ref ArrayList noEpisodeMatchList, ref Hashtable enrichedPrograms)
        {
            GuideProgram prog;
            using (TvGuideServiceAgent tgsa = new TvGuideServiceAgent())
            {
                prog = tgsa.GetProgramById(prgProgram.GuideProgramId);
            }

            if (!uniqueProgramsOnly.Contains(prog.GuideProgramId))
            {
                uniqueProgramsOnly.Add(prog.GuideProgramId);
                try
                {
                    if (noSeriesMatchList.Contains(prog.Title) || noEpisodeMatchList.Contains(prog.Title + "-" + prog.SubTitle))
                    {
                        // already failed on this, skip it=
                        return;
                    }
                    prog = enrichProgram(prog);
                }
                catch (NoSeriesMatchException)
                {
                    noSeriesMatchList.Add(prog.Title);
                    prog = null;
                }
                catch (NoEpisodeMatchException)
                {
                    noEpisodeMatchList.Add(prog.Title + "-" + prog.SubTitle);
                    prog = null;
                }
                catch (DataEnricherException)
                {
                    prog = null;
                }
                if (prog != null)
                {
                    log.Debug("About to commit recording to Pending Changes log");
                    enrichedPrograms = addToEnrichedPrograms(enrichedPrograms, prog);
                }
            }                
        }

        private Hashtable addToEnrichedPrograms(Hashtable existingTable, GuideProgram p)
        {
            try
            {
                existingTable.Add(p.GuideProgramId, p);
                log.DebugFormat("Successfully added this program to the Pending Updates.  CONGRATULATIONS! Currently pending updates: {0} ", existingTable.Count);

            }
            catch (ArgumentException)
            {
                log.Error("Pending updates reports that this recording already exists...  This shouldn't happen, but if it does, please notify us on the FTR forum!");
                // swallow it if it's already there.
            }
            return existingTable;
        }

        private GuideProgram enrichProgram(GuideProgram prog)
        {
            IDataEnricher dataEnricher = DataEnricherFactory.Instance.getGuideDataEnricher();
            try
            {
                if (String.IsNullOrEmpty(prog.SubTitle))
                {
                    log.WarnFormat("Error enriching program: {0} - {1:MM/dd/yy hh:mm tt} has no sub-title for matching purposes", prog.Title, prog.StartTime);
                }
                else
                {
                    string oldSXXEYY = prog.EpisodeNumberDisplay;
                    GuideProgram updatedProgram = (GuideProgram)dataEnricher.enrichProgram(prog);

                    try
                    {
                        string sep = updatedProgram.EpisodeNumberDisplay;
                        if (sep.Length == 6)
                        {
                            int epInt = Convert.ToInt32(sep.Substring(4));
                            int seasonInt = Convert.ToInt32(sep.Substring(1, 2));
                            if (prog.EpisodeNumber != epInt || prog.SeriesNumber != seasonInt || !sep.Equals(oldSXXEYY))
                            {
                                log.DebugFormat("Updating listing episode number: {0} ==> {1}", prog.EpisodeNumber, epInt);
                                prog.EpisodeNumber = epInt;
                                log.DebugFormat("Updating listing season number: {0} ==> {1}", prog.SeriesNumber, seasonInt);
                                prog.SeriesNumber = seasonInt;
                                log.DebugFormat("Updating listing SXXEYY number: {0} ==> {1}", prog.EpisodeNumberDisplay, sep);
                                prog.EpisodeNumberDisplay = sep;
                                return prog;
                            }

                            log.DebugFormat("Program listing {0} - {1} ({2}) is already up to date.  No changes will be made", prog.Title, prog.SubTitle, prog.EpisodeNumberDisplay);
                            intAlreadyEnriched++;
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        // couldn't convert ep nunmber to ints
                        log.ErrorFormat("Error while converting season/ep to integers: {0}", ex.Message);
                        return null;
                    }
                    return prog;
                }
            }
            catch (DataEnricherException dee)
            {
                // expected errors get thrown silently
                throw dee;
            }
            catch (Exception ex)
            {
                // unexpected error, swallow, log and move on
                log.Error("Error enriching program: {0}", ex);
            }
            // nothing was enriched
            return null;
        }

        public static void matchingSuccess(int matchCameFrom)
        {
            intMatcher[matchCameFrom]++;
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
