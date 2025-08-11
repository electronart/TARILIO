using eSearch.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Logging
{
    public class MultiLogger : ILogger2
    {
        private IEnumerable<ILogger> loggers;

        int errors = 0;
        int warnings = 0;

        public MultiLogger(IEnumerable<ILogger> loggers)
        {
            this.loggers = loggers;
        }

        public int GetNumErrors()
        {
            return errors;
        }

        public int GetNumWarnings()
        {
            return warnings;
        }

        public void Log(ILogger.Severity severity, string message, Exception? exception = null)
        {
            if (severity == ILogger.Severity.WARNING) ++warnings;
            if (severity == ILogger.Severity.ERROR) ++errors;

            Parallel.ForEach(loggers, logger =>
            {
                logger.Log(severity, message, exception);
            });
        }
    }
}
