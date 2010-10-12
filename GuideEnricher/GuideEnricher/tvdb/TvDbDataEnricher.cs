/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 06/10/2010
 * Time: 8:03 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using GuideEnricher;
using ForTheRecord.Common.Logging;

namespace GuideEnricher.tvdb
{
   /// <summary>
   /// Description of TvDbDataEnricher.
   /// </summary>
   public class TvDbDataEnricher : DataEnricherImpl, IDataEnricher
   {
      private static string MODULE = "TvDbEnricher";
      protected TvdbLibAccess tvdblib;
      
      public TvDbDataEnricher()
      {
         tvdblib = new TvdbLibAccess();
         
      }
      
      public void close() {
         tvdblib.closeCache();
      }
      
      public ForTheRecord.Entities.IProgramSummary enrichProgram(ForTheRecord.Entities.IProgramSummary existingProgram)
      {
         ForTheRecord.Entities.IProgramSummary retProgram = existingProgram;
         
         string seriesName = retProgram.Title;
         string episodeName = retProgram.SubTitle;
         
         Logger.Verbose("{0}: Starting lookup for {1} - {2}", MODULE, seriesName, episodeName);
         
         string seriesId = tvdblib.getSeriesId(seriesName);
         
         if (seriesId.Length == 0) {
            Logger.Info("{0}: Cannot find series ID for {1}", MODULE, seriesName);
            return retProgram;
         }
         
         string episodeNumber = tvdblib.getSeasonEpisode(seriesName, seriesId, episodeName, true);
         
         if ("".Equals(episodeNumber)) {
            Logger.Verbose("{0}: Found no matches for {1} - {2}",MODULE, seriesName, episodeName);
         } else {
            Logger.Verbose("{0}: Found match for {1} - {2}: {3}",MODULE, seriesName, episodeName, episodeNumber);
            
            retProgram.EpisodeNumberDisplay = episodeNumber;
         }
         
         return retProgram;
         
      }
   }
}
