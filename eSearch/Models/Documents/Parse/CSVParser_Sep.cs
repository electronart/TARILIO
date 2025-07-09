using eSearch.Interop;
using javax.swing.text;
using nietras.SeparatedValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace eSearch.Models.Documents.Parse
{
    public class CSVParser_Sep : IParser
    {
        public string[] Extensions
        {
            get
            {
                return new string[] { "csv", "tsv" };
            }
        }

        string? filePath;

        public void Parse(string filePath, out ParseResult parseResult)
        {
            this.filePath = filePath;
            parseResult = new ParseResult
            {
                ParserName = "CSVParser (Sep)",
                SubDocuments = CSVRecordEnumerator(),
                TextContent = "CSV File",
                TotalKnownSubDocuments = GetTotalRecords()
            };

        }

        public int GetTotalRecords()
        {
            using (var countReader = Sep.Reader().FromFile(filePath))
            {
                int i = 0;
                foreach (var row in countReader)
                {
                    ++i;
                }
                return i;
            }
        }

        public IEnumerable<IDocument> CSVRecordEnumerator()
        {
            var options = new SepReaderOptions { Unescape = true, DisableQuotesParsing = true };
            using (var reader = Sep.Reader(options => options).FromFile(filePath))
            {
                List<string> columnNames = new List<string>();
                if (reader.HasHeader)
                {
                    for (int i = 0; i < reader.Header.ColNames.Count; ++i)
                    {
                        string colName = reader.Header.ColNames[i];
                        if (!string.IsNullOrWhiteSpace(colName))
                        {
                            if (colName.StartsWith('"') && colName.EndsWith('"'))
                            {
                                colName = colName.Substring(1, colName.Length - 2);
                            }
                            columnNames.Add(colName);
                        } else
                        {
                            columnNames.Add("Column " + i);
                        }
                    }
                }

                // The following code is performance optimized for requirements which is why it's a little less readable.
                string startTableColumns = "<table><tr>";
                string startTableRow     = "<tr>";
                string endTableRow       = "</tr>";
                string endTableColumns   = "</tr>";
                string endTable         = "</table>";
                string startTH          = "<th>";
                string endTH            = "</th>";
                string startTD          = "<td>";
                string endTD            = "</td>";

                var recordTextBuilder = new StringBuilder();

                int numColumns;
                foreach (var row in reader)
                {
                     string tableHead = "";
                     string tableContent = "<tr>";
                     numColumns = row.ColCount;


                    var record = new InMemoryDocument
                    {
                        FileType = "Database Record",
                        Parser = "CSVParser (Sep)",
                        FileName = filePath,
                    };

                    List<IMetaData> metaData = new List<IMetaData>(numColumns + 1); // + 1 for 'Row'

                    metaData.Add(new Metadata
                    {
                        Key = "Row",
                        Value = row.RowIndex.ToString()
                    });


                    recordTextBuilder.Clear();
                    
                    for (int i = 0; i < numColumns; i++)
                    {
                        string columnName;
                        if (columnNames.Count > i)
                        {
                            columnName = columnNames[i];
                        }
                        else
                        {
                            columnName = "Column " + i;
                        }

                        string value = row[i].ToString();
                        if (value.StartsWith('"') && value.EndsWith('"'))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }
                        string valueHtmlEncoded = HttpUtility.HtmlEncode(value);

                        // This is just a faster string concat operation
                        tableHead = string.Create(tableHead.Length + columnName.Length + 9, 
                            (tableHead,startTH, columnName, endTH),
                            (span, state) => span.TryWrite($"{state.tableHead}{state.startTH}{state.columnName}{state.endTH}", out _)
                        );
                        tableContent = string.Create(tableContent.Length + valueHtmlEncoded.Length + 9,
                            (tableContent, startTD, valueHtmlEncoded, endTD),
                            (span, state) => span.TryWrite($"{state.tableContent}{state.startTD}{state.valueHtmlEncoded}{state.endTD}", out _)
                        );
                        metaData.Add(new Metadata
                        {
                            Key = columnName,
                            Value = value
                        });
                        recordTextBuilder.Append(" ").Append(value);
                    }
                    record.Text = recordTextBuilder.ToString();
                    record.MetaData = metaData;

                    // Performance reasons.
                    // https://www.reddit.com/r/dotnet/comments/15dr3gk/string_concatenation_benchmarks_in_net_8/
                    // https://gist.github.com/davepcallan/2063c516d6ea377e6c161bbc39c58701
                    record.HtmlRender = string.Create(startTableColumns.Length + endTableRow.Length + tableHead.Length + tableContent.Length + endTableRow.Length + endTable.Length, 
                        (startTableColumns, endTableRow, tableHead, tableContent, endTable), 
                        (span, state) => span.TryWrite($"{state.startTableColumns}{state.tableHead}{state.endTableRow}{state.tableContent}{state.endTableRow}{state.endTable}", out _)
                    );

                    yield return record;
                }
            }
        }
    }
}
