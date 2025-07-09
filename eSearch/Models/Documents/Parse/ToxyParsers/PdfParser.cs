


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toxy.Parsers;

namespace eSearch.Models.Documents.Parse.ToxyParsers
{
    public class PdfParser : IParser
    {
        public string[] Extensions
        {
            get { return new string[] { "pdf" }; }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            //var textParser  = new PDFTextParser(new Toxy.ParserContext(filePath));
            //var textContent = textParser.Parse();

            StringBuilder sb = new StringBuilder();

            //Debug.WriteLine("Extracting text from PDF...");
            sb.Append(PdfTextract.PdfTextExtractor.GetText(filePath));
            //Debug.WriteLine("Extracted Text:");
            //Debug.WriteLine(sb.ToString());
            //Debug.WriteLine("END_EXTRACTED_TEXT");
            //var docParser = new PDFDocumentParser(new Toxy.ParserContext(filePath));
            //var docResult = docParser.Parse();
            //var numParagraphs = docResult.Paragraphs.Count;
            //Debug.WriteLine("PDF Parser - Num Paragraphs " + numParagraphs );
            //int i = 0; 
            //while (i < numParagraphs)
            //{
            //    sb.Append("\t<p>").Append(docResult.Paragraphs[i]).Append("</p>\n");
            //    Debug.WriteLine(docResult.Paragraphs[i]);
            //}

            parseResult = new ParseResult();
            parseResult.TextContent = sb.ToString();
            parseResult.Title = Path.GetFileNameWithoutExtension(filePath);
            parseResult.ParserName = "ToxyPDFParser";
        }
    }
}
