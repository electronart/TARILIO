using Avalonia.Controls.Shapes;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersOne.Epub;

namespace eSearch.Models.Documents.Parse
{
    internal class EpubParser2 : IParser
    {
        StringBuilder textContent = new StringBuilder();
        string title = String.Empty;

        public string[] Extensions {
            get { return new string[] { "epub" }; }
        }

        public bool DoesParserExtractFiles => false;

        public bool DoesParserProduceSubDocuments => false;

        public void Parse(string filePath, out ParseResult parseResult)
        {
            parseResult = new();
            parseResult.ParserName = "EpubParser (VersOne.Epub)";

            textContent.Clear();
            EpubBook book = EpubReader.ReadBook(filePath);
            foreach(EpubLocalTextContentFile textContentFile in book.ReadingOrder)
            {
                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(textContentFile.Content);

                //Debug.WriteLine(htmlDocument.DocumentNode.OuterHtml);
               // Debug.WriteLine(textContentFile.Content);
               // Debug.WriteLine("AAA");

                
                var bodyNode = htmlDocument.DocumentNode.SelectSingleNode("//body");
                foreach(HtmlNode node in bodyNode.SelectNodes("//text()"))
                {
                    textContent.AppendLine("<p>" + node.InnerText.Trim() + "</p>");
                }
            }
            title = book.Title;
            if (string.IsNullOrEmpty(title))
            {
                title = System.IO.Path.GetFileNameWithoutExtension(filePath);
            }

            parseResult.Title = title;
            parseResult.TextContent = textContent.ToString();
            parseResult.Authors      = book.AuthorList.ToArray();
        }
    }
}
