using Markdig;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace eSearch.Models.Documents.Parse
{
    public class IpynbParser : IParser
    {
        public string[] Extensions
        {
            get
            {
                return new string[] { "ipynb" };
            }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            StringBuilder htmlBuilder = new StringBuilder();
            htmlBuilder.Append("<span class='ipynb-doc'>");

            JObject root  = JObject.Parse(System.IO.File.ReadAllText(filePath));
            if (root.ContainsKey("cells"))
            {
                JArray cells = (JArray)root["cells"];
                ParseCells(cells, htmlBuilder);
            }
            if (root.ContainsKey("worksheets"))
            {
                JArray worksheets = (JArray)root["worksheets"];
                foreach(JObject sheet in worksheets)
                {
                    if (sheet.ContainsKey("cells"))
                    {
                        ParseCells((JArray)sheet["cells"], htmlBuilder);
                    }
                }
            }
            
            htmlBuilder.Append("</span>");
            HtmlParser htmlParser = new HtmlParser();
            htmlParser.ParseText(htmlBuilder.ToString(), out parseResult);
            parseResult.ParserName = "IpynbParser";

            JObject metadata = (JObject)root["metadata"];
            ParseMetadataRecursive(metadata, parseResult);
        }

        public void ParseCells(JArray cells, StringBuilder htmlBuilder)
        {
            foreach (JObject cell in cells)
            {
                var cell_type = (string)cell["cell_type"];
                List<string> srcList = new List<string>();
                if (cell.ContainsKey("source"))
                {
                    if (cell["source"].Type == JTokenType.String)
                    {
                        srcList.Add((string)cell["source"]);
                    }
                    else
                    {
                        JArray temp = (JArray)cell["source"];
                        foreach (var item in temp)
                        {
                            if (item.Type == JTokenType.String) srcList.Add((string)item);
                        }
                    }
                }
                if (cell.ContainsKey("input"))
                {
                    if (cell["input"].Type == JTokenType.String)
                    {
                        srcList.Add((string)cell["input"]);
                    }
                    else
                    {
                        JArray temp = (JArray)cell["input"];
                        foreach (var item in temp)
                        {
                            if (item.Type == JTokenType.String) srcList.Add((string)item);
                        }
                    }
                }

                string src = "";
                foreach(var str in srcList)
                {
                    src += str;
                }

                switch (cell_type)
                {
                    case "code":
                        htmlBuilder.AppendLine("<pre>");
                        if (cell.ContainsKey("language"))
                        {
                            htmlBuilder.Append("<code class='language-" + (string)cell["language"] + "'>");
                        }
                        else
                        {
                            htmlBuilder.Append("<code>");
                        }
                        htmlBuilder.AppendLine(HttpUtility.HtmlEncode(src));
                        htmlBuilder.AppendLine("</code></pre>");

                        break;
                    case "markdown":
                        htmlBuilder.AppendLine("<p>");
                        string mdTxt;
                        try
                        {
                            mdTxt = Markdown.ToHtml(src);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Error Parsing Markdown");
                            Debug.WriteLine(ex.ToString());
                            mdTxt = HttpUtility.HtmlEncode(src);
                        }
                        htmlBuilder.AppendLine(mdTxt);
                        htmlBuilder.AppendLine("</p>");
                        break;
                    default:
                        break; // unsupported.
                }

            }
        }

        public void ParseMetadataRecursive(object obj, ParseResult parseResult, string path = "")
        {
            if (obj is JObject jMetadataObj)
            {
                var keys = jMetadataObj.Properties().Select(p => p.Name).ToList();
                foreach(var key in keys)
                {
                    ParseMetadataRecursive(jMetadataObj[key], parseResult, path + ">" + key);
                }
            }
            if (obj is string jMetadataStr)
            {
                parseResult.Metadata.Add(new Metadata { 
                    Key = path, 
                    Value = jMetadataStr });
            }
        }
    }
}
