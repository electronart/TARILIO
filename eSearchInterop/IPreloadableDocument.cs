using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Interop
{
    public interface IPreloadableDocument : IDocument
    {
        /// <summary>
        /// The thread blocking preload task.
        /// Preload document fields etc.
        /// eSearch will call this on preloadable documents when indexing
        /// Allowing eSearch to be preloading more than one document at a time.
        /// </summary>
        public Task PreloadDocument();
    }
}
