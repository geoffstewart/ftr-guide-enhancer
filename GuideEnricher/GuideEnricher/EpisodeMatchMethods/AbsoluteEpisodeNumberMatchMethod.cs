namespace GuideEnricher.EpisodeMatchMethods
{
    using System.Collections.Generic;
    using System.Reflection;
    using GuideEnricher.Model;
    using log4net;
    using TvdbLib.Data;
    using System.Linq;

    public class AbsoluteEpisodeNumberMatchMethod : MatchMethodBase
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override string MethodName
        {
            get { return "Absolute Episode Number"; }
        }

        public override bool Match(GuideEnricherEntities guideProgram, List<TvdbEpisode> episodes)
        {
            var episodeNumber = guideProgram.GetValidEpisodeNumber();
            if (episodeNumber == 0)
            {
                this.log.DebugFormat("Cannot use match method [{0}] {1} does not have an episode number", this.MethodName, guideProgram.Title);
                return false;
            }
            
            this.CalculateAbsoluteNumbers(episodes);
            this.MatchAttempts++;

            foreach (var episode in episodes)
            {
                if (episodeNumber == episode.AbsoluteNumber)
                {
                    return this.Matched(guideProgram, episode);
                }
            }

            return this.Unmatched(guideProgram);
        }

        public void CalculateAbsoluteNumbers(List<TvdbEpisode> episodes)
        {
            int absoluteNumber = 0;
            var actualEpisodes = episodes.Where(x => x.IsSpecial == false).ToList();
            actualEpisodes.Sort(new TvEpisodeComparer());
            foreach (var episode in actualEpisodes)
            {
                if (episode.AbsoluteNumber != -99)
                {
                    absoluteNumber = episode.AbsoluteNumber;
                }
                else
                {
                    absoluteNumber++;
                    episode.AbsoluteNumber = absoluteNumber;
                }

                this.log.DebugFormat("{0}-{1} is absolute number {2}", episode.SeasonNumber, episode.EpisodeNumber, episode.AbsoluteNumber);
            }
        }
    }
}