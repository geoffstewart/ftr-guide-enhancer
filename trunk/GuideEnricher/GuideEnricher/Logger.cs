using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using ForTheRecord.ServiceAgents;
using ForTheRecord.ServiceContracts;
using ForTheRecord.Entities;
using ForTheRecord.Client.Common;

namespace GuideEnricher
{
    [Flags]
    public enum LogType
    {
        Information = 0x1,
        Warning = 0x2,
        Error = 0x4,
        Verbose = 0x8
    }

    public class Logger
    {

        private static LogType logLevel = LogType.Information | LogType.Warning | LogType.Error | LogType.Verbose;


        public static void Info(string message, params string[] args)
        {
            // throws an exception about a missing dll 
            //ForTheRecord.Common.Logging.Logger.Info(message, args);
            WriteEventLog(string.Format(message, args), LogType.Information);
        }

        public static void Verbose(string message, params string[] args)
        {
            // throws an exception about a missing dll 
            //ForTheRecord.Common.Logging.Logger.Verbose(message, args);
            WriteEventLog(string.Format(message, args), LogType.Verbose);
        }

        public static void Warning(string message, params string[] args)
        {
            // throws an exception about a missing dll 
            //ForTheRecord.Common.Logging.Logger.Info(message, args);
            WriteEventLog(string.Format(message, args), LogType.Warning);
            using (ForTheRecord.ServiceAgents.LogServiceAgent logAgent = new LogServiceAgent()) {
               
               logAgent.LogMessage("GuideEnricher",LogSeverity.Warning,string.Format(message,args));
            }
        }

        public static void Error(string message, params string[] args)
        {
            // throws an exception about a missing dll 
            //ForTheRecord.Common.Logging.Logger.Info(message, args);
            WriteEventLog(string.Format(message, args), LogType.Error);
            using (ForTheRecord.ServiceAgents.LogServiceAgent logAgent = new LogServiceAgent()) {
               logAgent.LogMessage("GuideEnricher",LogSeverity.Error,string.Format(message,args));
            }
        }

        private static void WriteEventLog(string message, LogType logType)
        {
            // only show logTypes in the logLevel enum
            if ( (logType & logLevel) == logType )
            {
                string sSource = "GuideEnricher";
                string sLog = "Application";

                EventLogEntryType eventLogType = EventLogEntryType.Information;

                // if logType is Error or Warning, set the eventlog type accordingly. Everything else will be information
                switch (logType)
                {
                    case LogType.Error: 
                        eventLogType = EventLogEntryType.Error;
                        break;
                    case LogType.Warning:
                        eventLogType = EventLogEntryType.Warning;
                        break;
                }


                if (!EventLog.SourceExists(sSource))
                    EventLog.CreateEventSource(sSource, sLog);

                EventLog.WriteEntry(sSource, message, eventLogType);
            }
        }
    }
}
