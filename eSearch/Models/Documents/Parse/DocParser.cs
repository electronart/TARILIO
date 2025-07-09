using NPOI.POIFS.FileSystem;
using NPOI.HWPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace eSearch.Models.Documents.Parse
{
    internal class DocParser : IParser
    {
        public string[] Extensions
        {
            get { return new string[] { "doc" }; }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            try
            {
                parseResult = new();
                parseResult.ParserName = "docParser (NPOI.HWPF)";
                StringBuilder docTextBuilder = new StringBuilder();
                POIFSFileSystem fs = new POIFSFileSystem(File.OpenRead(filePath));
                HWPFDocument doc = new HWPFDocument(fs);
                var range = doc.GetRange();
                var numParagraphs = range.NumParagraphs;
                int p = 0;
                while (p < numParagraphs)
                {
                    var paragraph = range.GetParagraph(p);
                    docTextBuilder.AppendLine("<p>")
                        .AppendLine(paragraph.Text)
                        .AppendLine("</p>");
                    p++;
                }
                parseResult.TextContent = docTextBuilder.ToString();
                parseResult.Title = doc.SummaryInformation.Title;
                parseResult.Authors = new string[] { doc.SummaryInformation.Author };
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString() + "=======_+_=-=-=");
                throw ex;
            }
        }
    }
}
