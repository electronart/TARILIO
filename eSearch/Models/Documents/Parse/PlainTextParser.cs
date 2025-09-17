using HtmlAgilityPack;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using VersOne.Epub;

namespace eSearch.Models.Documents.Parse
{
    internal class PlainTextParser : IParser
    {
        public string[] Extensions
        {
            get {
                if (_extensions == null)
                {
                    // Build the supported extensions once and reuse the same list so the performance hit is only once.
                    List<string> temp = new List<string>();
                    temp.AddRange(new string[] { "txt", "json", "tex", "lang" });
                    foreach(var srcCodeFormat in DocumentType.SourceCodeFormats)
                    {
                        temp.Add(srcCodeFormat.Extension);
                    }
                    _extensions = temp.ToArray();
                }
                return _extensions;
            }
        }

        public bool DoesParserExtractFiles => false;

        public bool DoesParserProduceSubDocuments => false;

        private string[] _extensions = null;

        public void Parse(string filePath, out ParseResult parseResult)
        {
            parseResult = new();
            parseResult.ParserName = "PlainTextParser";
            parseResult.Title = Path.GetFileNameWithoutExtension(filePath);
            parseResult.TextContent = File.ReadAllText(filePath, Encoding.UTF8);
            parseResult.Authors = new string[] { Utils.GetFileOwner(filePath) };
        }
    }
}
