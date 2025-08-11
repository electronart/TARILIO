using eSearch.Interop;
using eSearch.Models.Configuration;
using eSearch.Models.Documents;
using eSearch.Models.Search;
using eSearch.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Indexing
{
    public interface IIndex
    {
        /// <summary>
        /// Display Name of the Index
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// Description of the Index
        /// </summary>
        string Description { get; set; }
        /// <summary>
        /// The Id of the Index
        /// </summary>
        string Id { get; }
        /// <summary>
        /// File system path of the Index
        /// 26 Nov 2024
        /// - The path may be relative. We switched to relative paths to support Portable builds. If the path is not rooted, assume location is relative to the Index Library location.
        /// - Better to use the new GetIndexAbsolutePath method.
        /// </summary>
        string Location { get; set; }

        /// <summary>
        /// Get the absolute path of this index.
        /// </summary>
        /// <returns></returns>
        public string GetAbsolutePath();

        /// <summary>
        /// The Soundex Dictionary that will be used when Soundex Search is enabled in the query.
        /// The dictionary is keyed by soundex code, containing a list of words in the index that match that code.
        /// This should then be used to build a 'synonym' search.
        /// </summary>
        /// <param name="soundexDictionary"></param>
        public void SetActiveSoundexDictionary(SoundexDictionary soundexDictionary);


        /// <summary>
        /// The size of the Index, in Bytes.
        /// </summary>
        int Size { get; set; }
        /// <summary>
        /// WordWheel for the Index. May return null if index does not support WordWheel.
        /// </summary>
        IWordWheel? WordWheel { get; }

        /// <summary>
        /// Ensure the index is open for reading.
        /// </summary>
        void OpenRead();

        /// <summary>
        /// Open Index for writing.
        /// </summary>
        /// <param name="create">Recreate the Index, rather than append.</param>
        /// <exception cref="FailedToOpenIndexException">Failed to open the index for any reason. Might be locked, IO error etc.</exception>
        void OpenWrite(bool create);
        /// <summary>
        /// Add Document to the Index.
        /// Must Open Index with OpenWrite before hand.
        /// Remember to Close Index afterwards.
        /// </summary>
        /// <param name="document"></param>
        void AddDocument(IDocument document);

        /// <summary>
        /// Add one or more documenst to the Index.
        /// Using this can improve performance over AddDocument in batches
        /// Must Open Index with OpenWrite before hand
        /// Remember to close index afterwards.
        /// </summary>
        /// <param name="documents"></param>
        void AddDocuments(IEnumerable<IDocument> documents);

        /// <summary>
        /// Close Index for writing.
        /// </summary>
        void CloseWrite();

        /// <summary>
        /// Make sure the index is closed ie. before deleting it otherwise can lead to FileIO Errors due to locked files.
        /// </summary>
        void EnsureClosed();

        /// <summary>
        /// Return the total number of documents in this index.
        /// </summary>
        /// <returns></returns>
        int GetTotalDocuments();

        /// <summary>
        /// Get the nth document of all documents in the index.
        /// </summary>
        /// <param name="n">Document to retrieve</param>
        /// <returns></returns>
        IDocument GetDocument(int n);

        /// <summary>
        /// Remove document from the index.
        /// </summary>
        /// <param name="document"></param>
        void RemoveDocument(IDocument document);

        /// <summary>
        /// Perform a search and get a paginator that can be used to iterate through search results.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public IVirtualReadOnlyObservableCollectionProvider<ResultViewModel> PerformSearch(QueryViewModel query);

        /// <summary>
        /// Get the available columns of data that can be displayed.
        /// </summary>
        /// <returns></returns>
        DataColumn[] GetAvailableColumns();

        /// <summary>
        /// All unique field names known to be in the index.
        /// </summary>
        public List<string> KnownFieldNames { get; set; }

        
        

    }
}
