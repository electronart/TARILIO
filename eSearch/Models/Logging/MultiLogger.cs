using eSearch.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Logging
{
    public class MultiLogger : ILogger
    {
        private IEnumerable<ILogger> loggers;

        public MultiLogger(IEnumerable<ILogger> loggers)
        {
            this.loggers = loggers;
        }

        public void Log(ILogger.Severity severity, string message, Exception? exception = null)
        {
            Parallel.ForEach(loggers, logger =>
            {
                logger.Log(severity, message, exception);
            });
        }
    }
}
