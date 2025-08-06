using eSearch.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Logging
{
    public class DebugLogger : ILogger2
    {
        int errors   = 0;
        int warnings = 0;

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

            switch(severity)
            {
                case ILogger.Severity.ERROR:
                case ILogger.Severity.WARNING:
                    Debug.WriteLine("ISSUE - " + message + " - Severity - " + severity.ToString());
                    Debug.WriteLine(exception?.ToString() ?? "");
                    break;
                default:
                    Debug.WriteLine(message + " --- " + exception.Message);
                    break;

            }

            
        }
    }
}
