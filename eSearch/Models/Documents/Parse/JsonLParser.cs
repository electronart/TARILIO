using DocumentFormat.OpenXml.Office2013.Word;
using Newtonsoft.Json.Linq;
using nietras.SeparatedValues;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using eSearch.Interop;

namespace eSearch.Models.Documents.Parse
{
    public class JsonLParser : IParser
    {
        public string[] Extensions =>  new string[] {"jsonl"};

        private string filePath;

        public void Parse(string filePath, out ParseResult parseResult)
        {
            this.filePath = filePath;
            parseResult = new ParseResult
            {
                ParserName = "JsonL Parser",
                SubDocuments = JsonLEnumerator(),
                TextContent = "JsonL File",
                TotalKnownSubDocuments = GetTotalRecords()
            };
        }

        private int GetTotalRecords()
        {
            int totalRecords = 0;
            using (StreamReader reader = new StreamReader(filePath))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        ++totalRecords;
                    }
                }
            }
            return totalRecords;
        }

        private IEnumerable<IDocument> JsonLEnumerator()
        {
            int row = 0;
            using (StreamReader reader = new StreamReader(filePath))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        ++row;
                        JToken parsed = JObject.Parse(line);
                        string beautified = parsed.ToString(Newtonsoft.Json.Formatting.Indented);

                        List<IMetaData> metaData = new List<IMetaData>();
                        metaData.Add(new Metadata
                        {
                            Key = "Row",
                            Value = row.ToString()
                        });

                        var record = new InMemoryDocument
                        {
                            FileType = "json",
                            Parser = "JsonL Parser",
                            FileName = filePath,
                            Text = beautified,
                            MetaData = metaData,
                            
                        };
                        yield return record;
                    }
                }
            }
        }
    }
}
