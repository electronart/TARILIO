using eSearch.Interop;
using eSearch.Models.Documents.Parse;
using eSearch.Models.Documents.Parse.ToxyParsers;
using FileSignatures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToxyParsers = eSearch.Models.Documents.Parse.ToxyParsers;

namespace eSearch.Models.Documents
{
    public class FileSystemDocument : IDocument
    {



        public IDocument.SkipReason ShouldSkipIndexing
        {
            get
            {
                return _shouldSkipIndexingThisDocument;
            }
            set
            {
                _shouldSkipIndexingThisDocument = value;
            }
        }

        private IDocument.SkipReason _shouldSkipIndexingThisDocument = IDocument.SkipReason.DontSkip;

        List<IParser> Parsers
        {
            get
            {
                if (_parsers == null)
                {
                    _parsers = new List<IParser>()
                    {
                        new CSVParser_Sep(),
                        new EconvoParser(),
                        //new CSVParser(),
                        new DocParser(),
                        new DocXParser(),
                        new PptParser(),
                        new PptXParser(),
                        new ToxyParsers.RTFParser(),
                        // new ToxyParsers.CSVParser(), - Disused - Prefer plain text parser because we use the raw csv data at render time.
                        new ToxyParsers.Excel2003Parser(),
                        new EmlParser(),
                        new PdfParserPDFPig(),
                        new PlainTextParser(),
                        new EpubParser2(),
                        new PlainTextParser(),
                        new XlsXParser(),
                        new PSTParser(),
                        new HtmlParser(),
                        new XmlParser(),
                        new TagLibSharpParser(),
                        new ArchiveParser(),
                        new MarkDownParserMarkDig(),
                        new IpynbParser(),
                        new JsonLParser(),
                        
                        new TikaParser3() // TikaParser should always be last. It will be used as fallback.
                    };
                }
                return _parsers;
            }
        }


        List<string> KnownExecutableExtensions = new List<string>
        {
    ".dll",
    ".dat",
    ".fap",
    ".apk",
    ".jar",
    ".ahk",
    ".ipa",
    ".run",
    ".cmd",
    ".xbe",
    ".0xe",
    ".rbf",
    ".vlx",
    ".workflow",
    ".u3p",
    ".bms",
    ".exe",
    ".bin",
    ".x86",
    ".8ck",
    ".elf",
    ".air",
    ".gadget",
    ".xap",
    ".app",
    ".x86_64",
    ".widget",
    ".shortcut",
    ".mcr",
    ".mpk",
    ".fba",
    ".ac",
    ".com",
    ".xlm",
    ".rxe",
    ".appimage",
    ".pif",
    ".tpk",
    ".73k",
    ".script",
    ".scpt",
    ".out",
    ".command",
    ".ex5",
    ".celx",
    ".scb",
    ".ba_",
    ".scr",
    ".paf.exe",
    ".scar",
    ".isu",
    ".fas",
    ".xex",
    ".action",
    ".tcp",
    ".acc",
    ".shb",
    ".rfu",
    ".ebs2",
    ".hta",
    ".cgi",
    ".sk",
    ".ex_",
    ".xbap",
    ".nexe",
    ".ecf",
    ".fxp",
    ".vpm",
    ".plsc",
    ".rpj",
    ".ws",
    ".cof",
    ".dld",
    ".mlx",
    ".vbs",
    ".vxp",
    ".caction",
    ".wsh",
    ".mm",
    ".plx",
    ".mcr",
    ".ex_",
    ".iim",
    ".phar",
    ".89k",
    ".epk",
    ".server",
    ".fpi",
    ".a7r",
    ".wcm",
    ".mel",
    ".gpe",
    ".esh",
    ".dek",
    ".cheat",
    ".pex",
    ".pyc",
    ".exe1",
    ".jsf",
    ".jsx",
    ".acr",
    ".ex4",
    ".pwc",
    ".ear",
    ".icd",
    ".vexe",
    ".cel",
    ".rox",
    ".snap",
    ".azw2",
    ".zl9",
    ".rgs",
    ".paf",
    ".mcr",
    ".ms",
    ".89z",
    ".atmx",
    ".gm9",
    ".tiapp",
    ".uvm",
    ".pyo",
    ".actc",
    ".applescript",
    ".frs",
    ".otm",
    ".msl",
    ".hms",
    ".n",
    ".widget",
    ".csh",
    ".mrc",
    ".wiz",
    ".beam",
    ".tms",
    ".ebs",
    ".cyw",
    ".spr",
    ".osx",
    ".sct",
    ".ebm",
    ".mrp",
    ".fky",
    ".xqt",
    ".ygh",
    ".fas",
    ".app",
    ".actm",
    ".udf",
    ".mxe",
    ".kix",
    ".seed",
    ".kx",
    ".vbscript",
    ".app",
    ".ezs",
    ".thm",
    ".lo",
    ".vbe",
    ".e_e",
    ".gs",
    ".jse",
    ".pxo",
    ".hpf",
    ".wpk",
    ".s2a",
    ".exz",
    ".rfs",
    ".dmc",
    ".scptd",
    ".tipa",
    ".ms",
    ".xys",
    ".mhm",
    ".ls",
    ".ita",
    ".sca",
    ".prc",
    ".eham",
    ".qit",
    ".wsf",
    ".es",
    ".arscript",
    ".rbx",
    ".mem",
    ".sapk",
    ".ebacmd",
    ".ipk",
    ".mam",
    ".ncl",
    ".ksh",
    ".dxl",
    ".upx",
    ".ham",
    ".btm",
    ".gpu",
    ".mio",
    ".vdo",
    ".ipf",
    ".exopc",
    ".ds",
    ".mac",
    ".sbs",
    ".cfs",
    ".sts",
    ".asb",
    ".pvd",
    ".qpx",
    ".wpm",
    ".afmacro",
    ".afmacros",
    ".uw8",
    ".srec",
    ".mlappinstall",
    ".rpg",
    ".p",
    ".ore",
    ".ezt",
    ".73p",
    ".smm",
};

        List<string> KnownIndexExtensions = new List<string>
        {
            ".ix",
            ".cfe",
            ".si",
            ".gen"
        };

        List<IParser> _parsers = null;


        /// <summary>
        /// The location of the file on the file system.
        /// </summary>
        string path;

        string? _text = null;
        string? _displayName = null;
        string _id;
        string? _parser = null;

        /// <summary>
        /// Construct FileSystemDocument. This object gets reused for many documents for efficiency.
        /// </summary>
        public FileSystemDocument() {

        }

        /// <summary>
        /// Set the current Document
        /// </summary>
        /// <param name="path">Full Path + extension of Document on File System</param>
        public void SetDocument(string path)
        {
            this.path = path;
            this._id = Guid.NewGuid().ToString();
            this._text = null;
            this._displayName = null;
            this._parser = null;
        }

        public FileSystemDocument(
            string identifier, string displayName, string text, string fileName, DateTime indexedDate, 
            List<Metadata> metadata, string? htmlRender)
        {
            this._id = identifier;
            _displayName = displayName;
            _text = text;
            path = fileName;
            this.IndexedDate = indexedDate;
            this.MetaData = metadata;
            this.HtmlRender = htmlRender;
        }

        public string GetPath()
        {
            return this.path;
        }

        public string Identifier
        {
            get { return _id; }
        }

        public string Parser
        {
            get
            {
                if (_parser == null )
                {
                    ExtractDataFromDocument();
                }
                return _parser;
            }
        }

        public string? DisplayName
        {
            get {
                if (_displayName == null) ExtractDataFromDocument();
                return _displayName; 
            }
        }

        public string? Text
        {
            get
            {
                if (_text == null) ExtractDataFromDocument();
                return _text;
            }
        }

        public string? FileName
        {
            get
            {
                return path;
            }
        }

        public long FileSize
        {
            get
            {
                try
                {
                    if (System.IO.File.Exists(FileName))
                    {
                        var fileInfo = new FileInfo(FileName);
                        return fileInfo.Length;
                    }
                } catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
                return 0L;
            }
        }

        public DateTime? CreatedDate
        {
            get
            {
                if (File.Exists(path))
                {
                    return File.GetCreationTime(path).ToUniversalTime();
                }
                return null;
            }
        }

        public DateTime? ModifiedDate
        {
            get
            {
                if (File.Exists(path))
                {
                    return File.GetLastWriteTime(path).ToUniversalTime();
                }
                return null;
            }
        }

        public DateTime? IndexedDate
        {
            get
            {
                return _indexedDate;
            }
            set
            {
                _indexedDate = value;
            }
        }

        public DateTime? AccessedDate
        {
            get
            {
                if (File.Exists(path))
                {
                    return File.GetLastAccessTime(path).ToUniversalTime();
                }
                return null;
            }
        }

        public IEnumerable<IMetaData> MetaData
        {
            get
            {
                return _metadata;
            } set
            {
                _metadata = value.ToList();
            }
        }

        List<IMetaData> _metadata;

        public IEnumerable<IDocument>? SubDocuments
        {
            get
            {
                if (_subDocuments == null) ExtractDataFromDocument();
                return _subDocuments;
            } set
            {
                _subDocuments = value;
            }
        }

        public int TotalKnownSubDocuments { get; set; } = 0;

        IEnumerable<IDocument>? _subDocuments = null;

        private DateTime? _indexedDate = null;


        public IEnumerable<string> ExtractedFiles
        {
            get
            {
                if (_extractedFiles == null) ExtractDataFromDocument();
                return _extractedFiles;
            }
        }

        private List<string> _extractedFiles = null;

        public string HtmlRender
        {
            get
            {
                if (_htmlRender == null) ExtractDataFromDocument();
                return _htmlRender;
            } set
            {
                _htmlRender = value;
            }
        }

        private string? _htmlRender = null;


        public void ExtractDataFromDocument()
        {
            _text = null;
            _displayName = null;
            _id = null;

            try
            {
                ParseResult parseResult = GetParseResult();
                //Debug.WriteLine("Parsed Output:");
                //Debug.WriteLine(parseResult.TextContent);
                _text = parseResult.TextContent;
                _displayName = parseResult.Title;
                _metadata = parseResult.Metadata;
                _parser = parseResult.ParserName;
                _shouldSkipIndexingThisDocument = parseResult.SkipIndexingDocument;
                _extractedFiles = parseResult.ExtractedFiles;
                _htmlRender = parseResult.HtmlRender ?? "";
                _subDocuments = parseResult.SubDocuments;
                TotalKnownSubDocuments = parseResult.TotalKnownSubDocuments;
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                _text = ex.ToString();
                _displayName = Path.GetFileName(path);
                _shouldSkipIndexingThisDocument = IDocument.SkipReason.ParseError;
            }
        }

        /// <summary>
        /// Will calculate the extension and return it lowercase without the .
        /// TODO Will use magic numbers rather than solely relying on the file path.
        /// </summary>
        /// <returns></returns>
        public string FileType
        {
            get
            {
                try
                {
                    bool testForMagicNumbers = true;
                    string extension = Path.GetExtension(FileName).ToLower().Substring(1);
                    if (extension == "epub" || extension == "ipynb")
                    {
                        testForMagicNumbers = false;
                    }
                    #region Magic Number Testing
                    if (testForMagicNumbers)
                    {
                        try
                        {
                            var inspector = new FileFormatInspector();
                            using (var stream = new FileStream(FileName, FileMode.Open, FileAccess.Read))
                            {
                                var format = inspector.DetermineFileFormat(stream);
                                if (format != null)
                                {
                                    return format.Extension;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Consider this non fatal.
                            // TODO Logging.
                        }
                    }
                    #endregion
                    return extension; // Exclude the .
                }
                catch (ArgumentException ex)
                {
                    // Invalid path.
                    return "Unknown";
                }
            }
        }

        public bool IsVirtualDocument
        {
            get
            {
                return false; // FileSystemDocuments are not virtual documents.
            }
        }

        public ParseResult GetParseResult()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string extension = FileType;
            extension = extension.Replace(".", "").ToLower();

            if (KnownExecutableExtensions.Contains("." + extension))
            {
                return new ParseResult
                {
                    SkipIndexingDocument = IDocument.SkipReason.Executable,
                    TextContent = "Skipped - Executable"
                };
            }
            if (KnownIndexExtensions.Contains("." + extension))
            {
                return new ParseResult
                {
                    SkipIndexingDocument = IDocument.SkipReason.IndexFile,
                    TextContent = "Skipped - Index file"
                };
            }

            IParser docParser = null;
            foreach (var parser in Parsers)
            {
                if (parser.Extensions.Contains(extension))
                {
                    docParser = parser;
                    break;
                }
            }

            if (docParser == null)
            {
                return new ParseResult
                {
                    SkipIndexingDocument = IDocument.SkipReason.UnsupportedFileFormat,
                    TextContent = "Skipped - Unrecognised File Format"
                };
            }
            docParser.Parse(path, out var parseResult);
            return parseResult;
        }
    }
}
