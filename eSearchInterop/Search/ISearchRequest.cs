using System;
using System.Collections.Generic;
using System.Text;

namespace eSearch.Interop.Search
{
    public interface ISearchRequest
    {
        public string Query { get; }

        public string Index { get; }
    }
}
