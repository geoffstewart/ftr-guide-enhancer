/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 06/10/2010
 * Time: 8:15 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;


namespace GuideEnricher
{
   /// <summary>
   /// Description of DataEnricherFactory.
   /// </summary>
   public sealed class DataEnricherFactory
   {
      private static DataEnricherFactory instance = new DataEnricherFactory();
      private static IDataEnricher guideDataEnricher = null;
      
      public static DataEnricherFactory Instance {
         get {
            return instance;
         }
      }
      
      private DataEnricherFactory()
      {
         // deliberately emptry for now
      }
      
      public IDataEnricher getGuideDataEnricher() {
         // for now, this is just for tvdb.com, but in future, 
         // other implementations may exist
         if (guideDataEnricher == null) {
            guideDataEnricher = new tvdb.TvDbDataEnricher();
         }
         return guideDataEnricher;
         
         
      }
   }
}
