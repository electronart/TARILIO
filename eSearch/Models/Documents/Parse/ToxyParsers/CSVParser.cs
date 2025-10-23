
using DocumentFormat.OpenXml.Wordprocessing;
using eSearch.Interop;
using javax.swing.text;
using javax.ws.rs;
using jdk.nashorn.@internal.runtime.regexp.joni;
using Lucene.Net.Documents;
using org.apache.sis.@internal.jaxb.gmx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Toxy;

namespace eSearch.Models.Documents.Parse.ToxyParsers
{
    // https://github.com/nissl-lab/toxy/tree/master/Toxy.Test
    internal class CSVParser : IParser
    {
        public string[] Extensions
        {
            get { return new string[] { "csv" }; }
        }

        public bool DoesParserExtractFiles => false;

        public bool DoesParserProduceSubDocuments => true;

        public void Parse(string filePath, out ParseResult parseResult)
        {

            #region Load Spreadsheet
            ParserContext context = new ParserContext(filePath);
            context.Properties.Add("ExtractHeader", "1");
            ISpreadsheetParser parser = (ISpreadsheetParser)ParserFactory.CreateSpreadsheet(context);
            ToxySpreadsheet ss = parser.Parse();
            #endregion

            #region Get Sheet Data as Individual Records
            ParseResult mainDocument = new ParseResult();
            mainDocument.ParserName = "ToxyCSVParser";
            mainDocument.Metadata.Add(new Metadata { Key = "Type", Value = "Database" });


            StringBuilder mainDocHtmlRender = new StringBuilder();
            mainDocHtmlRender.AppendLine("<p>CSV File</p>");
            List<ParseResult> records = new List<ParseResult>();

            foreach (var table in ss.Tables)
            {
                string tableName = string.IsNullOrEmpty(table.Name) ? "Table" : table.Name;
                string tableNfo = $"{table.Rows.Count} Rows";
                mainDocHtmlRender.AppendLine("<p>" + tableNfo + "</p>");
            }

            var csvEnumerator = new CSVEnumerator(ss, filePath);
            mainDocument.HtmlRender = mainDocHtmlRender.ToString();
            mainDocument.SubDocuments = csvEnumerator;
            mainDocument.TotalKnownSubDocuments = csvEnumerator.GetNumRows();
            parseResult = mainDocument;

            #endregion
        }
    }

    internal class CSVEnumerator : IEnumerable<IDocument>
    {
        StringBuilder htmlRenderBuilder = new StringBuilder();
        StringBuilder searchableDataBuilder = new StringBuilder();


        ToxySpreadsheet ss;
        string filePath;

        public CSVEnumerator(ToxySpreadsheet ss, string filePath)
        {
            this.filePath = filePath;
            this.ss = ss;
        }

        public int GetNumRows()
        {
            if (ss.Tables.Count == 0) throw new Exception("Unexpected - CSV File gives 0 tables?");
            if (ss.Tables.Count > 1) throw new Exception("Unexpected - CSV File gives more than 1 table?");
            return ss.Tables[0].Rows.Count;
        }

        public IEnumerator<IDocument> GetEnumerator()
        {
            if (ss.Tables.Count == 0) throw new Exception("Unexpected - CSV File gives 0 tables?");
            if (ss.Tables.Count > 1) throw new Exception("Unexpected - CSV File gives more than 1 table?");
            var table = ss.Tables[0];
            string tableName = string.IsNullOrEmpty(table.Name) ? "Table" : table.Name;
            string tableNfo = $"{table.Rows.Count} Rows";



            #region Build Table Header
            string tableHeader = "<tr>";
            if (table.Rows.Count > 0)
            {
                foreach (var cell in table.Rows[0].Cells)
                {
                    tableHeader += "<th>" + HttpUtility.HtmlEncode(cell.Value) + "</th>";
                }
            }
            tableHeader += "</tr>";
            #endregion

            for (int i = 0; i < table.Rows.Count; i++)
            {
                searchableDataBuilder.Clear();
                htmlRenderBuilder.Clear();

                    

                List<Metadata> docMetaData = new List<Metadata>();
                docMetaData.Add(new Metadata { Key = "Table", Value = tableName });
                docMetaData.Add(new Metadata { Key = "RowIndex", Value = "" + i });


                var record = new InMemoryDocument
                {
                    FileType = "Database Record",
                    Parser = "ToxyCSVParser",
                    DisplayName = table.Name,
                    FileName = filePath
                };

                htmlRenderBuilder.AppendLine("<table>");
                htmlRenderBuilder.AppendLine(tableHeader);
                htmlRenderBuilder.AppendLine("\t<tr>");
                foreach (var cell in table.Rows[i].Cells)
                {

                    var cellHeader = table.Rows[0].Cells.Count > cell.CellIndex ? table.Rows[0].Cells[cell.CellIndex].Value : $"Column {cell.CellIndex + 1}";
                    var cellValue = cell.Value;
                    docMetaData.Add(new Metadata { Key = "_Cell" + cell.CellIndex + "_" + cellHeader, Value = cellValue });
                        
                    searchableDataBuilder.AppendLine(cellValue);
                    htmlRenderBuilder.Append("\t\t<td>").Append(HttpUtility.HtmlEncode(cell.Value)).AppendLine("</td>");
                }
                htmlRenderBuilder.AppendLine("</tr></table>");

                record.HtmlRender = htmlRenderBuilder.ToString();
                record.Text = searchableDataBuilder.ToString();
                record.MetaData = docMetaData;
                yield return record;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
