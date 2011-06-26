namespace GuideEnricher
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using ForTheRecord.Entities;
    using ForTheRecord.ServiceContracts;
    using GuideEnricher.Config;
    using GuideEnricher.EpisodeMatchMethods;
    using GuideEnricher.Model;
    using GuideEnricher.tvdb;
    using log4net;
    using TvdbLib.Data;
    
    public class Enricher
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly List<GuideEnricherEntities> enrichedPrograms;
        private readonly ITvSchedulerService tvSchedulerService;
        private readonly ITvGuideService tvGuideService;
        private readonly IConfiguration config;
        private readonly ILogService ftrlogAgent;
        private readonly List<IEpisodeMatchMethod> matchMethods;
        private readonly TvdbLibAccess tvdbLibAccess;

        private const string MODULE = "GuideEnricher";

        public Enricher(IConfiguration configuration, ILogService ftrLogService, ITvGuideService tvGuideService, ITvSchedulerService tvSchedulerService, TvdbLibAccess tvdbLibAccess, List<IEpisodeMatchMethod> matchMethods)
        {
            this.config = configuration;
            this.enrichedPrograms = new List<GuideEnricherEntities>();
            this.ftrlogAgent = ftrLogService;
            this.tvGuideService = tvGuideService;
            this.tvSchedulerService = tvSchedulerService;
            this.tvdbLibAccess = tvdbLibAccess;
            this.matchMethods = matchMethods;
        }

        public void EnrichUpcomingPrograms(ScheduleType scheduleType)
        {
            bool updateMatchedEpisodes = bool.Parse(config.getProperty("updateAll"));
            bool updateSubtitles = bool.Parse(config.getProperty("updateSubtitles"));
            // GetUpcomingGuidePrograms seems to be missing som info
            // var programs = this.tvSchedulerService.GetUpcomingGuidePrograms(scheduleType, true);
            var programs = this.tvSchedulerService.GetAllUpcomingPrograms(scheduleType, true);
            var seriesToEnrich = new Dictionary<string, GuideEnricherSeries>();

            foreach (var program in programs)
            {
                if (!program.GuideProgramId.HasValue)
                {
                    log.DebugFormat("[{0}] {1} - {2:MM/dd hh:mm tt} does not have a guide program entry", program.Title, program.StartTime);
                    break;
                }

                var guideProgram = new GuideEnricherEntities(this.tvGuideService.GetProgramById(program.GuideProgramId.Value));
                if (!seriesToEnrich.ContainsKey(guideProgram.Title))
                {
                    seriesToEnrich.Add(guideProgram.Title, new GuideEnricherSeries(guideProgram.Title, updateMatchedEpisodes, updateSubtitles));
                }

                seriesToEnrich[guideProgram.Title].AddProgram(guideProgram);
            }

            foreach (var series in seriesToEnrich.Values)
            {
                this.EnrichSeries(series);
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

        public void EnrichSeries(GuideEnricherSeries series)
        {
            series.TvDbSeriesID = tvdbLibAccess.getSeriesId(series.Title);
            if (series.TvDbSeriesID == 0)
            {
                series.isIgnored = true;
            }

            if (series.isIgnored)
            {
                series.IgnoredPrograms.AddRange(series.PendingPrograms);
                series.PendingPrograms.Clear();
            }

            if (series.PendingPrograms.Count > 0)
            {
                log.DebugFormat("Beginning enrichment of episodes for series {0}", series.Title);
                var onlineSeries = tvdbLibAccess.GetTvdbSeries(series.TvDbSeriesID, false);
                EnrichProgramsInSeries(series, onlineSeries);
                if (series.FailedPrograms.Count > 0)
                {
                    log.DebugFormat("The first run for the series {0} had unmatched episodes.  Checking for online updates.", series.Title);

                    List<string> currentTvDbEpisodes = new List<string>();
                    onlineSeries.Episodes.ForEach(x => currentTvDbEpisodes.Add(x.EpisodeName));

                    TvdbSeries UpdatedOnlineSeries = tvdbLibAccess.GetTvdbSeries(series.TvDbSeriesID, true);
                    if (UpdatedOnlineSeries.Episodes.FindAll(x => !currentTvDbEpisodes.Contains(x.EpisodeName)).Count > 0)
                    {
                        log.DebugFormat("New episodes were found.  Trying enrichment again.");
                        series.TvDbInformationRefreshed();
                        EnrichProgramsInSeries(series, UpdatedOnlineSeries);
                    }
                }

                enrichedPrograms.AddRange(series.SuccessfulPrograms);
            }
        }

        private void EnrichProgramsInSeries(GuideEnricherSeries series, TvdbSeries OnlineSeries)
        {
            tvdbLibAccess.DebugEpisodeDump(OnlineSeries);
            do
            {
                GuideEnricherEntities guideProgram = series.PendingPrograms[0];
                tvdbLibAccess.EnrichProgram(guideProgram, OnlineSeries);
                if (guideProgram.Matched)
                {
                    series.AddAllToEnrichedPrograms(guideProgram);
                }
                else
                {
                    series.AddAllToFailedPrograms(guideProgram);
                }
            } 
            while (series.PendingPrograms.Count > 0);
            
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
