/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 3/25/2010
 * Time: 8:47 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace GuideEnricher.tvdb
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using GuideEnricher.Config;
    using GuideEnricher.EpisodeMatchMethods;
    using GuideEnricher.Exceptions;
    using GuideEnricher.Model;
    using log4net;
    using TvdbLib;
    using TvdbLib.Cache;
    using TvdbLib.Data;
    using TvdbLib.Exceptions;

    /// <summary>
    /// Description of TvdbLibAccess.
    /// </summary>
    public class TvdbLibAccess: IDisposable
    {
        private const string TVDBID = "BBB734ABE146900D";  // mine, don't abuse it!!!
        private const string MODULE = "GuideEnricher";
        
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IConfiguration config;
        private readonly List<IEpisodeMatchMethod> matchMethods;

        private TvdbHandler tvdbHandler;
        private Dictionary<string, string> seriesNameMapping;
        private Dictionary<string, int> seriesIDMapping = new Dictionary<string, int>();
        private Dictionary<string, string> seriesNameRegex = new Dictionary<string, string>();
        private List<string> seriesIgnore = new List<string>();

        private Dictionary<string, int> seriesCache = new Dictionary<string, int>();

        private TvdbLanguage language = TvdbLanguage.DefaultLanguage;

        public TvdbLibAccess(IConfiguration configuration, List<IEpisodeMatchMethod> matchMethods)
        {
            this.config = configuration;
            this.matchMethods = matchMethods;
            this.Init();
        }

        private void Init()
        {
            string cache = this.config.getProperty("TvDbLibCache");
            tvdbHandler = new TvdbHandler(new XmlCacheProvider(cache), TVDBID);
            tvdbHandler.InitCache();

            seriesNameMapping = this.config.getSeriesNameMap();
            seriesIgnore = this.config.getIgnoredSeries();
            this.language = SetLanguage();
            this.IntializeRegexMappings();
        }

        public void EnrichProgram(GuideEnricherProgram existingProgram, bool forceRefresh)
        {
            log.DebugFormat("Starting lookup for {0} - {1}", existingProgram.Title, existingProgram.SubTitle);
            var seriesId = this.getSeriesId(existingProgram.Title);

            TvdbSeries tvdbSeries = null;
            tvdbSeries = GetTvdbSeries(seriesId, tvdbSeries, forceRefresh);

            if (tvdbSeries == null)
            {
                log.ErrorFormat("TVDB issue getting series info for {0}", existingProgram.Title);
                return;
            }

            if (config.getProperty("dumpepisodes").ToUpper() == "TRUE")
            {
                this.DumpSeriesEpisodes(tvdbSeries, tvdbSeries.Episodes);
            }

            foreach (var matchMethod in this.matchMethods)
            {
                if (matchMethod.Match(existingProgram, tvdbSeries.Episodes))
                {
                    existingProgram.Matched = true;
                    break;
                }
            }
        }

        private TvdbSeries GetTvdbSeries(int seriesId, TvdbSeries tvdbSeries, bool forceRefresh)
        {
            bool callSuccessful = false;
            int attemptNumber = 0;
            while (attemptNumber++ < 3 && !callSuccessful)
            {
                try
                {
                    tvdbSeries = this.tvdbHandler.GetSeries(seriesId, this.language, true, false, false);
                    if (forceRefresh)
                    {
                        tvdbSeries = this.tvdbHandler.ForceReload(tvdbSeries, true, false, false);
                    }

                    callSuccessful = true;
                }
                catch (TvdbException tvdbException)
                {
                    this.log.Debug("TVDB Error getting series", tvdbException);
                }
            }

            return tvdbSeries;
        }

        private void DumpSeriesEpisodes(TvdbSeries series, List<TvdbEpisode> episodes)
        {
            this.log.InfoFormat("Dumping episode info for {0}", series.SeriesName);
            foreach (var episode in episodes)
            {
                this.log.InfoFormat("S{0:00}E{1:00}-{2}", episode.SeasonNumber, episode.EpisodeNumber, episode.EpisodeName);
            }
        }

        public void Dispose()
        {
            this.tvdbHandler.CloseCache();
        }

        private void IntializeRegexMappings()
        {
            if (this.seriesNameMapping == null)
            {
                return;
            }

            foreach (string regex in this.seriesNameMapping.Keys)
            {
                if (this.seriesNameMapping[regex].StartsWith("id="))
                {
                    this.seriesIDMapping.Add(regex, int.Parse(this.seriesNameMapping[regex].Substring(3)));
                    this.seriesNameMapping.Remove(regex);
                    break;
                }

                if (regex.StartsWith("regex="))
                {
                    this.seriesNameRegex.Add(regex.Substring(6), this.seriesNameMapping[regex]);
                    this.seriesNameMapping.Remove(regex);
                    break;
                }
            }
        }

        private TvdbLanguage SetLanguage()
        {
            List<TvdbLanguage> availableLanguages = this.tvdbHandler.Languages;
            string selectedLanguage = this.config.getProperty("TvDbLanguage");
            
            // if there is a value for TvDbLanguage in the settings, set the right language
            if (string.IsNullOrEmpty(selectedLanguage))
            {
                selectedLanguage = "en";
            }

            TvdbLanguage lang = availableLanguages.Find(x => x.Abbriviation == selectedLanguage);
            this.log.DebugFormat("Language: {0}", lang.Abbriviation);
            return lang;
        }

        public int getSeriesId(string seriesName)
        {
            // TODO: A few things here.  We should add more intelligence when there is more then 1 match
            // Things like default to (US) or (UK) or what ever is usally the case.  Also, perhaps a global setting that would always take newest series first...

            if (this.seriesIDMapping.ContainsKey(seriesName))
            {
                var seriesid = this.seriesIDMapping[seriesName];
                log.DebugFormat("SD-TvDb: Direct mapping: series: {0} id: {1}", seriesName, seriesid);
                return seriesid;
            }
            
            if (seriesCache.ContainsKey(seriesName))
            {
                log.DebugFormat("SD-TvDb: Series cache hit: {0} has id: {1}", seriesName, seriesCache[seriesName]);
                return seriesCache[seriesName];
            }

            ThrowIfSeriesIgnored(seriesName);

            string searchSeries = seriesName;

            if (IsSeriesInMappedSeries(seriesName))
            {
                searchSeries = this.seriesNameMapping[seriesName];
            }
            else if (this.seriesNameRegex.Count > 0)
            {
                foreach (string regexEntry in seriesNameRegex.Keys)
                {
                    var regex = new Regex(regexEntry);
                    if (regex.IsMatch(seriesName))
                    {
                        if (seriesNameRegex[regexEntry].StartsWith("replace="))
                        {
                            searchSeries = regex.Replace(seriesName, seriesNameRegex[regexEntry].Substring(8));
                        }
                        else
                        {
                            searchSeries = seriesNameRegex[regexEntry];
                        }

                        log.DebugFormat("SD-TvDb: Regex mapping: series: {0} regex: {1} seriesMatch: {2}", seriesName, regexEntry, searchSeries);
                        break;
                    }
                }
            }

            List<TvdbSearchResult> searchResults = tvdbHandler.SearchSeries(searchSeries);

            log.DebugFormat("SD-TvDb: Search for {0} return {1} results", searchSeries, searchResults.Count);
            
            if (searchResults.Count >= 1)
            {
                for (int i = 0; i < searchResults.Count; i++)
                {
                    if (searchSeries.ToLower().Equals(searchResults[i].SeriesName.ToLower()))
                    {
                        var seriesId = searchResults[i].Id;
                        log.DebugFormat("SD-TvDb: series: {0} id: {1}", searchSeries, seriesId);
                        seriesCache.Add(seriesName, seriesId);
                        return seriesId;
                    }
                }

                log.DebugFormat("SD-TvDb: Could not find series match: {0} renamed {1}", seriesName, searchSeries);
            }

            log.DebugFormat("Cannot find series ID for {0}", seriesName);
            throw new NoSeriesMatchException();
        }

        private void ThrowIfSeriesIgnored(string seriesName)
        {
            if (this.seriesIgnore == null)
            {
                return;
            }

            if (this.seriesIgnore.Contains(seriesName))
            {
                this.log.DebugFormat("{0}: Series {1} is ignored", MODULE, seriesName);
                throw new SeriesIgnoredException();
            }
        }

        private bool IsSeriesInMappedSeries(string seriesName)
        {
            if (this.seriesNameMapping == null)
            {
                return false;
            }

            return this.seriesNameMapping.ContainsKey(seriesName);
        }
    }
}
