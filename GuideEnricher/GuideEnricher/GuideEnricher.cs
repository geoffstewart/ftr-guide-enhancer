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

      ForTheRecordServiceAgent agent;
      
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
         
         Thread.Sleep(2000);

         try
         {
             ServerSettings serverSettings = new ServerSettings();
             serverSettings.ServerName = "localhost";
             serverSettings.Transport = ServiceTransport.NetTcp;
             serverSettings.Port = 49942;

             if (!ServiceChannelFactories.Initialize(serverSettings, false))
             {
                 Logger.Info("Unable to connect to ForTheRecordService, check your settings.");
                 
             }
             else
             {
                 Logger.Info("Connected to ForTheRecordService");
             

                 //         ftrl = new ForTheRecordListener();

                 ServiceHost sh = ForTheRecordListener.CreateServiceHost(Config.getProperty("serviceUrl"));

                 try
                 {
                     sh.Open();
                 }
                 catch (System.ServiceProcess.TimeoutException ex)
                 {
                     Logger.Error("Timeout on creating the ServiceHost", ex.Message);
                 }
                 catch (Exception ex)
                 {
                     Logger.Error("Error on creating ServiceHost:{0}{1}", Environment.NewLine, ex.Message);
                 }

                 agent = new ForTheRecordServiceAgent();
                 ForTheRecordEventGroup eventGroupsToListenTo = ForTheRecordEventGroup.ScheduleEvents;
                 agent.EnsureEventListener(eventGroupsToListenTo, Config.getProperty("serviceUrl"), Constants.EventListenerApiVersion);

             }

         }
         catch
         {

         }
         
         
      }
      
      /// <summary>
      /// Stop this service.
      /// </summary>
      protected override void OnStop()
      {
          if (agent != null)
          {
              agent.RemoveEventListener(Config.getProperty("serviceUrl"));
              agent.Dispose();
          }
      }
      
   }
}
