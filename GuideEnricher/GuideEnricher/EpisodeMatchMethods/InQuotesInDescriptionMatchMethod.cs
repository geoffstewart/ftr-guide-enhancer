namespace GuideEnricher.EpisodeMatchMethods
{
    using System.Collections.Generic;
    using System.Linq;

    using GuideEnricher.Model;

    using TvdbLib.Data;

    public class InQuotesInDescriptionMatchMethod : MatchMethodBase
    {
        protected System.Text.RegularExpressions.Regex quotedSentence = new System.Text.RegularExpressions.Regex(@"(?<=').*?(?=')");

        public override string MethodName
        {
            get
            {
                return "Inside Single Quotes in Description";
            }
        }

        public override bool Match(GuideEnricherProgram enrichedGuideProgram, List<TvdbEpisode> episodes)
        {
            this.MatchAttempts++;
            var match = quotedSentence.Match(enrichedGuideProgram.Description);
            if (match != null && !string.IsNullOrEmpty(match.Value))
            {
                var matchedEpisode = episodes.FirstOrDefault(x => x.EpisodeName == match.Value);
                if (matchedEpisode != null)
                {
                    return this.Matched(enrichedGuideProgram, matchedEpisode);
                }
            }
            return false;
        }
    }
}