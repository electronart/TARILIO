using sun.swing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toxy;

namespace eSearch.Models.Documents.Parse.ToxyParsers
{
    public class Utils
    {
        public static void ExtractSheetData(ToxySpreadsheet ss, ref ParseResult parseResult, string filePath)
        {
            StringBuilder parsedOutput = new StringBuilder();
            foreach (var table in ss.Tables)
            {
                parsedOutput.AppendLine().AppendLine().AppendLine();
                foreach (var row in table.Rows)
                {
                    foreach (var cell in row.Cells)
                    {
                        parsedOutput.Append(cell.Value).Append("\t");
                    }
                    parsedOutput.AppendLine();
                }
            }

            parseResult = new ParseResult();
            if (!string.IsNullOrWhiteSpace(ss.Name))
            {
                parseResult.Title = ss.Name;
            }
            else
            {
                parseResult.Title = Path.GetFileNameWithoutExtension(filePath);
            }
            parseResult.TextContent = parsedOutput.ToString();
            parseResult.ParserName = "ToxyCSVParser";
        }


        
    }
}
