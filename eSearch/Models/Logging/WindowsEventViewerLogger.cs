
using eSearch.Interop;
using eSearch.Models.Indexing;
using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using static eSearch.Interop.ILogger;


namespace eSearch.Models.Logging
{
    [SupportedOSPlatform("windows")]
    public class WindowsEventViewerLogger : ILogger2
    {
        public WindowsEventViewerLogger(IIndex index)
        {
            this.index = index;
        }
        
        private IIndex index;
        private const string SourceName = "eSearch";

        int warnings = 0;
        int errors = 0;
        
        public void Log(ILogger.Severity severity, string message, Exception? exception = null)
        {
            if (severity == Severity.ERROR) ++errors;
            if (severity == Severity.WARNING) ++warnings;

            string fullMessage =  $"Index Name: {index.Name}\n";
                   fullMessage += $"Index ID: {index.Id}\n";
                   
                   fullMessage += $"{message}";
            if (exception != null)
            {
                fullMessage += $"\n---\n{exception.ToString()}\n---\n";
            }
            fullMessage += $"\n{Program.ProgramConfig.GetProductTagText()} {Program.GetProgramVersion()}";
            using (EventLog eventLog = new EventLog())
            {
                eventLog.Source = SourceName;
                EventLogEntryType entryType = MapSeverityToEntryType(severity);
                eventLog.WriteEntry(fullMessage, entryType);
            }
        }

        private EventLogEntryType MapSeverityToEntryType(Severity severity)
        {
            switch (severity)
            {
                case Severity.DEBUG:
                    return EventLogEntryType.Information;
                case Severity.INFO:
                    return EventLogEntryType.Information;
                case Severity.WARNING:
                    return EventLogEntryType.Warning;
                case Severity.ERROR:
                    return EventLogEntryType.Error;
                default:
                    return EventLogEntryType.Information;
            }
        }

        public int GetNumErrors()
        {
            return errors;
        }

        public int GetNumWarnings()
        {
            return warnings;
        }
    }
}
