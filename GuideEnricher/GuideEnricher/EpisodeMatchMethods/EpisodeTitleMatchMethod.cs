namespace GuideEnricher.EpisodeMatchMethods
{
    using System.Collections.Generic;
    using System.Reflection;
    using GuideEnricher.Model;
    using log4net;
    using TvdbLib.Data;

    public class EpisodeTitleMatchMethod : MatchMethodBase
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override string MethodName
        {
            get { return "Episode Title"; }
        }

        public override bool Match(GuideEnricherProgram guideProgram, List<TvdbEpisode> episodes)
        {
            if (string.IsNullOrEmpty(guideProgram.SubTitle))
            {
                this.log.DebugFormat("Cannot use match method [{0}] {1} does not have a subtitle", this.MethodName, guideProgram.Title);
                return false;
            }

            this.MatchAttempts++;
            foreach (var episode in episodes)
            {
                if (episode.EpisodeName == guideProgram.SubTitle)
                {
                    return this.Matched(guideProgram, episode);
                }
            }

            return this.Unmatched(guideProgram);
        }
    }
}