namespace GuideEnricher.EpisodeMatchMethods
{
    using System.Collections.Generic;
    using System.Reflection;
    using GuideEnricher.tvdb;
    using log4net;
    using TvdbLib.Data;

    public abstract class MatchMethodBase : IEpisodeMatchMethod
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public int MatchAttempts { get; protected set; }

        public int SuccessfulMatches { get; protected set; }

        public bool IsMatched { get; private set; }

        public abstract string MethodName { get; }

        public abstract bool Match(EnrichedGuideProgram enrichedGuideProgram, List<TvdbEpisode> episodes);

        protected bool Matched(EnrichedGuideProgram guideProgram, TvdbEpisode episode)
        {
            this.SuccessfulMatches++;
            guideProgram.EpisodeNumberDisplay = Enricher.FormatSeasonAndEpisode(episode.SeasonNumber, episode.EpisodeNumber);

            log.DebugFormat("[{0}] Correctly matched {1} - {2} as {3}", this.MethodName, guideProgram.Title, guideProgram.SubTitle, guideProgram.EpisodeNumberDisplay);
            return true;
        }

        protected bool Unmatched(EnrichedGuideProgram guideProgram)
        {
            log.DebugFormat("[{0}] Could not match {1} - {2}", this.MethodName, guideProgram.Title, guideProgram.SubTitle);
            return false;
        }
    }
}