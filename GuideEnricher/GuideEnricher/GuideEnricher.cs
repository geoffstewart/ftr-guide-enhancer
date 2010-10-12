/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 28/09/2010
 * Time: 9:12 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;

using ForTheRecord.ServiceAgents;
using ForTheRecord.ServiceContracts;
using ForTheRecord.Entities;
using ForTheRecord.Client.Common;

namespace GuideEnricher
{
   public class GuideEnricher : ServiceBase
   {
      public const string MyServiceName = "GuideEnricher";
      
      public GuideEnricher()
      {
         InitializeComponent();
      }
      
      private void InitializeComponent()
      {
         this.ServiceName = MyServiceName;
      }
      
      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      protected override void Dispose(bool disposing)
      {
         // TODO: Add cleanup code here (if required)
         base.Dispose(disposing);
      }
      
      /// <summary>
      /// Start this service.
      /// </summary>
      protected override void OnStart(string[] args)
      {
         
         Thread.Sleep(20000);
         
         ServerSettings serverSettings = new ServerSettings();
         serverSettings.ServerName = "localhost";
         serverSettings.Transport = ServiceTransport.NetTcp;
         serverSettings.Port = 49942;
         
         ServiceChannelFactories.Initialize(serverSettings, false);
         
//         ftrl = new ForTheRecordListener();

         ServiceHost sh = ForTheRecordListener.CreateServiceHost(Config.getProperty("serviceUrl"));
         
         sh.Open();
         

          using (ForTheRecordServiceAgent agent = new ForTheRecordServiceAgent())
                {
                    ForTheRecordEventGroup eventGroupsToListenTo = ForTheRecordEventGroup.ScheduleEvents;
                    agent.EnsureEventListener(eventGroupsToListenTo, Config.getProperty("serviceUrl") , Constants.EventListenerApiVersion);
                }
         
      }
      
      /// <summary>
      /// Stop this service.
      /// </summary>
      protected override void OnStop()
      {
      }
      
   }
}
