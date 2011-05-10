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
    using System.Text;
    using System.Text.RegularExpressions;
    using log4net;
    using TvdbLib;
    using TvdbLib.Cache;
    using TvdbLib.Data;

    /// <summary>
    /// Description of TvdbLibAccess.
    /// </summary>
    public class TvdbLibAccess
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IConfiguration config;
        private const int cnstIntRange = 20;

        private TvdbHandler tvdbHandler;
        private Dictionary<string, string> seriesNameMapping;
        private Dictionary<string, string> seriesNameRegex = new Dictionary<string, string>();
        private List<string> seriesIgnore = new List<string>();

        private Dictionary<string, string> seriesCache = new Dictionary<string, string>();

        private TvdbLanguage language = TvdbLanguage.DefaultLanguage;

        public static string IGNORED = "-IGNORED-"; // not likely a series will be called this

        private enum tailOrShort { Regular, Short, Tail };

        public TvdbLibAccess(IConfiguration configuration)
        {
            this.config = configuration;
            init();
        }

        private void init()
        {
            string cache = this.config.getProperty("TvDbLibCache");
            string tvdbid = "BBB734ABE146900D";  // mine, don't abuse it!!!
            tvdbHandler = new TvdbHandler(new XmlCacheProvider(cache), tvdbid);
            tvdbHandler.InitCache();

            seriesNameMapping = this.config.getSeriesNameMap();
            seriesIgnore = this.config.getIgnoredSeries();

            this.language = SetLanguage();

            this.IntializeRegexMappings();
        }

        private void IntializeRegexMappings()
        {
            if (this.seriesNameMapping == null)
            {
                return;
            }

            foreach (string regex in this.seriesNameMapping.Keys)
            {
                if (regex.StartsWith("regex="))
                {
                    this.seriesNameRegex.Add(regex, this.seriesNameMapping[regex]);
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

        public void closeCache()
        {
            tvdbHandler.CloseCache();
        }

        public string getSeriesId(string seriesName)
        {
            string searchSeries = seriesName;

            // check cache first
            if (seriesCache.ContainsKey(seriesName))
            {
                log.DebugFormat("SD-TvDb: Series cache hit: {0} has id: {1}", seriesName, seriesCache[seriesName]);
                return seriesCache[seriesName];
            }

            if (seriesNameMapping.ContainsKey(seriesName))
            {
                if (this.seriesIgnore.Contains(seriesName))
                {
                    return IGNORED;
                }
                searchSeries = this.seriesNameMapping[seriesName];
            }
            else if (this.seriesNameRegex.Count > 0)
            {
                // compare the incoming seriesName to see if it matches a regex mapping
                foreach (string regexEntry in seriesNameRegex.Keys)
                {
                    // get rid of regex=
                    string regex = regexEntry.Substring(6);
                    Regex re = new Regex(regex);
                    if (re.IsMatch(seriesName))
                    {
                        if (this.seriesIgnore.Contains(regexEntry))
                        {
                            return IGNORED;
                        }
                        searchSeries = seriesNameRegex[regexEntry];
                        log.DebugFormat("SD-TvDb: Regex mapping: series: {0} regex: {1} seriesMatch: {2}", seriesName, regex, searchSeries);
                        
                        break;
                    }
                }
            }
            if (searchSeries.StartsWith("id="))
            {
                // we're doing a direct mapping from series to tvdb.com id
                string seriesid = searchSeries.Substring(3);
                log.DebugFormat("SD-TvDb: Direct mapping: series: {0} id: {1}", seriesName, seriesid);
                return seriesid;
            }

            List<TvdbSearchResult> l = tvdbHandler.SearchSeries(searchSeries);

            log.DebugFormat("SD-TvDb: Search for {0} return {1} results", searchSeries, l.Count);
            
            if (l.Count >= 1)
            {
                for (int i = 0; i < l.Count; i++)
                {
                    //Log.WriteFile("seriesin: " + seriesName.ToLower() + "   result: " + l[0].SeriesName.ToLower());
                    if (searchSeries.ToLower().Equals(l[i].SeriesName.ToLower()))
                    {
                        string id = "" + l[i].Id;
                        log.DebugFormat("SD-TvDb: series: {0} id: {1}", searchSeries, id);
                        // add to cache
                        seriesCache.Add(seriesName, id);

                        return id;
                    }
                }

                log.DebugFormat("SD-TvDb: Could not find series match: {0} renamed {1}", seriesName, searchSeries);
            }
            
            return string.Empty;
        }

        public string getSeasonEpisode(string seriesName, string seriesId, string episodeName)
        {
            return getSeasonEpisode(seriesName, seriesId, episodeName, false);
        }

        public string getSeasonEpisode(string seriesName, string seriesId, string episodeName, bool recurseCall)
        {
            if (string.IsNullOrEmpty(seriesId))
            {
                return string.Empty;
            }
            
		    TvdbSeries s = tvdbHandler.GetSeries(Convert.ToInt32(seriesId), language, true, false, false);
            List<TvdbEpisode> episodeList = s.Episodes;

            // TODO: Look at this logic
            if (!recurseCall)
            {
                // We shouldn't need so much logging.  Uncomment just for debugging before release.
//                foreach (TvdbEpisode e in episodeList)
//                {
//                    log.DebugFormat("({0}) : {1} =?= {2}", episodeList.IndexOf(e), episodeName, e.EpisodeName);
//                }
            } 

            #region Compare Function 1
            string result = this.comp1_DirectMatch(episodeName, episodeList);
            if (!String.IsNullOrEmpty(result))
            {
                log.Debug("Matched using Direct Episode Match [1]");
                return result;
            }
            #endregion

            #region Compare Function 2
            result = comp2_RemovePunctuation(episodeName, episodeList);
            if (!String.IsNullOrEmpty(result))
            {
                log.Debug("Matched using Punctuation Removed Test [2]");
                return result;
            }
            #endregion

            #region Compare Function 3
            result =  compX_RegExStripper(episodeName, episodeList);
            if (!String.IsNullOrEmpty(result))
            {
                log.Debug("Matched using Regex Cleanup [3]");
                return result;
            }
            #endregion

            #region Short Matching Attempts

            int SHORTMATCH = 20;

            #region Compare Function 4 - Short
            result = comp1_DirectMatch(episodeName, episodeList, tailOrShort.Regular);
            if (!String.IsNullOrEmpty(result)) return result;
            #endregion

            #region Compare Function 5 - Short
            result = comp2_RemovePunctuation(episodeName, episodeList, tailOrShort.Regular);
            if (!String.IsNullOrEmpty(result)) return result;
            #endregion
            #endregion

            #region Tail Matching Attempts

            #region Compare Function 6 - Tail
            result = comp1_DirectMatch(episodeName, episodeList, tailOrShort.Regular);
            if (!String.IsNullOrEmpty(result)) return result;
            #endregion

            #region Compare Function 7 - Tail
            result = comp2_RemovePunctuation(episodeName, episodeList, tailOrShort.Regular);
            if (!String.IsNullOrEmpty(result)) return result;
            #endregion 
            #endregion

            #region Multiple episode search 7
            // check for multiple episodes... assume separated by ;
            if (!recurseCall && episodeName.Contains(";"))
            {
                string firstEpisode = episodeName.Substring(0, episodeName.IndexOf(';'));
                result = getSeasonEpisode(seriesName, seriesId, firstEpisode, true);
                if (!String.IsNullOrEmpty(result))
                {
                    log.Debug("Matched using recurse [7]");
                    GuideEnricherService.matchingSuccess(7);
                    return result;
                }
            }
            else
            {
                log.Debug("Compare function (9) skipped - Split episode not detected!");
            }
            #endregion

            #region Forced update recursion 8
            if (!recurseCall)
            {
                log.Debug("Compare function (8) - All tests failed. Recursing after a forced theTvDb update.");
                TvdbSeries tvdbSeries = this.tvdbHandler.GetFullSeries(Convert.ToInt32(seriesId), language, false);
                this.tvdbHandler.ForceReload(tvdbSeries, true, true, false);
                result = getSeasonEpisode(seriesName, seriesId, episodeName, true);
                if (!String.IsNullOrEmpty(result))
                {
                    GuideEnricherService.matchingSuccess(8);
                    return result;
                }
                
                log.DebugFormat("No episode match for series: {0}, seriesId: {1}, episodeName: {2}", seriesName, seriesId, episodeName);
            }
            #endregion

            // still can't match.. return nothing
            return string.Empty;
        }

        private string comp1_DirectMatch(string episodeName, List<TvdbEpisode> el)
        {
            return comp1_DirectMatch(episodeName, el, tailOrShort.Regular);
        }

        private string comp1_DirectMatch(string episodeName, List<TvdbEpisode> el, tailOrShort limiter)
        {
            #region Allow for short/tail match in episode name
            if (!limiter.Equals(tailOrShort.Regular))
            {
                if (episodeName.Length >= cnstIntRange)
                {
                    if (limiter.Equals(tailOrShort.Short))
                    {
                        episodeName = episodeName.Substring(0, cnstIntRange);
                        log.DebugFormat("Compare function (4) - Short Match Test (First {0} Charcters of Episode Name)", cnstIntRange);
                    }
                    else
                    {
                        log.DebugFormat("Ineligible for Compare function (4): Episode name is shorter than {0} charachters.", cnstIntRange);
                        return string.Empty;
                    }
                }
                else if (limiter.Equals(tailOrShort.Tail))
                {
                    episodeName = episodeName.Substring(episodeName.Length + cnstIntRange);
                    log.DebugFormat("Compare function (6) - Tail Match Test (Last {0} charcters of Episode Name)", cnstIntRange);
                }
                else
                {
                    log.DebugFormat("Ineligible for Compare function (6): Episode name is shorter than {0} charachters", cnstIntRange);
                    return string.Empty;
                }
            }
            #endregion

            foreach (TvdbEpisode e in el)
            {
                String tvdbEpisodeName = e.EpisodeName;
                #region Allow for short/tail mtch in database episode name
                if (!limiter.Equals(tailOrShort.Regular))
                {
                    if (tvdbEpisodeName.Length >= cnstIntRange)
                    {
                        if (limiter.Equals(tailOrShort.Short))
                        {
                            tvdbEpisodeName = tvdbEpisodeName.Substring(0, cnstIntRange);
                            log.DebugFormat("Compare function (4) - Short Match Test (First {0} Charcters of Episode Name)", cnstIntRange);
                        }
                        else if (limiter.Equals(tailOrShort.Tail))
                        {
                            tvdbEpisodeName = tvdbEpisodeName.Substring(episodeName.Length - cnstIntRange);
                            log.DebugFormat("Compare function (6) - Tail Match Test (Last {0} charcters of Episode Name)", cnstIntRange);
                        }
                    }
                    else if (limiter.Equals(tailOrShort.Short) || limiter.Equals(tailOrShort.Tail)) continue;
                }
                #endregion

                if (episodeName.Equals(tvdbEpisodeName) || episodeName.ToLower().Equals(tvdbEpisodeName.ToLower()))
                {
                    int sn = e.SeasonNumber;
                    int ep = e.EpisodeNumber;
                    string ret = FormatSeasonAndEpisode(sn, ep);

                    if (limiter.Equals(tailOrShort.Regular)) GuideEnricherService.matchingSuccess(0);
                    else if (limiter.Equals(tailOrShort.Short)) GuideEnricherService.matchingSuccess(3);
                    else GuideEnricherService.matchingSuccess(5);

                    log.DebugFormat("Compare function success (Listing <==> Database): {0} <==> {1}, NOT ALTERED", episodeName, e.EpisodeName);
                    return ret;
                }
            }
            return string.Empty;
        }

        private string comp2_RemovePunctuation(string episodeName, List<TvdbEpisode> el)
        {
            return comp2_RemovePunctuation(episodeName, el, tailOrShort.Regular);
        }

        private string comp2_RemovePunctuation(string episodeName, List<TvdbEpisode> el, tailOrShort limiter)
        {
            #region Allow for short/tail match in episode name
            if (!limiter.Equals(tailOrShort.Regular))
            {
                if (episodeName.Length >= cnstIntRange)
                {
                    if (limiter.Equals(tailOrShort.Short))
                    {
                        episodeName = episodeName.Substring(0, cnstIntRange);
                        log.DebugFormat("Compare function (5) - Short Match Test (First {0} Charcters of Episode Name)", cnstIntRange);
                    }
                    if (limiter.Equals(tailOrShort.Tail))
                    {
                        episodeName = episodeName.Substring(episodeName.Length - cnstIntRange);
                        log.DebugFormat("Compare function (7) - Tail Match Test (Last {0} charcters of Episode Name)", cnstIntRange);
                    }
                }
                else if (limiter.Equals(tailOrShort.Short))
                {
                    log.DebugFormat("Ineligible for Compare function (5): Episode name is shorter than {0} charachters.", cnstIntRange);
                    return string.Empty;
                }
                else if (limiter.Equals(tailOrShort.Tail))
                {
                    log.DebugFormat("Ineligible for Compare function (7): Episode name is shorter than {0} charachters.", cnstIntRange);
                    return string.Empty;
                }
            }
            #endregion

            foreach (TvdbEpisode e in el)
            {
                log.DebugFormat("({0})", el.IndexOf(e));

                String tvdbEpisodeName = e.EpisodeName;
                #region Allow for short/tail match in database episode name
                if (!limiter.Equals(tailOrShort.Regular))
                {
                    if (tvdbEpisodeName.Length >= cnstIntRange)
                    {
                        if (limiter.Equals(tailOrShort.Short))
                        {
                            tvdbEpisodeName = tvdbEpisodeName.Substring(0, cnstIntRange);
                        }
                        if (limiter.Equals(tailOrShort.Tail))
                        {
                            tvdbEpisodeName = tvdbEpisodeName.Substring(episodeName.Length - cnstIntRange);
                        }
                    }
                    else if (limiter.Equals(tailOrShort.Short) || limiter.Equals(tailOrShort.Tail)) continue;
                }
                #endregion

                // remove all punctuation
                string e1 = removePunctuation(episodeName);
                string e2 = removePunctuation(tvdbEpisodeName);

                if (e1.Equals(e2) || e1.ToLower().Equals(e2.ToLower()))
                {
                    int sn = e.SeasonNumber;
                    int ep = e.EpisodeNumber;
                    string ret = FormatSeasonAndEpisode(sn, ep);
                    
                    if (limiter.Equals(tailOrShort.Regular)) GuideEnricherService.matchingSuccess(1);
                    else if (limiter.Equals(tailOrShort.Short)) GuideEnricherService.matchingSuccess(4);
                    else GuideEnricherService.matchingSuccess(6);

                    log.DebugFormat("Compare function success (Listing <==> Database): {0} <==> {1}, Altered String Match {2} <==> {3}", episodeName, e.EpisodeName, e1, e2);
                    return ret;
                }
            }

            return string.Empty;
        }       

        private string compX_RegExStripper(string episodeName, List<TvdbEpisode> el)
        {
            foreach (TvdbEpisode e in el)
            {
                log.DebugFormat("({0})", el.IndexOf(e));

                String desperateEpisode1 = episodeName.ToLower();
                String desperateEpisode2 = e.EpisodeName.ToLower();

                //cleanup episode from listing
                desperateEpisode1 = Regex.Replace(desperateEpisode1, @"1|2|3|4|5|6|7|8|9|0|\(|\)|&|part|in|,|is|and|the|I|X|V|-|%|percent| ", string.Empty, RegexOptions.IgnoreCase);
                desperateEpisode2 = Regex.Replace(desperateEpisode2, @"1|2|3|4|5|6|7|8|9|0|\(|\)|&|part|in|,|is|and|the|I|X|V|-|%|percent| ", string.Empty, RegexOptions.IgnoreCase);
                if (desperateEpisode1.Equals(desperateEpisode2))
                {
                    int sn = e.SeasonNumber;
                    int ep = e.EpisodeNumber;
                    string ret = FormatSeasonAndEpisode(sn, ep);
                    GuideEnricherService.matchingSuccess(2);
                    log.DebugFormat("Compare function (3) Success (Listing <==> Database): {0}  <==> {1}, Altered string Match {2} <==> {3}", episodeName, e.EpisodeName, desperateEpisode1, desperateEpisode2);
                    return ret;
                }
            } 

            return null;
        }

        private static string FormatSeasonAndEpisode(int season, int episode)
        {
            return string.Format("S{0:00}E{1:00}", season, episode);
        }

        private string removePunctuation(string inString)
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < inString.Length; i++)
            {

                if(Char.IsLetterOrDigit(inString[i]))
                {
                    sb = sb.Append(inString[i]);
                }
            }

            return sb.ToString();
        }
    }
}
