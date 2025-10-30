using DynamicData;
using eSearch.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Logging
{
    public class InMemoryLog : ILogger
    {

        public InMemoryLog(TimeSpan logRetention)
        {
            this._logRetension = logRetention;
        }

        private TimeSpan _logRetension;

        public ObservableCollection<LogItem> LogItems { get; private set; } = new ObservableCollection<LogItem>();

        public void Log(ILogger.Severity severity, string message, Exception? exception = null)
        {
            LogItems.Add(new LogItem(severity, message, exception));
            // Remove any items older than retension policy now.
            var cutoffTime = DateTime.Now - _logRetension;
            var itemsToRemove = LogItems.Where(item => item.DateTime < cutoffTime).ToList();
            foreach (var item in itemsToRemove)
            {
                LogItems.Remove(item);
            }
        }

        public class LogItem
        {
            public DateTime DateTime { get; }
            public ILogger.Severity Severity { get; }
            public string Message { get; }
            public Exception? Exception { get; }

            public LogItem(ILogger.Severity severity, string message, Exception? exception, DateTime? dateTime = null)
            {
                this.DateTime = dateTime ?? DateTime.Now;
                this.Severity = severity;
                this.Message = message;
                this.Exception = exception;
            }
        }
    }
}
