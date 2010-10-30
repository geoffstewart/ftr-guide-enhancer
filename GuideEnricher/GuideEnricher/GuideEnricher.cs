/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 28/09/2010
 * Time: 9:12 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
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
   /**
    * Class that defines the service start/stop behaviours
    */
   public class GuideEnricher : ServiceBase
   {
      public const string MyServiceName = "GuideEnricher";

      private string MODULE="GuideEnricher";

      public static EventWaitHandle waitHandle = new AutoResetEvent(false);
      
      private Thread worker = null;
      
      private bool workerLoop = true;
      
      public GuideEnricher()
      {
         InitializeComponent();
      }
      
      private void InitializeComponent()
      {
         this.ServiceName = MyServiceName;
         
         worker = new Thread(enrichGuideData);

         
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
         Thread.Sleep(10000);
         
         try
         {
            ServerSettings serverSettings = new ServerSettings();
            serverSettings.ServerName = Config.getProperty("ftrUrlHost");
            serverSettings.Transport = ServiceTransport.NetTcp;
            serverSettings.Port = Convert.ToInt32(Config.getProperty("ftrUrlPort"));
            string pass = Config.getProperty("ftrUrlPassword");
            
            if (pass != null && pass.Length > 0) {
               serverSettings.Password = pass;
            }

            if (!ServiceChannelFactories.Initialize(serverSettings, false))
            {
               Logger.Info("Unable to connect to ForTheRecordService, check your settings.");
               
            }
            else
            {
               Logger.Info("Connected to ForTheRecordService");
               
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
               
               // verify it's listening
               
               using (ForTheRecordServiceAgent agent = new ForTheRecordServiceAgent()) {
                  ForTheRecordEventGroup eventGroupsToListenTo = ForTheRecordEventGroup.ScheduleEvents | ForTheRecordEventGroup.GuideEvents;
                  agent.EnsureEventListener(eventGroupsToListenTo, Config.getProperty("serviceUrl"), Constants.EventListenerApiVersion);

               }
            }

         }
         catch (Exception ex) 
         {
            Logger.Error("Error on starting service: {0}(1)", Environment.NewLine,ex.Message);

         }
         
         // start worker thread
         worker.Start();
         
      }
      
      /// <summary>
      /// Stop this service.
      /// </summary>
      protected override void OnStop()
      {
         Logger.Info("Ending the worker thread...");
         workerLoop = false;
         waitHandle.Set();
         
      }
      
      /**
       * Main worker thread method
       * 
       * Loop that will find all upcoming recordings, enrich data, update in database
       * if the episode information changed
       */
      
      public void enrichGuideData() {
         IDataEnricher de = DataEnricherFactory.Instance.getGuideDataEnricher();
         
         while (workerLoop) {
            
            Logger.Info("Thread waiting for events...");
            
            // wait until the listener thread signals us to update the guide data
            waitHandle.WaitOne();
            if (!workerLoop) {
               break;
            }
            
            UpcomingRecording[] upcomingRecs = null;
            ArrayList enrichedPrograms = new ArrayList();
            
            using (TvControlServiceAgent tcsa = new TvControlServiceAgent()) {
               upcomingRecs = tcsa.GetAllUpcomingRecordings(UpcomingRecordingsFilter.Recordings,false);
            }
            
            ftrlog("Starting process to enrich guide data.  Processing " + Convert.ToString(upcomingRecs.Length) + " upcoming shows");
            
            Logger.Info("{0}: Starting the process to enrich guide data.  Processing {1} upcoming shows",
                        MODULE, Convert.ToString(upcomingRecs.Length));
            
            for (int i = 0; i < upcomingRecs.Length; i++) {
               UpcomingProgram upProg = upcomingRecs[i].Program;

               if (upProg.GuideProgramId != null)
               {
                  GuideProgram prog = null;
                  using (TvGuideServiceAgent tgsa = new TvGuideServiceAgent()) {
                     prog = tgsa.GetProgramById((Guid)upProg.GuideProgramId);
                  }

                  // enrich program
                  try {
                     string oldEpisodeNumber = prog.EpisodeNumberDisplay;
                     IProgramSummary updatedProgram = de.enrichProgram(prog);
                     
                     if (!prog.EpisodeNumberDisplay.Equals(oldEpisodeNumber)) {
                        // program was actually enriched
                        prog.EpisodeNumberDisplay = updatedProgram.EpisodeNumberDisplay;
                        enrichedPrograms.Add(prog);
                     }
                  } catch (Exception ex) {
                     Logger.Error("Error enriching program: {0}",ex.Message);
                  }


               }
               
            }
            // if any programs were enriched, do the import ot update in teh db
            if (enrichedPrograms.Count > 0) {
               using (TvGuideServiceAgent tgsa = new TvGuideServiceAgent()) {
                  Logger.Info("{0}: About to commit enriched guide data. {1} entries were enriched",
                              MODULE, Convert.ToString(enrichedPrograms.Count));
                  ftrlog("About to commit enriched guide data.  " + Convert.ToString(enrichedPrograms.Count) +
                     " entries were enriched.");
                  GuideProgram[] progArray = new GuideProgram[enrichedPrograms.Count];
                  for (int i = 0; i < enrichedPrograms.Count; i++) {
                     progArray[i] = (GuideProgram)enrichedPrograms[i];
                  }
                  tgsa.ImportPrograms(progArray, GuideSource.XmlTv);
               }
            } else {
               ftrlog("No programs were enriched");
               Logger.Info("{0}: No programs were enriched.",
                           MODULE);
            }
            
            Logger.Info("{0}: Done enriching guide data", MODULE);
            ftrlog("Done enriching guide data");

            
         }
      }

      public static void ftrlog(string message) {
         using (ForTheRecord.ServiceAgents.LogServiceAgent logAgent = new LogServiceAgent()) {
            logAgent.LogMessage("GuideEnricher",LogSeverity.Information,message);
         }
      }
      
   }
 
}
