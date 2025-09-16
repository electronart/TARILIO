using DocumentFormat.OpenXml.Drawing;
using eSearch.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace eSearch.Models.Documents.Parse
{
    internal class XmlParser : IParser
    {
        public string[] Extensions
        {
            get
            {
                return new string[] { "xml", "tbx", "tmx", "xliff" };
            }
        }

        public bool DoesParserExtractFiles => false;

        public bool DoesParserProduceSubDocuments => false;

        public void Parse(string filePath, out ParseResult result)
        {
            try
            {
                string xml = File.ReadAllText(filePath, Encoding.UTF8);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                StringBuilder sb = new StringBuilder();
                ParseRecursive(doc, sb);
                result = new ParseResult
                {
                    ParserName = "XmlParser (System.Xml)",
                    Title = System.IO.Path.GetFileNameWithoutExtension(filePath),
                    TextContent = sb.ToString(),
                    HtmlRender = HttpUtility.HtmlEncode(xml)
                };

            } catch (Exception ex)
            {
                result = new ParseResult
                {
                    SkipIndexingDocument = IDocument.SkipReason.ParseError,
                    TextContent = ex.ToString()
                };
            }
        }

        private void ParseRecursive(XmlNode node, StringBuilder sb)
        {
            if (node.NodeType == XmlNodeType.Text)
            {
                if (node.Value != null)
                {
                    sb.AppendLine(node.Value + " ");
                }
            }
            foreach (var childNode in node.ChildNodes)
            {
                sb.AppendLine();
                if (childNode is XmlNode child)
                {
                    ParseRecursive(child, sb);
                }
            }
        }
    }
}
