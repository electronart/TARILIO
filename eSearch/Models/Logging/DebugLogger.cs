using eSearch.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Logging
{
    public class DebugLogger : ILogger
    {
        public void Log(ILogger.Severity severity, string message, Exception? exception = null)
        {
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
