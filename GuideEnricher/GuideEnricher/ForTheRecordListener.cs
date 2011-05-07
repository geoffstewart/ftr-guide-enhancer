/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 11/10/2010
 * Time: 1:27 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace GuideEnricher
{
    using System;
    using System.Reflection;
    using System.ServiceModel;
    using ForTheRecord.Client.Common;
    using ForTheRecord.ServiceContracts.Events;
    using log4net;

    /// <summary>
    /// Description of ForTheRecordListener.
    /// </summary>
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class ForTheRecordListener : EventsListenerServiceBase
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static string MODULE = "GuideEnricherListener";

        public static ServiceHost CreateServiceHost(string eventsServiceBaseUrl)
        {
            ServiceHost sh = CreateServiceHost(typeof(ForTheRecordListener), eventsServiceBaseUrl, typeof(IGuideEventsListener),
                   typeof(IRecordingEventsListener), typeof(IScheduleEventsListener), typeof(ISystemEventsListener));

            return sh;
        }

        public override void NewGuideData()
        {
            log.DebugFormat("{0}: Handle NewGuideData event", MODULE);
            signalOtherThread();
        }

        public override void UpcomingRecordingsChanged()
        {
            log.DebugFormat("{0}: Handle UpcomingRecordingsChanged event", MODULE);
            signalOtherThread();
        }

        public override void UpcomingSuggestionsChanged()
        {
            log.DebugFormat("{0}: Handle UpcomingSuggestionsChanged event", MODULE);
            signalOtherThread();
        }

        public override void ScheduleChanged(Guid scheduleId)
        {
            log.DebugFormat("{0}: Handle ScheduleChanged event", MODULE);
            signalOtherThread();
        }

        private void signalOtherThread()
        {
            log.DebugFormat("{0}: signal worker thread", MODULE);
            GuideEnricher.waitHandle.Set();
        }
    }
}
