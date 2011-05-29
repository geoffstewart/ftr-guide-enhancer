namespace GuideEnricher.EpisodeMatchMethods
{
    using System.Collections.Generic;
    using TvdbLib.Data;

    public interface IEpisodeMatchMethod
    {
        int MatchAttempts { get; }
        
        int SuccessfulMatches { get; }

        string MethodName { get; }

        bool Match(EnrichedGuideProgram enrichedGuideProgram, List<TvdbEpisode> episodes);
    }
}