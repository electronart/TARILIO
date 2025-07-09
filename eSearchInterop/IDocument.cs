using System;
using System.Collections.Generic;

namespace eSearch.Interop
{
    public interface IDocument
    {
        /// <summary>
        /// The Display Name of this Document
        /// </summary>
        string? DisplayName { get; }
        /// <summary>
        /// A unique identifier of this document in this index. The identifier will not be unique between indexes. 
        /// The type of identifier may vary depending on the type of data that is being indexed.
        /// </summary>
        string Identifier { get; }
        /// <summary>
        /// Get the Text Contents of the Document.
        /// </summary>
        string? Text { get; }

        /// <summary>
        /// The full FileName, including the path.
        /// </summary>
        string? FileName { get; }

        /// <summary>
        /// Which parser was used to get the text of this document.
        /// </summary>
        string? Parser { get; }

        /// <summary>
        /// Size of the file, in bytes.
        /// May return 0 when unknown.
        /// </summary>
        long FileSize { get; }

        DateTime? CreatedDate { get; }

        DateTime? ModifiedDate { get; }

        DateTime? IndexedDate { get; }

        /// <summary>
        /// FileSystemDocuments only. The date the file was last accessed.
        /// </summary>
        DateTime? AccessedDate { get; }

        IEnumerable<IMetaData>? MetaData { get; }
         
        IEnumerable<IDocument>? SubDocuments { get; }

        /// <summary>
        /// Note - This may not reflect the actual number of sub documents in some cases, where a count cannot be made without iterating the entire collection.
        /// it is NOT recursive and DOES NOT include sub documents of sub documents.
        /// </summary>
        public int TotalKnownSubDocuments { get; }

        /// <summary>
        /// Some documents should be skipped from indexing such as executables and errors in release build.
        /// </summary>
        public SkipReason ShouldSkipIndexing { get; }

        /// <summary>
        /// Returns true if this is not a file on the file system.
        /// </summary>
        public bool IsVirtualDocument { get; }

        /// <summary>
        /// Get the type of the document.
        /// Does not always use the file extension, can use magic numbers and other techniques depending on the datasource.
        /// </summary>
        /// <returns></returns>
        public string FileType { get; }

        /// <summary>
        /// Any files that were extracted from this document to the temp extractions folder.
        /// These files should be deleted after they are indexed.
        /// </summary>
        public IEnumerable<string> ExtractedFiles { get; }

        public enum SkipReason
        {
            DontSkip,
            TooLarge,
            ParseError,
            Executable,
            IndexFile,
            UnsupportedFileFormat
        }

        public string HtmlRender
        {
            get;
        }
    }
}
