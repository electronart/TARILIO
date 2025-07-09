using eSearch.Interop;
using eSearch.Models.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search
{
    public class Result
    {
        /// <summary>
        /// The Document that contains one or more hits.
        /// </summary>
        public IDocument Document { get; }
        /// <summary>
        /// How many hits found in the document. Also includes Metadata hits.
        /// </summary>
        public int Hits { get; }
        /// <summary>
        /// The Words of Context for this Search Result.
        /// </summary>
        public string Context { get; }

        public Result(IDocument document, int hits, string context)
        {
            Document = document;
            Hits = hits;
            Context = context;
        }
    }
}
