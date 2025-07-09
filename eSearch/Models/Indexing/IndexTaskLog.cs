using eSearch.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static eSearch.Interop.ILogger;

namespace eSearch.Models.Indexing
{
    public class IndexTaskLog : ILogger
    {

        public readonly List<LogItem> LoggedItems = new List<LogItem>();

        public int NumErrors;

        public int NumWarnings;

        public IndexTaskLog() { }

        public void Log(Severity severity, string message, Exception exception = null) 
        {
            switch (severity)
            {
                case Severity.WARNING:
                    ++NumWarnings; break;
                    case Severity.ERROR:
                    ++NumErrors; break;
            }
            LoggedItems.Add(new LogItem(severity, message, exception));
        }

        public string BuildTxtLog(string header = "", string footer = "")
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(header))
            {
                sb.AppendLine(header);
            }

            foreach (var item in LoggedItems)
            {
                sb
                    .Append("[").Append(item.Severity.ToString()).Append("] ")
                    .Append(item.DateTime.ToShortDateString()).Append(" ").Append(item.DateTime.ToShortTimeString()).Append(" ")
                    .Append(item.Message).AppendLine();
                if (item.Exception != null)
                {
                    sb.AppendLine(item.Exception.ToString());
                }
            }

            if (!string.IsNullOrEmpty(footer))
            {
                sb.AppendLine(footer);
            }

            return sb.ToString();
        }

        public class LogItem
        {
            public Severity  Severity;
            public string    Message;
            public Exception Exception;
            public DateTime  DateTime;

            public LogItem(Severity severity, string message, Exception exception)
            {
                Severity = severity;
                Message = message;
                Exception = exception;
                DateTime = DateTime.Now;
            }
        }

    }
}
