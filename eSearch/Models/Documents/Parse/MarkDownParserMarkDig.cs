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
        private MarkdownPipeline? _pipeline;

        public string[] Extensions
        {
            get
            {
                return new string[] { "md" , "markdown" };
            }
        }

        public bool DoesParserExtractFiles => false;

        public bool DoesParserProduceSubDocuments => false;

        public void Parse(string filePath, out ParseResult parseResult)
        {
            if (_pipeline == null)
            {
                _pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions().Build(); // Support for citations, figures, footnotes, grid tables, diagrams etc
            }
            var html = Markdown.ToHtml(System.IO.File.ReadAllText(filePath), _pipeline);
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
