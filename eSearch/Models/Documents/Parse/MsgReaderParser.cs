using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MsgReader.Outlook;
using eSearch.Models.Documents.Parse;
using eSearch.Interop; // Assuming this is needed for IDocument, etc.

namespace eSearch.Models.Documents.Parse
{
    internal class MsgReaderParser : IParser
    {
        public string[] Extensions => new[] { "msg" };

        public bool DoesParserExtractFiles => true;

        public bool DoesParserProduceSubDocuments => false;

        public void Parse(string filePath, out ParseResult parseResult)
        {
            parseResult = new ParseResult
            {
                ParserName = "MsgReaderParser"
            };

            using (var msg = new Storage.Message(filePath))
            {
                parseResult.Title = msg.Subject ?? "Untitled";

                var sender = msg.Sender;
                parseResult.Authors = new[] { sender?.DisplayName ?? sender?.Email ?? "Unknown" };

                // Metadata for From, To, CC, BCC
                var metadata = new List<IMetaData>();

                string fromStr = sender != null ? $"{sender.DisplayName} <{sender.Email}>" : "";
                metadata.Add(new Metadata { Key = "From", Value = fromStr });

                var toRecipients = msg.Recipients.Where(r => r.Type == RecipientType.To);
                string toStr = string.Join(", ", toRecipients.Select(r => $"{r.DisplayName} <{r.Email}>"));
                metadata.Add(new Metadata { Key = "To", Value = toStr });

                var ccRecipients = msg.Recipients.Where(r => r.Type == RecipientType.Cc);
                string ccStr = string.Join(", ", ccRecipients.Select(r => $"{r.DisplayName} <{r.Email}>"));
                metadata.Add(new Metadata { Key = "CC", Value = ccStr });

                var bccRecipients = msg.Recipients.Where(r => r.Type == RecipientType.Bcc);
                string bccStr = string.Join(", ", bccRecipients.Select(r => $"{r.DisplayName} <{r.Email}>"));
                metadata.Add(new Metadata { Key = "BCC", Value = bccStr });

                // Attachments metadata (filenames as CSV)
                if (msg.Attachments.Any())
                {
                    List<string> attachmentNames = new List<string>();
                    foreach(var attachment in msg.Attachments)
                    {
                        if (attachment is Storage.Attachment att)
                        {
                            attachmentNames.Add(att.FileName);
                        }
                    }
                    string attCsv = string.Join(", ", attachmentNames);
                    metadata.Add(new Metadata { Key = "Attachments", Value = attCsv });
                }

                parseResult.Metadata = metadata;

                // Determine if we need a temp directory (for attachments or HTML parsing)
                bool hasAttachments = msg.Attachments.Any();
                bool needsHtmlParse = string.IsNullOrEmpty(msg.BodyText) && !string.IsNullOrEmpty(msg.BodyHtml);
                string tmpDir = null;

                if (hasAttachments || needsHtmlParse)
                {
                    tmpDir = CreateTempDirectory();
                }

                // Handle HtmlRender and TextContent
                parseResult.HtmlRender = msg.BodyHtml;

                if (!string.IsNullOrEmpty(msg.BodyText))
                {
                    parseResult.TextContent = msg.BodyText;
                }
                else if (needsHtmlParse)
                {
                    // Write HTML to temp file and parse with HtmlParser for plain text
                    string htmlPath = Path.Combine(tmpDir, "body.html");
                    File.WriteAllText(htmlPath, msg.BodyHtml);

                    var htmlParser = new HtmlParser();
                    ParseResult htmlResult;
                    htmlParser.Parse(htmlPath, out htmlResult);

                    parseResult.TextContent = htmlResult.TextContent;

                    // Clean up the temp HTML file (don't add to ExtractedFiles)
                    File.Delete(htmlPath);
                }
                else
                {
                    parseResult.TextContent = "Empty or unreadable document.";
                }

                // Extract attachments if present
                if (hasAttachments)
                {
                    foreach (var attachment in msg.Attachments)
                    {
                        if (attachment is Storage.Attachment att)
                        {
                            string attPath = Path.Combine(tmpDir, att.FileName);
                            File.WriteAllBytes(attPath, att.Data);
                            parseResult.ExtractedFiles.Add(attPath);
                        }
                    }
                }

                // If no files were extracted to tmpDir, clean it up (optional, but prevents empty dirs)
                if (tmpDir != null && !parseResult.ExtractedFiles.Any() && Directory.Exists(tmpDir))
                {
                    Directory.Delete(tmpDir, true);
                }
            }
        }

        private string CreateTempDirectory()
        {
            string tmpDir = Path.Combine(Program.ESEARCH_TEMP_FILES_PATH, "Extractions", Guid.NewGuid().ToString());
            if (Directory.Exists(tmpDir))
            {
                Directory.Delete(tmpDir, true);
            }
            Directory.CreateDirectory(tmpDir);
            return tmpDir;
        }
    }
}