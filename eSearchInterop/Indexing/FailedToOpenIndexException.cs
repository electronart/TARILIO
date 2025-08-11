using System;
using System.Collections.Generic;
using System.Text;

namespace eSearch.Interop.Indexing
{
    public class FailedToOpenIndexException : Exception
    {
        public FailedToOpenIndexException() { }

        public FailedToOpenIndexException(string message) : base(message) { }

        public FailedToOpenIndexException(string message, Exception inner) : base(message, inner) { }
    }
}
