using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Documents.Parse
{
    internal class DocXParser : IParser
    {
        public string[] Extensions {
            get { return new string[] { "docx" }; }
        }

        public bool DoesParserExtractFiles => false;

        public bool DoesParserProduceSubDocuments => false;

        public void Parse(string filePath, out ParseResult parseResult)
        {
            parseResult = new();
            parseResult.ParserName = "docxParser (OpenXML)";

            StringBuilder   sbMainBody = new StringBuilder();
            List<string>    authors;
            
            using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false))
            {
                #region Extract Text from Body
                Body body = doc.MainDocumentPart.Document.Body;
                foreach (var child in body.ChildElements)
                {
                    if (child.InnerText != null && !string.IsNullOrWhiteSpace(child.InnerText) && !child.InnerText.StartsWith("Contents TOC"))
                    {
                        sbMainBody
                            .AppendLine("<p>")
                            .AppendLine(child.InnerText)
                            .AppendLine("</p>");
                    }
                }
                #endregion
                #region Extract Document Metadata
                if (!string.IsNullOrEmpty(doc.PackageProperties?.Creator))
                {
                    authors = new List<string> { doc.PackageProperties.Creator.Trim() };
                }
                if (!string.IsNullOrEmpty(doc.PackageProperties?.Title))
                {
                    parseResult.Title = doc.PackageProperties.Title.Trim();
                }
                #endregion
            }
            parseResult.TextContent = sbMainBody.ToString().Trim();
        }
    }
}
