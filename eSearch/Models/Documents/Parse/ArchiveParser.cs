using SharpCompress.Archives;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Documents.Parse
{
    public class ArchiveParser : IParser
    {
        public string[] Extensions {
            get { 
                return new string[] { "tar", "zip", "rar", "gz", "bz2"  };  
            }
        }

        public bool DoesParserExtractFiles => true;

        public bool DoesParserProduceSubDocuments => false;

        public void Parse(string filePath, out ParseResult parseResult)
        {
            // https://github.com/adamhathcock/sharpcompress/wiki/API-Examples

            parseResult = new ParseResult
            {
                ParserName = "ArchiveParser (SharpCompress)"
            };

            var sb = new StringBuilder();
            sb.AppendLine("Contents:").AppendLine();

            using (var stream = File.OpenRead(filePath))
            {
                var reader = ReaderFactory.Open(stream);
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        string fileName = reader.Entry.Key;
                        sb.AppendLine(fileName);

                        string output_dir = Path.Combine(Program.ESEARCH_TEMP_FILES_PATH, "Extractions");
                        Directory.CreateDirectory(output_dir);
                        reader.WriteEntryToDirectory(output_dir, new SharpCompress.Common.ExtractionOptions { ExtractFullPath = false, Overwrite = true });
                        parseResult.ExtractedFiles.Add(Path.Combine(output_dir, Path.GetFileName(fileName)));
                    }
                }
            }
            parseResult.TextContent = sb.ToString();
            parseResult.Title = Path.GetFileNameWithoutExtension(filePath);
        }
    }
}
