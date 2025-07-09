using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markdig;


namespace eSearch.Models.Documents.Parse
{
    public class MarkDownParserMarkDig : IParser
    {
        public string[] Extensions
        {
            get
            {
                return new string[] { "md" , "markdown" };
            }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            var html = Markdown.ToHtml(System.IO.File.ReadAllText(filePath));
            HtmlParser htmlParser = new HtmlParser();
            htmlParser.ParseText(html, out parseResult);
            parseResult.ParserName = "MarkdownParserMarkdig";
        }

        public static string ToHtml(string markDownText)
        {
            return Markdown.ToHtml(markDownText);
        }
    }
}
