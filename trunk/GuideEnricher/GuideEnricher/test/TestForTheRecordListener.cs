/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 13/10/2010
 * Time: 1:11 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using NUnit.Framework;

using ForTheRecord.ServiceAgents;
using ForTheRecord.Entities;

namespace GuideEnricher.test
{
   [TestFixture]
   public class TestForTheRecordListener
   {
      [Test]
      public void TestMethod()
      {
         ServerSettings serverSettings = new ServerSettings();
         serverSettings.ServerName = "localhost";
         serverSettings.Transport = ServiceTransport.NetTcp;
         serverSettings.Port = 49942;

         ServiceChannelFactories.Initialize(serverSettings, false);
         
         ForTheRecordListener ftrl = new ForTheRecordListener();
         ftrl.UpcomingRecordingsChanged();
      }
   }
}
