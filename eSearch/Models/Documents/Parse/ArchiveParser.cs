using org.apache.pdfbox.cos;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
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
                return new string[] { "tar", "zip", "rar", "gz", "bz2", "7z"  };  
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

            string extension = Path.GetExtension(filePath).ToLower();
            if (extension == ".7z")
            {
                // The 7Zip format doesn't allow for reading as a forward-only stream so 7Zip is only supported through the Archive API
                // Note: Extracting a solid rar or 7z file needs to be done in sequential order to get acceptable decompression speed.
                // It is explicitly recommended to use ExtractAllEntries when extracting an entire IArchive instead of iterating over all its Entries.

                // Since I can't iterate over files, I'm going to create a unique folder for the extractions and treat all files in that folder as
                // being our extracted files.
                string tmp_dir = Path.Combine(Program.ESEARCH_TEMP_FILES_PATH, "Extractions", Guid.NewGuid().ToString());
                if (Directory.Exists(tmp_dir))
                {
                    Directory.Delete(tmp_dir, true);
                }
                Directory.CreateDirectory(tmp_dir);

                using (var archive = SevenZipArchive.Open(filePath))
                {
                    using (var reader = archive.ExtractAllEntries())
                    {
                        reader.WriteAllToDirectory(tmp_dir, new SharpCompress.Common.ExtractionOptions()
                        {
                            ExtractFullPath = false, Overwrite = true
                        });
                    }
                }

                // Next, discover all extracted files...

                string[] extracted_files = Directory.GetFiles(tmp_dir);
                foreach(var extracted_file in extracted_files)
                {
                    sb.AppendLine(Path.GetFileName(extracted_file));
                    parseResult.ExtractedFiles.Add(extracted_file);
                }
            }
            else
            {
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
            }
            parseResult.TextContent = sb.ToString();
            parseResult.Title = Path.GetFileNameWithoutExtension(filePath);
        }
    }
}
