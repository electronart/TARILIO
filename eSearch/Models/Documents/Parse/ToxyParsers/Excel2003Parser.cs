using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toxy;

namespace eSearch.Models.Documents.Parse.ToxyParsers
{
    internal class Excel2003Parser : IParser
    {
        public string[] Extensions
        {
            get { return new string[] { "xls" }; }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            parseResult = new ParseResult();
            ParserContext context = new ParserContext(filePath);
            ISpreadsheetParser parser = ParserFactory.CreateSpreadsheet(context);
            ToxySpreadsheet ss = parser.Parse();
            ToxyParsers.Utils.ExtractSheetData(ss, ref parseResult, filePath);
            parseResult.ParserName = "ToxyExcel2003Parser";
        }
    }
}
