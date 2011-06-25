namespace GuideEnricher
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using ForTheRecord.Entities;
    using ForTheRecord.ServiceContracts;
    using GuideEnricher.Config;
    using GuideEnricher.EpisodeMatchMethods;
    using GuideEnricher.Exceptions;
    using GuideEnricher.Model;
    using GuideEnricher.tvdb;
    using log4net;
    using System.Linq;

    public class Enricher
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly List<GuideEnricherProgram> enrichedPrograms;
        private readonly ITvSchedulerService tvSchedulerService;
        private readonly ITvGuideService tvGuideService;
        private readonly IConfiguration config;
        private readonly ILogService ftrlogAgent;
        private readonly List<IEpisodeMatchMethod> matchMethods;
        private TvdbLibAccess tvdbLibAccess;

        private const string MODULE = "GuideEnricher";

        public Enricher(IConfiguration configuration, ILogService ftrLogService, ITvGuideService tvGuideService, ITvSchedulerService tvSchedulerService)
        {
            this.config = configuration;
            this.enrichedPrograms = new List<GuideEnricherProgram>();
            this.ftrlogAgent = ftrLogService;
            this.tvGuideService = tvGuideService;
            this.tvSchedulerService = tvSchedulerService;
            this.matchMethods = EpisodeMatchMethodLoader.GetMatchMethods();
        }

        public void EnrichUpcomingPrograms(ScheduleType scheduleType)
        {
            using (this.tvdbLibAccess = new TvdbLibAccess(this.config, this.matchMethods))
            {
                foreach (var scheduleSummary in this.tvSchedulerService.GetAllSchedules(ChannelType.Television, scheduleType, true))
                {
                    try
                    {
                        if (scheduleSummary.IsActive)
                        {
                            log.DebugFormat("Enriching {0}", scheduleSummary.Name);
                            var schedule = this.tvSchedulerService.GetScheduleById(scheduleSummary.ScheduleId);
                            var umatchedEpisodes = this.EnrichProgramsInSchedule(schedule, false);
                            if (umatchedEpisodes)
                            {
                                // Force a refresh on the series and try again...
                                this.EnrichProgramsInSchedule(schedule, true);
                            }
                        }
                    }
                    catch (NoSeriesMatchException)
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
                    var message = string.Format("No {0} entries were enriched", scheduleType);
                    this.ftrlogAgent.LogMessage(MODULE, LogSeverity.Information, message);
                    log.Debug(message);
                }

                foreach (var matchMethod in this.matchMethods)
                {
                    log.DebugFormat("Match method {0} matched {1} out of {2} attempts", matchMethod.MethodName, matchMethod.SuccessfulMatches, matchMethod.MatchAttempts);
                }
            }
        }

        private bool EnrichProgramsInSchedule(Schedule schedule, bool forceRefresh)
        {
            bool unmatchedEpisodes = true;

            var upcomingPrograms = this.tvSchedulerService.GetUpcomingPrograms(schedule, true);

            if (upcomingPrograms.Length == 0)
            {
                log.InfoFormat("Schedule '{0}' has no upcoming programs", schedule.Name);
                
                // return false to avoid unnecessary refresh
                return false;
            }

            foreach (var upcomingProgram in upcomingPrograms)
            {
                var guideProgram = new GuideEnricherProgram(this.tvGuideService.GetProgramById((Guid)upcomingProgram.GuideProgramId));
                if ((!guideProgram.Matched || bool.Parse(this.config.getProperty("updateAll"))) && !guideProgram.Ignore)
                {
                    var programWithSameSubTitle = this.enrichedPrograms.Where(x => x.Title == guideProgram.Title && x.SubTitle == guideProgram.SubTitle).FirstOrDefault();
                    if (programWithSameSubTitle != null)
                    {
                        guideProgram.EpisodeNumberDisplay = programWithSameSubTitle.EpisodeNumberDisplay;
                        guideProgram.SeriesNumber = programWithSameSubTitle.SeriesNumber;
                        guideProgram.EpisodeNumber = programWithSameSubTitle.EpisodeNumber;

                        if (bool.Parse(config.getProperty("updateSubtitles")))
                        {
                            guideProgram.SubTitle = programWithSameSubTitle.SubTitle;
                        }

                        guideProgram.Matched = true;
                        this.enrichedPrograms.Add(guideProgram);
                    }
                    else
                    {
                        var programWithSameTitle = this.enrichedPrograms.Where(x => x.Title == guideProgram.Title).FirstOrDefault();
                        if (programWithSameTitle != null)
                        {
                            guideProgram.TheTVDBSeriesID = programWithSameTitle.TheTVDBSeriesID;
                        }

                        this.tvdbLibAccess.EnrichProgram(guideProgram, forceRefresh);
                        
                        if (guideProgram.Matched)
                        {
                            this.enrichedPrograms.Add(guideProgram);
                        }
                        else
                        {
                            guideProgram.Ignore = true;
                        }
                    }
                    
                    // Force a refresh only once
                    forceRefresh = false;
                }
                else
                {
                    unmatchedEpisodes = false;
                }
            }

            return unmatchedEpisodes;
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
                List<GuideProgram> update = guidesToUpdate;
                this.enrichedPrograms.GetRange(position, windowSize).ForEach(x => update.Add(x.GuideProgram));
                this.UpdateForTheRecordPrograms(guidesToUpdate.ToArray());
                position += windowSize;
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
