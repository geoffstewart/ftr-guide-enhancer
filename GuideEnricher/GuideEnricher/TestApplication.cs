using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ForTheRecord.ServiceAgents;
using System.ServiceModel;
using ForTheRecord.Entities;

namespace GuideEnricher
{
    static class TestApplication
    {
        static void Main()
        {

            Thread.Sleep(2000);

            var config = Config.Config.GetInstance();
            ServerSettings serverSettings = new ServerSettings();
            serverSettings.ServerName = "ganymed";
            serverSettings.Transport = ServiceTransport.NetTcp;
            serverSettings.Port = 49942;

            ServiceChannelFactories.Initialize(serverSettings, false);

            ServiceHost sh = ForTheRecordListener.CreateServiceHost(config.getProperty("serviceUrl"));

            sh.Open();

            using (ForTheRecordServiceAgent agent = new ForTheRecordServiceAgent())
            {
                ForTheRecordEventGroup eventGroupsToListenTo = ForTheRecordEventGroup.ScheduleEvents;
                agent.EnsureEventListener(eventGroupsToListenTo, config.getProperty("serviceUrl"), Constants.EventListenerApiVersion);
                Console.Read();
            }

        }
    }
}
