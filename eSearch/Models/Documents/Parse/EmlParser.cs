using eSearch.Interop;
using MimeKit;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace eSearch.Models.Documents.Parse
{
    public class EmlParser : IParser
    {

        HtmlParser HtmlParser { 
            get
            {
                if (_htmlParser == null)
                {
                    _htmlParser = new HtmlParser();
                }
                return _htmlParser;
            } 
        }

        private HtmlParser? _htmlParser = null;

        public string[] Extensions
        {
            get { return ["eml"]; }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            var mimeMsg = MimeMessage.Load(filePath);
            var mimeAttachments = mimeMsg.Attachments.ToList();

            List<IMetaData> metadata = new List<IMetaData>();
            metadata.Add(new Metadata { Key = "To", Value = string.Join(", ", mimeMsg.To) });
            metadata.Add(new Metadata { Key = "From", Value = string.Join(", ", mimeMsg.From) });
            metadata.Add(new Metadata { Key = "BCC", Value = string.Join(", ", mimeMsg.Bcc) });
            metadata.Add(new Metadata { Key = "CC", Value = string.Join(", ", mimeMsg.Cc) });
            #region Handle Attachments
            List<string> extractedFiles = new List<string>();
            List<string> extractedFileNames = new List<string>();
            string output_dir = Path.Combine(Program.ESEARCH_TEMP_FILES_PATH, "Extractions");
            Directory.CreateDirectory(output_dir);


            foreach (var attachment in mimeAttachments)
            {

                using (var memory = new MemoryStream())
                {
                    string fileName = string.Empty;

                    if (attachment is MimePart mimePart)
                    {
                        mimePart.Content.DecodeTo(memory);
                        fileName = mimePart.FileName;
                    }
                    else if (attachment is MessagePart messagePart)
                    {
                        messagePart.Message.WriteTo(memory);
                        fileName = messagePart.ContentDisposition?.FileName ?? messagePart.ContentType?.Name ?? "Unnamed Message.eml";
                        if (!fileName.EndsWith(".eml"))
                        {
                            fileName = fileName + ".eml";
                        }
                    }
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        fileName = "Unnamed Message.eml";
                    }

                    var bytes = memory.ToArray();

                    string outputFile = Path.Combine(output_dir, fileName);
                    File.WriteAllBytes(outputFile, bytes);
                    extractedFiles.Add(outputFile);
                    extractedFileNames.Add(fileName);

                }
            }
            metadata.Add(new Metadata { Key = "Attachments", Value = string.Join(", ", extractedFileNames) });
            #endregion

            
            HtmlParser.ParseText(mimeMsg.HtmlBody ?? mimeMsg.TextBody ?? string.Empty, out var htmlParseResult);


            parseResult = new ParseResult
            {
                ParserName = "EMLParserMimeKit",
                Metadata = metadata,
                HtmlRender = mimeMsg.HtmlBody ?? string.Empty,
                TextContent = htmlParseResult.TextContent ?? string.Empty,
                ExtractedFiles = extractedFiles
            };
        }
    }
}
