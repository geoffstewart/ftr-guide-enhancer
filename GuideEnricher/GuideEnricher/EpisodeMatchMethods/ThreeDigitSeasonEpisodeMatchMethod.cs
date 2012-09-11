namespace GuideEnricher.EpisodeMatchMethods
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using GuideEnricher.Model;

    using TvdbLib.Data;

    using log4net;

    public class ThreeDigitSeasonEpisodeMatchMethod : MatchMethodBase
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override string MethodName
        {
            get
            {
                return "Three Digit Season Episode";
            }
        }

        public override bool Match(GuideEnricherEntities enrichedGuideProgram, List<TvdbEpisode> episodes)
        {
            var lastSeasonNumber = episodes.Max(x => x.SeasonNumber);
            var episodeNumber = enrichedGuideProgram.GetValidEpisodeNumber();
            if (lastSeasonNumber > 9)
            {
                this.log.DebugFormat("Cannot use match method [{0}] for {1} as there are more than 9 seasons", this.MethodName, enrichedGuideProgram.Title);
                return false;
            }

            this.MatchAttempts++;

            var matchedEpisode = episodes.FirstOrDefault(x => x.SeasonNumber == episodeNumber / 100 && x.EpisodeNumber == episodeNumber % 100);
            if (matchedEpisode != null)
            {
                return this.Matched(enrichedGuideProgram, matchedEpisode);
            }
            
            return false;
        }
    }
}