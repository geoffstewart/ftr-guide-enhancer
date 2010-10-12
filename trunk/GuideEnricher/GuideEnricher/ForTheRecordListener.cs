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

using ForTheRecord.ServiceContracts.Events;
using ForTheRecord.Client.Common;
using ForTheRecord.Common.Logging;
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
         ForTheRecord.ServiceAgents.TvGuideServiceAgent tgsa = new ForTheRecord.ServiceAgents.TvGuideServiceAgent();
         ForTheRecord.ServiceAgents.TvSchedulerServiceAgent tssa = new ForTheRecord.ServiceAgents.TvSchedulerServiceAgent();
         TvControlServiceAgent tcsa = new TvControlServiceAgent();
         
         UpcomingRecording[] recs = tcsa.GetAllUpcomingRecordings(UpcomingRecordingsFilter.Recordings,false);
         
         for (int i = 0; i < recs.Length; i++) {
            UpcomingProgram upProg = recs[i].Program;
            
            GuideProgram prog = tgsa.GetProgramById(upProg.UpcomingProgramId);
            
            // enrich program
            IProgramSummary updatedProgram = de.enrichProgram(prog);
            
            prog.EpisodeNumberDisplay = updatedProgram.EpisodeNumberDisplay;
            
            // update program in guide
            
            tgsa.ImportProgram(prog, GuideSource.Other);
                        
              
         }
      }

   }
}
