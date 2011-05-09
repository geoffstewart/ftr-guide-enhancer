/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 3/25/2010
 * Time: 8:47 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Text;
using System.Text.RegularExpressions;
using TvdbLib;
using TvdbLib.Cache;
using TvdbLib.Data;
using System.Collections.Generic;
using System.Collections;

namespace GuideEnricher.tvdb
{
    using System.Reflection;
    using log4net;

    /// <summary>
    /// Description of TvdbLibAccess.
    /// </summary>
    public class TvdbLibAccess
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private TvdbHandler tvdbHandler;
        private Dictionary<string, string> seriesNameMapping = new Dictionary<string, string>();
        private Dictionary<string, string> seriesNameRegex = new Dictionary<string, string>();
        private List<string> seriesIgnore = new List<string>();

        private Dictionary<string, string> seriesCache = new Dictionary<string, string>();

        private TvdbLanguage language = TvdbLanguage.DefaultLanguage;

        public static string IGNORED = "-IGNORED-"; // not likely a series will be called this

        public TvdbLibAccess()
        {
            init();
        }

        private void init()
        {
            string cache = Config.getProperty("TvDbLibCache");
            string tvdbid = "BBB734ABE146900D";  // mine, don't abuse it!!!
            tvdbHandler = new TvdbHandler(new XmlCacheProvider(cache), tvdbid);
            tvdbHandler.InitCache();

            seriesNameMapping = Config.getSeriesNameMap();
            seriesIgnore = Config.getIgnoredSeries();

            this.language = SetLanguage();

            // initialize any regex mappings
            foreach (string regex in seriesNameMapping.Keys)
            {
                if (regex.StartsWith("regex="))
                {
                    this.seriesNameRegex.Add(regex, seriesNameMapping[regex]);
                }
            }
        }

        private TvdbLanguage SetLanguage()
        {
            List<TvdbLanguage> availableLanguages = this.tvdbHandler.Languages;
            string selectedLanguage = Config.getProperty("TvDbLanguage");
            
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
            result = comp1_DirectMatch(episodeName, episodeList, SHORTMATCH);
            if (!String.IsNullOrEmpty(result)) return result;
            #endregion

            #region Compare Function 5 - Short
            result = comp2_RemovePunctuation(episodeName, episodeList, SHORTMATCH);
            if (!String.IsNullOrEmpty(result)) return result;
            #endregion
            #endregion

            #region Tail Matching Attempts

            #region Compare Function 6 - Tail
            result = comp1_DirectMatch(episodeName, episodeList, -SHORTMATCH);
            if (!String.IsNullOrEmpty(result)) return result;
            #endregion

            #region Compare Function 7 - Tail
            result = comp2_RemovePunctuation(episodeName, episodeList, -SHORTMATCH);
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
                    GuideEnricher.matchingSuccess(7);
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
                    GuideEnricher.matchingSuccess(8);
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
            return comp1_DirectMatch(episodeName, el, 99);
        }

        private string comp1_DirectMatch(string episodeName, List<TvdbEpisode> el, int range)
        {
            #region Allow for short/tail match in episode name
            if (range != 99)
            {
                if (range > 0)
                {
                    if (episodeName.Length >= range)
                    {
                        episodeName = episodeName.Substring(0, range);
                        log.DebugFormat("Compare function (4) - Short Match Test (First {0} Charcters of Episode Name)", range);
                    }
                    else
                    {
                        log.DebugFormat("Ineligible for Compare function (4): Episode name is shorter than {0} charachters.", range);
                        return string.Empty;
                    }
                }
                else if (episodeName.Length >= -range)
                {
                    episodeName = episodeName.Substring(episodeName.Length + range);
                    log.DebugFormat("Compare function (6) - Tail Match Test (Last {0} charcters of Episode Name)", -range);
                }
                else
                {
                    log.DebugFormat("Ineligible for Compare function (6): Episode name is shorter than {0} charachters", -range);
                    return string.Empty;
                }
            }
            #endregion

            foreach (TvdbEpisode e in el)
            {
                String tvdbEpisodeName = e.EpisodeName;
                #region Allow for short/tail mtch in database episode name
                if (range != 99)
                {
                    if (range > 0)
                    {
                        if (tvdbEpisodeName.Length >= range)
                        {
                            tvdbEpisodeName = tvdbEpisodeName.Substring(0, range);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (tvdbEpisodeName.Length >= -range)
                    {
                        tvdbEpisodeName = tvdbEpisodeName.Substring(tvdbEpisodeName.Length + range);
                    }
                    else
                    {
                        continue;
                    }
                }
                #endregion

                if (episodeName.Equals(tvdbEpisodeName) || episodeName.ToLower().Equals(tvdbEpisodeName.ToLower()))
                {
                    int sn = e.SeasonNumber;
                    int ep = e.EpisodeNumber;
                    string ret = FormatSeasonAndEpisode(sn, ep);

                    if (range != 99)
                    {
                        if (range > 0) GuideEnricher.matchingSuccess(3);
                        else GuideEnricher.matchingSuccess(5);
                    }
                    else GuideEnricher.matchingSuccess(0);

                    log.DebugFormat("Compare function success (Listing <==> Database): {0} <==> {1}, NOT ALTERED", episodeName, e.EpisodeName);
                    return ret;
                }
            }
            return string.Empty;
        }

        private string comp2_RemovePunctuation(string episodeName, List<TvdbEpisode> el)
        {
            return comp2_RemovePunctuation(episodeName, el, 99);
        }

        private string comp2_RemovePunctuation(string episodeName, List<TvdbEpisode> el, int range)
        {
            #region Allow for short/tail match in episode name
            if (range != 99)
            {
                if (range > 0)
                {
                    if (episodeName.Length >= range)
                    {
                        episodeName = episodeName.Substring(0, range);
                        log.DebugFormat("Compare function (5) - Short Match Test (First {0} Charcters of Episode Name).", range);
                    }
                    else
                    {
                        log.DebugFormat("Ineligible for Compare function (5): Episode name is shorter than {0} characters.", range);
                        return string.Empty;
                    }
                }
                else if (episodeName.Length >= -range)
                {
                    episodeName = episodeName.Substring(episodeName.Length + range);
                    log.DebugFormat("Compare function (7) - Tail Match Test (Last {0} characters of Episode Name)", -range);
                }
                else
                {
                    log.DebugFormat("Ineligible for Compare function (7): Episode name is shorter than {0} characters", -range);
                    return string.Empty;
                }
            }
            #endregion

            foreach (TvdbEpisode e in el)
            {
                log.DebugFormat("({0})", el.IndexOf(e));

                String tvdbEpisodeName = e.EpisodeName;
                #region Allow for short/tail match in database episode name
                if (range != 99)
                {
                    if (range > 0)
                    {
                        if (tvdbEpisodeName.Length >= range)
                        {
                            tvdbEpisodeName = tvdbEpisodeName.Substring(0, range);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (tvdbEpisodeName.Length >= -range)
                    {
                        tvdbEpisodeName = tvdbEpisodeName.Substring(episodeName.Length + range);
                    }
                    else
                    {
                        continue;
                    }
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
                    
                    if (range != 99)
                    {
                        if (range > 0) GuideEnricher.matchingSuccess(4);
                        else GuideEnricher.matchingSuccess(6);                
                    }
                    else GuideEnricher.matchingSuccess(1);

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
                    GuideEnricher.matchingSuccess(2);
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
