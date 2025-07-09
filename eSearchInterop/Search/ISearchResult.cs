using System;
using System.Collections.Generic;
using System.Text;

namespace eSearch.Interop.Search
{
    public interface ISearchResult
    {
        public string DisplayName { get; }
        public string FilePath { get; }
        public string Context { get; }

        public IDocument GetDocument();
    }
}
