namespace GuideEnricher.EpisodeMatchMethods
{
    using System.Collections.Generic;
    using System.Reflection;
    using GuideEnricher.Model;
    using log4net;
    using TvdbLib.Data;
    using System.Linq;

    public class AirDateMatchMethod : MatchMethodBase
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override string MethodName
        {
            get { return "Original Air Date";  }
        }

        public override bool Match(GuideEnricherEntities guideProgram, List<TvdbEpisode> episodes)
        {
            if (!guideProgram.PreviouslyAiredTime.HasValue)
            {
                this.log.DebugFormat("[{0}] {1} - {2:MM/dd hh:mm tt} does not have an original air date", this.MethodName, guideProgram.Title, guideProgram.StartTime);
                return false;
            }

            this.MatchAttempts++;
            var match = episodes.Where(e => e.FirstAired == guideProgram.PreviouslyAiredTime).FirstOrDefault();

            if (match != null)
            {
                return this.Matched(guideProgram, match);
            }

            return this.Unmatched(guideProgram);
        }
    }
}