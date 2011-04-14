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

using GuideEnricher.exception;

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
      
      private Thread sleeper = null;
      
      private bool workerLoop = true;
      
      public GuideEnricher()
      {
         InitializeComponent();
      }
      
      private void InitializeComponent()
      {
         this.ServiceName = MyServiceName;
         
         worker = new Thread(enrichGuideData);

         sleeper = new Thread(enrichTimer);
         
      }
      
      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      protected override void Dispose(bool disposing)
      {
         base.Dispose(disposing);
      }
      
      /// <summary>
      /// Start this service.
      /// </summary>
      protected override void OnStart(string[] args)
      {
//         Thread.Sleep(10000);
         
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

            Logger.Info("Just about to call ServiceChannelFactories.Initialize()");
            
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
         
         ftrlog("Starting GuideEnricher...");
         
         // start worker thread
         worker.Start();
         sleeper.Start();
      }
      
      /// <summary>
      /// Stop this service.
      /// </summary>
      protected override void OnStop()
      {
         Logger.Info("Ending the worker thread...");
         workerLoop = false;
         waitHandle.Set();
         sleeper.Abort();
         ftrlog("Stopping the GuideEnricher");
         
      }
      
      /**
       * Main worker thread method
       * 
       * Loop that will find all upcoming recordings, enrich data, update in database
       * if the episode information changed
       */
      
      public void enrichGuideData() {
         try {
         
            while (workerLoop) {
               
               Logger.Info("Thread waiting for events...");
               
               // wait until the listener thread signals us to update the guide data
               waitHandle.WaitOne();
               if (!workerLoop) {
                  break;
               }
               
               UpcomingRecording[] upcomingRecs = null;
               Hashtable enrichedPrograms = new Hashtable();
               
               using (TvControlServiceAgent tcsa = new TvControlServiceAgent()) {
                  upcomingRecs = tcsa.GetAllUpcomingRecordings(UpcomingRecordingsFilter.Recordings,false);
               }
               
               ftrlog("Starting process to enrich guide data.  Processing " + Convert.ToString(upcomingRecs.Length) + " upcoming shows");
               
               Logger.Info("{0}: Starting the process to enrich guide data.  Processing {1} upcoming shows",
                           MODULE, Convert.ToString(upcomingRecs.Length));
               
               // lists to keep track of failed searches for series and episodes
               // use these to prevent repeated searches that you know will fail
               ArrayList noSeriesMatchList = new ArrayList();
               ArrayList noEpisodeMatchList = new ArrayList();
               
               for (int i = 0; i < upcomingRecs.Length; i++) {
                  UpcomingProgram upProg = upcomingRecs[i].Program;
   
                  if (upProg.GuideProgramId != null)
                  {
                     GuideProgram prog = null;
                     using (TvGuideServiceAgent tgsa = new TvGuideServiceAgent()) {
                        prog = tgsa.GetProgramById((Guid)upProg.GuideProgramId);
                     }
   
                     try {
                        if (noSeriesMatchList.Contains(prog.Title) ||
                            noEpisodeMatchList.Contains(prog.Title + "-" + prog.SubTitle)) {
                           // already failed on this, skip it=
                           continue;
                        }
                          
                        prog = enrichProgram(prog);
                        
                     } catch (NoSeriesMatchException) {
                        noSeriesMatchList.Add(prog.Title);
                        prog = null;
                     } catch (NoEpisodeMatchException) {
                        noEpisodeMatchList.Add(prog.Title + "-" + prog.SubTitle);
                        prog = null;
                     } catch (DataEnricherException) {
                        prog = null;
                        // ignore other types for now
                     }
                     
                     if (prog != null) {
                        enrichedPrograms = addToEnrichedPrograms(enrichedPrograms, prog);
                        
                        // find other programs that are part of this schedule and enrich them
                        // too so they have the same episode information
                        // otherwise, FTR thinks they are different and needs to record them, 
                        // often times creating a conflict.
                        Schedule sked = null;
                        UpcomingProgram[] upPrograms = null;
                        using (TvSchedulerServiceAgent tssa = new TvSchedulerServiceAgent()) {
                           sked = tssa.GetScheduleById(upProg.ScheduleId);
                           if (sked != null) {
                              upPrograms = tssa.GetUpcomingPrograms(sked,true);
                           }
                        }
                        if (upPrograms != null) {
                           for(int j = 0; j < upPrograms.Length; j++) {
                              UpcomingProgram upProg2 = upPrograms[j];
                              
                              GuideProgram prog2 = null;
                              using (TvGuideServiceAgent tgsa = new TvGuideServiceAgent()) {
                                 prog2 = tgsa.GetProgramById((Guid)upProg2.GuideProgramId);
                              }
                              try {
                                 prog2 = enrichProgram(prog2);
                        
                              } catch (NoSeriesMatchException) {
                                 noSeriesMatchList.Add(prog2.Title);
                                 prog = null;
                              } catch (NoEpisodeMatchException) {
                                 noEpisodeMatchList.Add(prog2.Title + "-" + prog2.SubTitle);
                                 prog = null;
                              } catch (DataEnricherException) {
                                 prog2 = null;
                                 // ignore other types for now
                              }
                              if (prog2 != null) {
                                 enrichedPrograms = addToEnrichedPrograms(enrichedPrograms,prog2);
                              }
                           }
                        }
                        
                       
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
                     int i = 0;
                     foreach (GuideProgram gp in enrichedPrograms.Values) {
                        progArray[i] = (GuideProgram)gp;
                        i++;
                     }
                     tgsa.ImportPrograms(progArray, GuideSource.XmlTv);
                  }
               } else {
                  ftrlog("No programs were enriched");
                  Logger.Info("{0}: No programs were enriched.",
                              MODULE);
               }
               
               if (noSeriesMatchList.Count > 0 || noEpisodeMatchList.Count > 0) {
                  ftrlog("There were " + Convert.ToString(noSeriesMatchList.Count) + " series that could not be matched and " +
                         Convert.ToString(noEpisodeMatchList.Count) + " episodes that could not be found.  See system log for details");
               }
               string s = "";
               foreach(string series in noSeriesMatchList) {
                  s += series + ", ";
               }
               string e = "";
               foreach(string ep in noEpisodeMatchList) {
                  e += ep + ", ";
               }
                  
               // keep log messages less than 32722 characters or the windows logger will barf
               if (s.Length > 32000) {
                  s = s.Substring(0,32000);
               }
               if (e.Length > 32000) {
                  e = e.Substring(0,32000);
               }
               Logger.Info("The following series could not be matched: {0}", s);
               Logger.Info("The following episodes could not be matched: {0}",e);
               Logger.Info("{0}: Done enriching guide data", MODULE);
               ftrlog("Done enriching guide data");
   
               
            }
         } catch (Exception topEx) {
            Logger.Error("The main loop for GuideEnricher received this exception:\n{0}\n{1}", topEx.Message,topEx.StackTrace);
            
         }
      }

      private Hashtable addToEnrichedPrograms(Hashtable existingTable, GuideProgram p) {
         try {
            existingTable.Add(p.GuideProgramId, p);
            
         } catch (ArgumentException) {
            // swallow it if it's already there.
         }
         return existingTable;
      }
      private GuideProgram enrichProgram(GuideProgram prog) {
         IDataEnricher de = DataEnricherFactory.Instance.getGuideDataEnricher();
         // enrich program
         try {
            string oldEpisodeNumber = prog.EpisodeNumberDisplay;
            GuideProgram updatedProgram = (GuideProgram)de.enrichProgram(prog);
            
            if (!prog.EpisodeNumberDisplay.Equals(oldEpisodeNumber)  ||
                prog.EpisodeNumber != updatedProgram.EpisodeNumber ||
                prog.SeriesNumber != updatedProgram.SeriesNumber) {
               // program was actually enriched
               prog.EpisodeNumberDisplay = updatedProgram.EpisodeNumberDisplay;
               
               // update the series/episode number fields too
               try {
                  string sep = prog.EpisodeNumberDisplay;
                  if (sep.Length == 6) {
                     int epInt = Convert.ToInt32(sep.Substring(4));
                     int seasonInt = Convert.ToInt32(sep.Substring(1,2));
                     prog.EpisodeNumber = epInt;
                     prog.SeriesNumber = seasonInt;
                  }
               } catch (Exception ex) {
                  // couldn't convert ep nunmber to ints
                  Logger.Warning("Error while converting season/ep to integers: {0}",ex.Message);
               }
               
               return prog;
            }
         } catch (DataEnricherException dee) {
            // expected errors get thrown silently
            throw dee;
         } catch (Exception ex) {
            // unexpected error, swallow, log and move on
            Logger.Warning("Error enriching program: {0}",ex.Message);
         }
         // nothing was enriched
         return null;
      }
      
      public static void ftrlog(string message) {
         using (ForTheRecord.ServiceAgents.LogServiceAgent logAgent = new LogServiceAgent()) {
            logAgent.LogMessage("GuideEnricher",LogSeverity.Information,message);
         }
      }
      
      public void enrichTimer() {
         string waittime = Config.getProperty("sleepTimeInHours");
         if (waittime == null) {
            waittime = "12";
         }
         if ("0".Equals(waittime)) {
            // sleeper thread is disabled
            ftrlog("Sleeper thread is disabled");
            return;
         }
         int waittimeHoursInt = Convert.ToInt32(waittime);
         int waittimeInt = waittimeHoursInt * 60 * 60 * 1000;
         
         while(workerLoop) {
            ftrlog("The wait thread will wait for " + waittime + " hours");
            Thread.Sleep(waittimeInt);
            ftrlog("Wait thread is awake and calling enrichGuideData...");
            waitHandle.Set();
         }
      }
      
   }
 
}
