namespace GuideEnricher
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using ForTheRecord.Entities;
    using ForTheRecord.ServiceContracts;
    using GuideEnricher.EpisodeMatchMethods;
    using GuideEnricher.Exceptions;
    using GuideEnricher.tvdb;
    using log4net;

    public class Enricher
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly List<EnrichedGuideProgram> enrichedPrograms;
        private readonly ITvSchedulerService tvSchedulerService;
        private readonly ITvGuideService tvGuideService;
        private readonly IConfiguration config;
        private readonly ILogService ftrlogAgent;
        private readonly List<Guid> uniqueProgramsOnly;
        private readonly List<IEpisodeMatchMethod> matchMethods;
        private TvdbLibAccess tvdbLibAccess;
        private ArrayList noSeriesMatchList;
        private ArrayList noEpisodeMatchList;

        private const string MODULE = "GuideEnricher";

        public Enricher(IConfiguration configuration, ILogService ftrLogService, ITvGuideService tvGuideService, ITvSchedulerService tvSchedulerService)
        {
            this.config = configuration;
            this.enrichedPrograms = new List<EnrichedGuideProgram>();
            this.uniqueProgramsOnly = new List<Guid>();
            this.ftrlogAgent = ftrLogService;
            this.tvGuideService = tvGuideService;
            this.tvSchedulerService = tvSchedulerService;
            this.matchMethods = EpisodeMatchMethodLoader.GetMatchMethods();
        }

        public void EnrichUpcomingPrograms()
        {
//            var upcomingRecordings = this.tvControlService.GetAllUpcomingRecordings(UpcomingRecordingsFilter.Recordings, false);
//            this.ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, "Starting process to enrich guide data.  Processing " + Convert.ToString(upcomingRecordings.Length) + " upcoming shows");
//            log.InfoFormat("{0}: Starting the process to enrich guide data.  Processing {1} upcoming shows", MODULE, Convert.ToString(upcomingRecordings.Length));

            // lists to keep track of failed searches for series and episodes
            // use these to prevent repeated searches that you know will fail

            this.noSeriesMatchList = new ArrayList();
            this.noEpisodeMatchList = new ArrayList();

            using (tvdbLibAccess = new TvdbLibAccess(this.config, this.matchMethods))
            {
                foreach (var scheduleSummary in this.tvSchedulerService.GetAllSchedules(ChannelType.Television, ScheduleType.Recording, true))
                {
                    log.DebugFormat("Enriching {0}", scheduleSummary.Name);
                    var schedule = this.tvSchedulerService.GetScheduleById(scheduleSummary.ScheduleId);
                    foreach (var upcomingProgram in this.tvSchedulerService.GetUpcomingPrograms(schedule, true))
                    {
                        var guideProgram = new EnrichedGuideProgram(this.tvGuideService.GetProgramById((Guid)upcomingProgram.GuideProgramId));
                        if (!guideProgram.Matched)
                        {
                            this.EnrichSubroutine(guideProgram);
                        }
                    }
                }

                if (this.enrichedPrograms.Count > 0)
                {
                    this.UpdateFTRGuideData();
                }
                else
                {
                    ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, "No programs were enriched");
                    log.Debug("No programs were enriched");
                }

                foreach (var matchMethod in matchMethods)
                {
                    log.DebugFormat("Match method {0} matched {1} out of {2} attempts", matchMethod.MethodName, matchMethod.SuccessfulMatches, matchMethod.MatchAttempts);
                }
            }
        }

        public void EnrichSubroutine(EnrichedGuideProgram guideProgram)
        {
            if (this.uniqueProgramsOnly.Contains(guideProgram.GuideProgramId))
            {
                return;
            }

            this.uniqueProgramsOnly.Add(guideProgram.GuideProgramId);
            
            try
            {
                if (noSeriesMatchList.Contains(guideProgram.Title) || noEpisodeMatchList.Contains(guideProgram.Title + "-" + guideProgram.SubTitle))
                {
                    return;
                }

                this.tvdbLibAccess.EnrichProgram(guideProgram);
            }
            catch (NoSeriesMatchException noSeriesMatchException)
            {
                this.ftrlogAgent.LogMessage(MODULE, LogSeverity.Error, string.Format("Cannot find the correct series for {0}, consider editing the application config with the correct id using \"id=xxxxx\"", guideProgram.Title));
                this.noSeriesMatchList.Add(guideProgram.Title);
                return;
            }
            catch (NoEpisodeMatchException)
            {
                this.noEpisodeMatchList.Add(guideProgram.Title + "-" + guideProgram.SubTitle);
                return;
            }
            catch (DataEnricherException)
            {
                return;
            }

            if (guideProgram.Matched)
            {
                this.enrichedPrograms.Add(guideProgram);
            }
        }

        private void UpdateFTRGuideData()
        {
            log.DebugFormat("About to commit enriched guide data. {0} entries were enriched", this.enrichedPrograms.Count);
            this.ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, String.Format("About to commit enriched guide data. {0} entries were enriched.", this.enrichedPrograms.Count));

            int position = 0;
            int windowSize = Int32.Parse(this.config.getProperty("maxShowNumberPerUpdate"));
            List<GuideProgram> guidesToUpdate;

            while (position + windowSize < this.enrichedPrograms.Count)
            {
                log.DebugFormat("Importing shows {0} to {1}", position + 1, position + windowSize + 1);
                guidesToUpdate = new List<GuideProgram>();
                this.enrichedPrograms.GetRange(position, windowSize).ForEach(x => guidesToUpdate.Add(x.GuideProgram));
                this.UpdateForTheRecordPrograms(guidesToUpdate.ToArray());
                position += 10;
            }

            log.DebugFormat("Importing shows {0} to {1}", position + 1, this.enrichedPrograms.Count);
            guidesToUpdate = new List<GuideProgram>();
            this.enrichedPrograms.GetRange(position, this.enrichedPrograms.Count - position).ForEach(x => guidesToUpdate.Add(x.GuideProgram));
            this.UpdateForTheRecordPrograms(guidesToUpdate.ToArray());
        }

        private void UpdateForTheRecordPrograms(GuideProgram[] programs)
        {
            this.tvGuideService.ImportPrograms(programs, GuideSource.Other);
        }

        public static string FormatSeasonAndEpisode(int season, int episode)
        {
            return String.Format("S{0:00}E{1:00}", season, episode);
        }
    }
}
