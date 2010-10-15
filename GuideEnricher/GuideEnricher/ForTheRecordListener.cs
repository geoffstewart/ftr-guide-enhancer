/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 11/10/2010
 * Time: 1:27 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Collections;

using ForTheRecord.ServiceContracts.Events;
using ForTheRecord.Client.Common;
using ForTheRecord.Entities;
using ForTheRecord.ServiceAgents;

namespace GuideEnricher
{
   /// <summary>
   /// Description of ForTheRecordListener.
   /// </summary>
   [ServiceBehavior(
      ConcurrencyMode = ConcurrencyMode.Single,
      InstanceContextMode = InstanceContextMode.Single)]
   public class ForTheRecordListener : EventsListenerServiceBase
   {
      private static string MODULE = "GuideEnricherListener";
      protected IDataEnricher de;
      
      public ForTheRecordListener()
      {
         DataEnricherFactory def = DataEnricherFactory.Instance;
         de = def.getGuideDataEnricher();
                       
         
         
         
      }
      
      public static ServiceHost CreateServiceHost(string eventsServiceBaseUrl)
      {
         ServiceHost sh = CreateServiceHost(typeof(ForTheRecordListener), eventsServiceBaseUrl, typeof(IGuideEventsListener),
                                  typeof(IScheduleEventsListener));
         
         return sh;
      }
      
      public override void NewGuideData()
      {
         Logger.Verbose("{0}: Handle NewGuideData event",MODULE);
         enrichGuideData();
      }
      
      public override void UpcomingRecordingsChanged()
      {
         Logger.Verbose("{0}: Handle UpcomingRecordingsChanged event",MODULE);
         enrichGuideData();
      }

      public override void UpcomingSuggestionsChanged()
      {
         Logger.Verbose("{0}: Handle UpcomingSuggestionsChanged event",MODULE);
         enrichGuideData();
      }

      public override void ScheduleChanged(Guid scheduleId)
      {
         Logger.Verbose("{0}: Handle ScheduleChanged event",MODULE);
         enrichGuideData();
      }
      
      protected void enrichGuideData() {
         UpcomingRecording[] upcomingRecs = null;
         ArrayList enrichedPrograms = new ArrayList();
         
         using (TvControlServiceAgent tcsa = new TvControlServiceAgent()) {
            upcomingRecs = tcsa.GetAllUpcomingRecordings(UpcomingRecordingsFilter.Recordings,false);
         }
         
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
               GuideProgram[] progArray = new GuideProgram[enrichedPrograms.Count];
               for (int i = 0; i < enrichedPrograms.Count; i++) {
                  progArray[i] = (GuideProgram)enrichedPrograms[i];
               }
               tgsa.ImportPrograms(progArray, GuideSource.XmlTv);
            }
         } else {
             Logger.Info("{0}: No programs were enriched.",
                        MODULE);
         }
         
         Logger.Info("{0}: Done enriching guide data", MODULE);
      }
   
   }
}
