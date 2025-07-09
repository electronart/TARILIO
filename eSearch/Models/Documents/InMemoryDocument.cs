using eSearch.Interop;
using eSearch.Models.Documents.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Documents
{
    public struct InMemoryDocument : IDocument
    {
        public string? DisplayName { get; set; }

        public string Identifier
        {
            get
            {
                if (_id == null)
                {
                    _id = Guid.NewGuid().ToString();
                }
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        private string? _id = null;

        public InMemoryDocument()
        {
        }

        public string? Text { get; set; } = null;

        public string? FileName { get; set; } = null;

        public string? Parser { get; set; } = null;

        public long FileSize { get; set; } = 0;

        public DateTime? CreatedDate { get; set; } = null;

        public DateTime? ModifiedDate { get; set; } = null;

        public DateTime? IndexedDate { get; set; } = null;

        public DateTime? AccessedDate { get; set; } = null;

        public IEnumerable<IMetaData> MetaData { get; set; } = new List<IMetaData>();

        public IEnumerable<IDocument>? SubDocuments { get; set; } = null;

        public int TotalKnownSubDocuments { get; set; } = 0;

        public IDocument.SkipReason ShouldSkipIndexing { get; set; } = IDocument.SkipReason.DontSkip;

        public IEnumerable<string> ExtractedFiles { get; set; } = new List<string>();

        public string HtmlRender { get; set; } = String.Empty;

        public string FileType { get; set; } = String.Empty;

        public bool IsVirtualDocument
        {
            get
            {
                return true; // This document is a memory document, and thus is not on the file system.
            }
        }
    }
}
