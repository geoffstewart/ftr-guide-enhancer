namespace GuideEnricher.EpisodeMatchMethods
{
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using log4net;
    using TvdbLib.Data;

    [MatchMethodPriority(Priority = 4)]
    public class RemoveCommonWordsMatchMethod : MatchMethodBase
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string COMMON_WORDS_REG_EX = @"1|2|3|4|5|6|7|8|9|0|\(|\)|&|part|in|,|is|and|the|I|X|V|-|%|percent| ";

        public override string MethodName
        {
            get { return "Remove common words"; }
        }

        public override bool Match(EnrichedGuideProgram guideProgram, List<TvdbEpisode> episodes)
        {
            if (string.IsNullOrEmpty(guideProgram.SubTitle))
            {
                log.DebugFormat("Cannot use match method [{0}] {1} does not have a subtitle", this.MethodName, guideProgram.Title);
                return this.Unmatched(guideProgram);
            }

            this.MatchAttempts++;

            foreach (var episode in episodes)
            {
                if (!string.IsNullOrEmpty(episode.EpisodeName))
                {
                    if (Regex.Replace(episode.EpisodeName, COMMON_WORDS_REG_EX, string.Empty, RegexOptions.IgnoreCase) == Regex.Replace(guideProgram.SubTitle, COMMON_WORDS_REG_EX, string.Empty, RegexOptions.IgnoreCase))
                    {
                        return this.Matched(guideProgram, episode);
                    }
                }
            }

            return this.Unmatched(guideProgram);
        }
    }
}