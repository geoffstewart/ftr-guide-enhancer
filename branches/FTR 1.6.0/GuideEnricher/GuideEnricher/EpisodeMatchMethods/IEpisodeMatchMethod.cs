namespace GuideEnricher.EpisodeMatchMethods
{
    using System.Collections.Generic;
    using GuideEnricher.Model;
    using TvdbLib.Data;

    public interface IEpisodeMatchMethod
    {
        int MatchAttempts { get; }
        
        int SuccessfulMatches { get; }

        string MethodName { get; }

        bool Match(GuideEnricherEntities enrichedGuideProgram, List<TvdbEpisode> episodes);
    }
}