using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static eSearch.Interop.ILogger;

namespace eSearch.Models.Logging
{
    public class MSLogger : ILogger
    {

        private eSearch.Interop.ILogger WrappedLogger;

        public MSLogger(eSearch.Interop.ILogger wrappedLogger)
        {
            this.WrappedLogger = wrappedLogger;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            WrappedLogger.Log(FromLogLevel(logLevel), formatter(state,exception) , exception);
        }

        private Severity FromLogLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    return Severity.ERROR;
                case LogLevel.Warning:
                    return Severity.WARNING;
                case LogLevel.Trace:
                case LogLevel.Debug:
                    return Severity.DEBUG;
                default:
                    return Severity.INFO;
            }
        }
    }
}
