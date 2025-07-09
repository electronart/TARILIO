using System;

namespace eSearch.Interop
{
    public interface IDataSource : IFormattable
    {
        /// <summary>
        /// Pass the logger that should be used for the data source and child data source(s)
        /// </summary>
        /// <param name="logger"></param>
        public void UseIndexTaskLog(ILogger logger);

        /// <summary>
        /// Go to the next document, if there is one currently available
        /// </summary>
        /// <returns>true if there are further documents, else false.</returns>
        /// <remarks>
        /// !Notice! <br></br>
        /// On returning false, check IsDiscoveryComplete() - If true, end of the index, otherwise <b>more documents may be returned later.</b></remarks>
        void GetNextDoc(out IDocument document, out bool isDiscoveryComplete);

        /// <summary>
        /// Get how far through the datasource we've gotten.
        /// </summary>
        /// <returns>A Percentage value between 0 and 100.</returns>
        double GetProgress();

        /// <summary>
        /// Get how many documents have been discovered so far in this data source, including child data sources.
        /// </summary>
        int GetTotalDiscoveredDocuments();

        /// <summary>
        /// Go back to the first document.
        /// </summary>
        void Rewind();

        /// <summary>
        /// Get the description of this DataSource, as should be displayed on the UI.
        /// </summary>
        /// <returns>User friendly description.</returns>
        string Description();
    }
}
