using System;
using System.Collections.Generic;
using System.Text;

namespace eSearch.Interop
{
    public interface ILogger
    {
        public enum Severity
        {
            DEBUG, INFO, WARNING, ERROR
        }

        public void Log(Severity severity, string message, Exception? exception = null);

    }

    public interface ILogger2 : ILogger
    {
        public int GetNumErrors();

        public int GetNumWarnings();

    }
}
