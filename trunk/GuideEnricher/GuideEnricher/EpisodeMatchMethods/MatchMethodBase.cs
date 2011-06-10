﻿namespace GuideEnricher.EpisodeMatchMethods
{
    using System.Collections.Generic;
    using System.Reflection;
    using GuideEnricher.Config;
    using log4net;
    using TvdbLib.Data;

    public abstract class MatchMethodBase : IEpisodeMatchMethod
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public int MatchAttempts { get; protected set; }

        public int SuccessfulMatches { get; protected set; }

        public bool IsMatched { get; private set; }

        public abstract string MethodName { get; }

        public abstract bool Match(GuideEnricherProgram enrichedGuideProgram, List<TvdbEpisode> episodes);

        protected bool Matched(GuideEnricherProgram guideProgram, TvdbEpisode episode)
        {
            this.SuccessfulMatches++;
            guideProgram.EpisodeNumberDisplay = Enricher.FormatSeasonAndEpisode(episode.SeasonNumber, episode.EpisodeNumber);
            if (bool.Parse(Config.GetInstance().getProperty("updateAll")))
            {
                guideProgram.SubTitle = episode.EpisodeName;
            }

            log.DebugFormat("[{0}] Correctly matched {1} - {2} as {3}", this.MethodName, guideProgram.Title, guideProgram.SubTitle, guideProgram.EpisodeNumberDisplay);
            return true;
        }

        protected bool Unmatched(GuideEnricherProgram guideProgram)
        {
            log.DebugFormat("[{0}] Could not match {1} - {2}", this.MethodName, guideProgram.Title, guideProgram.SubTitle);
            return false;
        }
    }
}