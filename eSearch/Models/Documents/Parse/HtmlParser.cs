using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Documents.Parse
{
    internal class HtmlParser : IParser
    {
        public string[] Extensions
        {
            get { return new string[] { "html", "htm" }; }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            var encoding = Utils.GetEncoding(filePath, true);
            //var encoding = Encoding.GetEncoding(1252);
            var text = File.ReadAllText(filePath, encoding);
            ParseText(text, out parseResult);
            parseResult.HtmlRender = text;
            //ParseDoc(doc, out parseResult);
            if (parseResult.Title == "")
            {
                parseResult.Title = Path.GetFileName(filePath);
            }
        }

        public void ParseDoc(HtmlDocument doc, out ParseResult parseResult)
        {
            List<Metadata> docMetaData = new List<Metadata>();
            var metaDataNodes = doc.DocumentNode.SelectNodes("//meta");
            if (metaDataNodes != null)
            {
                foreach (var metaDataNode in metaDataNodes)
                {
                    string name = metaDataNode.GetAttributeValue("name", "");
                    string value = metaDataNode.GetAttributeValue("content", "");
                    if (name != "")
                    {
                        docMetaData.Add(new Metadata { Key = name, Value = value });
                    }
                }
            }
            StringWriter sw = new StringWriter();
            ConvertTo(doc.DocumentNode, sw);
            sw.Flush();
            string docTitle;
            try
            {
                docTitle = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim() ?? "";
            }
            catch (Exception ex)
            {
                docTitle = "";
            }
            string docText = sw.ToString();

            parseResult = new();
            parseResult.ParserName = "HtmlParser";
            parseResult.TextContent = docText.Trim();
            parseResult.HtmlRender = doc.ParsedText;
            parseResult.Title = docTitle;
            parseResult.Metadata.AddRange(docMetaData);
        }

        public void ParseText(string text, out ParseResult parseResult)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(text);
            ParseDoc(doc, out parseResult);
        }

        private static void ConvertContentTo(HtmlNode node, TextWriter outText)
        {
            foreach (HtmlNode subnode in node.ChildNodes)
            {
                ConvertTo(subnode, outText);
            }
        }

        private static void ConvertTo(HtmlNode node, TextWriter outText)
        {
            string html;
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // don't output comments
                    break;

                case HtmlNodeType.Document:
                    ConvertContentTo(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // script and style must not be output
                    string parentName = node.ParentNode.Name;
                    if ((parentName == "script") || (parentName == "style"))
                        break;

                    // get text
                    html = ((HtmlTextNode)node).Text;

                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(html))
                        break;

                    // check the text is meaningful and not a bunch of whitespaces
                    if (html.Trim().Length > 0)
                    {
                        outText.Write(HtmlEntity.DeEntitize(html));
                    }
                    break;

                case HtmlNodeType.Element:
                    switch (node.Name)
                    {
                        case "span":
                        case "b":
                        case "i":
                        case "em":
                        case "strong":
                        case "a":
                        case "mark":
                        case "s":
                            break;
                        case "li":
                            outText.Write("\n\t• ");
                            break;
                        case "th":
                        case "td":
                            outText.Write("\t\t");
                            break;
                        default:
                            outText.Write("\r\n");
                            break;

                    }

                    if (node.HasChildNodes)
                    {
                        ConvertContentTo(node, outText);
                    }
                    break;
            }
        }
    }
}
