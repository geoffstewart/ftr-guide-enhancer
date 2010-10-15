/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 06/10/2010
 * Time: 8:33 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Configuration;

namespace GuideEnricher
{
   /// <summary>
   /// Description of Config.
   /// </summary>
   public class Config
   {
      public Config()
      {
         
      }
      public static string getProperty(string propName) {
         
         //         string applicationName =
         //         string exePath = System.IO.Path.Combine(
         //               Environment.CurrentDirectory, applicationName);
         //
         //         System.Configuration.Configuration conf = ConfigurationManager.OpenExeConfiguration(exePath);
         if ("TvDbLibCache".Equals(propName)) {
            return "C:\\tvdblibcache\\";
         }
         if ("serviceUrl".Equals(propName)) {
            return "net.tcp://localhost:49830/GuideEnricher";
         }
         return "";
      }
      public static void setProperty(string propName, string propValue) {
      }
      
      public static IDictionary getMapProperty(string propName) {
         Hashtable map = new Hashtable();
         map.Add("American Dad","American Dad!");
         
         if ("TvDbSeriesMappings".Equals(propName)) {
            return map;
         }
         return new Hashtable();
      }
      public static void setMapProperty(string propName, IDictionary propValues) {
         
      }
   }
}
