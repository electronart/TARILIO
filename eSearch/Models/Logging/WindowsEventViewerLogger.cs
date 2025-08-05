
using eSearch.Interop;
using System;
using System.Diagnostics;


namespace eSearch.Models.Logging
{
    public class WindowsEventViewerLogger : ILogger
    {
        public WindowsEventViewerLogger(string IndexID)
        {
            this.IndexID = IndexID;
        }
        
        private string IndexID;
        private const string SourceName = "eSearch";

        public void Log(ILogger.Severity severity, string message, Exception? exception = null)
        {
            
        }
    }
}
