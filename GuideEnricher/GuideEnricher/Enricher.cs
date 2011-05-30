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
        private readonly List<GuideEnricherProgram> enrichedPrograms;
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
            this.enrichedPrograms = new List<GuideEnricherProgram>();
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

            this.noEpisodeMatchList = new ArrayList();

            using (tvdbLibAccess = new TvdbLibAccess(this.config, this.matchMethods))
            {
                foreach (var scheduleSummary in this.tvSchedulerService.GetAllSchedules(ChannelType.Television, ScheduleType.Recording, true))
                {
                    try
                    {
                        log.DebugFormat("Enriching {0}", scheduleSummary.Name);
                        var schedule = this.tvSchedulerService.GetScheduleById(scheduleSummary.ScheduleId);
                        var noUmatchedEpisodes = this.EnrichProgramsInSchedule(schedule, false);
                        if (!noUmatchedEpisodes)
                        {
                            // Force a refresh on the series and try again...
                            this.EnrichProgramsInSchedule(schedule, true);
                        }
                    }
                    catch (NoSeriesMatchException noSeriesMatchException)
                    {
                        this.ftrlogAgent.LogMessage(MODULE, LogSeverity.Error, string.Format("Cannot find the correct series for schedule '{0}', consider editing the application config with the correct id using \"id=xxxxx\"", scheduleSummary.Name));
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

        private bool EnrichProgramsInSchedule(Schedule schedule, bool forceRefresh)
        {
            bool noUnmatchedEpisodes = true;

            foreach (var upcomingProgram in this.tvSchedulerService.GetUpcomingPrograms(schedule, true))
            {
                var guideProgram = new GuideEnricherProgram(this.tvGuideService.GetProgramById((Guid)upcomingProgram.GuideProgramId));
                if (!guideProgram.Matched)
                {
                    this.EnrichSingleProgram(guideProgram, forceRefresh);

                    // Force a refresh only once
                    forceRefresh = false;
                }
                else
                {
                    noUnmatchedEpisodes = false;
                }
            }

            return noUnmatchedEpisodes;
        }

        public void EnrichSingleProgram(GuideEnricherProgram guideProgram, bool forceRefresh)
        {
            if (this.uniqueProgramsOnly.Contains(guideProgram.GuideProgramId))
            {
                return;
            }

            this.uniqueProgramsOnly.Add(guideProgram.GuideProgramId);
            
            try
            {
                if (noEpisodeMatchList.Contains(guideProgram.Title + "-" + guideProgram.SubTitle))
                {
                    return;
                }

                this.tvdbLibAccess.EnrichProgram(guideProgram, forceRefresh);
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
