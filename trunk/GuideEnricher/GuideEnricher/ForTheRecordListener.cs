/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 11/10/2010
 * Time: 1:27 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;

using ForTheRecord.Client.Common;
using ForTheRecord.Entities;
using ForTheRecord.ServiceAgents;
using ForTheRecord.ServiceContracts.Events;

namespace GuideEnricher
{
   /// <summary>
   /// Description of ForTheRecordListener.
   /// </summary>
   [ServiceBehavior(
      ConcurrencyMode = ConcurrencyMode.Multiple,
      InstanceContextMode = InstanceContextMode.Single)]
   public class ForTheRecordListener : EventsListenerServiceBase
   {
      private GuideEnricher windowsService = null;
      private static string MODULE = "GuideEnricherListener";
      
      public ForTheRecordListener()
      {
         windowsService = new GuideEnricher();
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
         signalOtherThread();
      }
      
      public override void UpcomingRecordingsChanged()
      {
         Logger.Verbose("{0}: Handle UpcomingRecordingsChanged event",MODULE);
         signalOtherThread();
      }

      public override void UpcomingSuggestionsChanged()
      {
         Logger.Verbose("{0}: Handle UpcomingSuggestionsChanged event",MODULE);
         signalOtherThread();
      }

      public override void ScheduleChanged(Guid scheduleId)
      {
         Logger.Verbose("{0}: Handle ScheduleChanged event",MODULE);
         signalOtherThread();
      }
      
      private void signalOtherThread() {
         Logger.Info("{0}: signal worker thread",MODULE);
         GuideEnricher.waitHandle.Set();
      }
      

      
   }
}
