using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eSearch.Interop.Search
{
    public interface ISearchRequestHandler
    {
        public IEnumerable<string> GetAvailableIndexNames();

        public Task<IList<ISearchResult>> GetSearchResults(
            ISearchRequest searchRequest, 
            CancellationToken cancellationToken);
    }
}
