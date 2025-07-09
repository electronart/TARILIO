using eSearch.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Documents.Parse.ToxyParsers
{
    public class RTFParser : IParser
    {

        public string[] Extensions
        {
            get { return new string[] { "rtf" }; }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            Toxy.ParserContext context = new Toxy.ParserContext(filePath);
            Toxy.Parsers.RTFTextParser toxyRTFParser = new Toxy.Parsers.RTFTextParser(context);
            string strHTML = toxyRTFParser.Parse();
            string strText = strHTML;
            HtmlParser htmlParser = new HtmlParser();
            htmlParser.ParseText(strHTML, out ParseResult htmlParserResult);
            if (htmlParserResult.SkipIndexingDocument != IDocument.SkipReason.ParseError)
            {
                strText = htmlParserResult.TextContent;
            }


            parseResult = new ParseResult
            {
                Title = Path.GetFileNameWithoutExtension(filePath),
                TextContent = strText,
                HtmlRender = strHTML,
                ParserName = "ToxyRTFParser"
            };
        }
    }
}
