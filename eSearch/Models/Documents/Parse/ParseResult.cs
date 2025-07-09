using eSearch.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Documents.Parse
{
    public class ParseResult
    {
        public string          Title        = "Untitled";
        public string[]        Authors      = new string[] {"Unknown"};
        public string          TextContent  = "Empty or unreadable document.";

        public string?         HtmlRender = null;

        public string          ParserName    = "Unknown Parser";
        public List<IMetaData>  Metadata     = new List<IMetaData>();

        /// <summary>
        /// For eg. Archives - Check here for where the files were extracted to and remember to clean them up afterwards.
        /// </summary>
        public List<string>    ExtractedFiles = new List<string>();

        public IEnumerable<IDocument> SubDocuments = new List<IDocument>();

        /// <summary>
        /// Note - This may not reflect the actual number of sub documents in some cases, where a count cannot be made without iterating the entire collection.
        /// </summary>
        public int TotalKnownSubDocuments { get; set; } = 0;


        /// <summary>
        /// Sometimes gets set to true when document is an executable or parse result was an error etc.
        /// </summary>
        public IDocument.SkipReason SkipIndexingDocument = IDocument.SkipReason.DontSkip;
    }
}
