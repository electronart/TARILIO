using Avalonia.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TikaOnDotNet.TextExtraction;
using TikaOnDotNet;

namespace eSearch.Models.Documents.Parse
{
    /// <summary>
    /// Don't use.
    /// This Parser uses TikaOnDotNet - Does not seem to work on dotnetcore.
    /// </summary>
    internal class TikaParser : IParser
    {
        public string[] Extensions
        {
            get { return new string[] { }; }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            parseResult = new();
            parseResult.ParserName = "tikaParser (TikaOnDotNet)";
            TextExtractor extractor = new TextExtractor();
            var res = extractor.Extract(filePath);
            parseResult.TextContent = res.Text;
            parseResult.Title = System.IO.Path.GetFileNameWithoutExtension(filePath);
        }
    }
}
