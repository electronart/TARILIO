using eSearch.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Models.Documents
{
    public class DocumentType
    {
        public readonly string Extension;
        public readonly string Description;

        public DocumentType(string extension, string description)
        {
            this.Extension = extension;
            this.Description = description;
        }

        public static ObservableCollection<TreeNode> GetDocumentTypeTreeNodeHeirachy(IEnumerable<string> SelectedExtensions)
        {
            ObservableCollection<TreeNode> treeNodes = new ObservableCollection<TreeNode>();
            // Documents
            treeNodes.Add(BuildTreeNode("##Documents", S.Get("Documents"), DocumentFormats, SelectedExtensions));
            // Presentations
            treeNodes.Add(BuildTreeNode("##Presentations", S.Get("Presentations"), PresentationFormats, SelectedExtensions));
            // Publications
            treeNodes.Add(BuildTreeNode("##Publications", S.Get("Publications"), PublicationFormats, SelectedExtensions));
            // Text
            treeNodes.Add(BuildTreeNode("##Text", S.Get("Text"), TextFormats, SelectedExtensions));
            // Email
            treeNodes.Add(BuildTreeNode("##Email", S.Get("Emails"), EmailFormats, SelectedExtensions));
            // Spreadsheet
            treeNodes.Add(BuildTreeNode("##Spreadsheets", S.Get("Spreadsheets"), SpreadsheetFormats, SelectedExtensions));
            // Audio
            treeNodes.Add(BuildTreeNode("##Audio", S.Get("Audio"), AudioFormats, SelectedExtensions));
            // Image
            treeNodes.Add(BuildTreeNode("##Images", S.Get("Images"), ImageFormats, SelectedExtensions));
            // Video
            treeNodes.Add(BuildTreeNode("##Video", S.Get("Videos"), VideoFormats, SelectedExtensions));
            // Data
            treeNodes.Add(BuildTreeNode("##Data", S.Get("Data"), DataExchangeFormats, SelectedExtensions));
            // Archives
            treeNodes.Add(BuildTreeNode("##Archives", S.Get("Archives"), ArchiveFormats, SelectedExtensions));
            // Source Code
            treeNodes.Add(BuildTreeNode("##SourceCode", S.Get("Source Code"), SourceCodeFormats, SelectedExtensions));
            
            return treeNodes;
        }

        private static TreeNode BuildTreeNode(string CategoryInternalName, string CategoryTitle, DocumentType[] docTypes, IEnumerable<string> SelectedExtensions)
        {

            List<TreeNode> subNodes = new List<TreeNode>();
            foreach(var docType in docTypes)
            {
                bool isChecked = SelectedExtensions.Contains(CategoryInternalName) || SelectedExtensions.Contains(docType.Extension);
                bool isEnabled = true;
                subNodes.Add(new TreeNode(docType.Description + " (." + docType.Extension + ")", isEnabled, isChecked, docType));
            }
            TreeNode mainNode = new TreeNode(CategoryTitle, true, SelectedExtensions.Contains(CategoryInternalName), new DocumentType(CategoryInternalName, "Internal Category - Not displayed on UI"));
            foreach(var subNode in subNodes)
            {
                mainNode.AddSubNode(subNode);
            }
            return mainNode;
        }

        public static DocumentType[] DocumentFormats
        {
            get
            {
                return new DocumentType[]
                {
                    new DocumentType("doc",   "Microsoft Office Word 97-2003"),
                    new DocumentType("docx",  "Microsoft Office Word OpenXML"),
                    new DocumentType("odt",   "OpenDocument Text"),
                    new DocumentType("pages", "iWorks Pages"),
                    new DocumentType("wpd",   "WordPerfect"),
                    new DocumentType("ipynb", "Jupyter Notebook")

                    
                };
            }
        }

        public static DocumentType[] SourceCodeFormats
        {
            get
            {
                return new DocumentType[]
                {
                    new DocumentType("asm", "Assembly Language"),
                    new DocumentType("bat", "Batch File"),
                    new DocumentType("c", "C"),
                    new DocumentType("cs", "C Sharp"),
                    new DocumentType("clj", "Clojure"),
                    new DocumentType("cpp", "C++"),
                    new DocumentType("coffee", "CoffeeScript"),
                    new DocumentType("dart", "Dart"),
                    new DocumentType("elm", "Elm"),
                    new DocumentType("erl", "Erlang"),
                    new DocumentType("fs", "F#"),
                    new DocumentType("go", "Go"),
                    new DocumentType("groovy", "Groovy"),
                    new DocumentType("hs", "Haskell"),
                    new DocumentType("java", "Java"),
                    new DocumentType("js", "JavaScript"),
                    new DocumentType("jsx", "JavaScript JSX"),
                    new DocumentType("jl", "Julia"),
                    new DocumentType("kt", "Kotlin"),
                    new DocumentType("lisp", "Lisp"),
                    new DocumentType("lua", "Lua"),
                    new DocumentType("m", "Objective-C"),
                    new DocumentType("ml", "OCaml"),
                    new DocumentType("pl", "Perl"),
                    new DocumentType("php", "PHP"),
                    new DocumentType("pro", "Prolog"),
                    new DocumentType("ps1", "PowerShell Script"),
                    new DocumentType("py", "Python"),
                    new DocumentType("r", "R"),
                    new DocumentType("rkt", "Racket"),
                    new DocumentType("rb", "Ruby"),
                    new DocumentType("rs", "Rust"),
                    new DocumentType("scss", "Sass (SCSS)"),
                    new DocumentType("sass", "Sass"),
                    new DocumentType("scala", "Scala"),
                    new DocumentType("sh", "Shell Script"),
                    new DocumentType("swift", "Swift"),
                    new DocumentType("sql", "SQL"),
                    new DocumentType("ts", "TypeScript"),
                    new DocumentType("tsx", "TypeScript JSX"),
                    new DocumentType("v", "Verilog"),
                    new DocumentType("vhd", "VHDL"),
                    new DocumentType("vb", "Visual Basic"),
                };
            }
        }

        public static DocumentType[] PresentationFormats
        {
            get
            {
                return new DocumentType[]
                {
                    new DocumentType("ppt", "Microsoft Office PowerPoint 97-2003"),
                    new DocumentType("pptx", "Microsoft Office PowerPoint OpenXML"),
                    new DocumentType("odp", "OpenDocument Presentation"),
                    new DocumentType("key", "Keynote")
                };
            }
        }

        public static DocumentType[] PublicationFormats
        {
            get
            {
                return new DocumentType[]
                {
                    new DocumentType("epub", "Electronic Publication"),
                    new DocumentType("pdf",  "Portable Document Format"),
                    new DocumentType("html", "Hypertext Markup Language"),
                    new DocumentType("htm", "Hypertext Markup Language"),
                };
            }
        }


        public static DocumentType[] TextFormats
        {
            get
            {
                return new DocumentType[]
                {
                    new DocumentType("rtf", "Rich Text Format"),
                    new DocumentType("txt", "Text file"),
                    new DocumentType("tex", "TeX"),
                    new DocumentType("md", "Markdown"),
                    new DocumentType("markdown", "Markdown")
                };
            }
        }

        public static DocumentType[] EmailFormats
        {
            get
            {
                return new DocumentType[]
                {
                    new DocumentType("eml","Electronic Mail"),
                    new DocumentType("emlx","Apple Mail"),
                    new DocumentType("msg","Outlook Message"),
                    new DocumentType("oft","Outlook File Template"),
                    new DocumentType("ost","Outlook Off-line Storage Table"),
                    new DocumentType("pst","Outlook Personal Storage Table"),
                    new DocumentType("vcf","Virtual Contact File")
                };
            }
        }

        public static DocumentType[] ArchiveFormats
        {
            get
            {
                return new DocumentType[]
                {
                    new DocumentType("rar","WinRAR"),
                    new DocumentType("bz2","Bzip2 Compressed File"),
                    new DocumentType("zip","Zipped file"),
                    new DocumentType("gz","Gnu Zipped Archive"),
                };
            }
        }

        public static DocumentType[] SpreadsheetFormats
        {
            get
            {
                return new DocumentType[]
                {
                    new DocumentType("xls", "Microsoft Office Excel 97-2003"),
                    new DocumentType("xlsx", "Microsoft Office Excel OpenXML"),
                    new DocumentType("ods", "OpenDocument Spreadsheet")
                };
            }
        }

        public static DocumentType[] DataExchangeFormats
        {
            get
            {
                return new DocumentType[]
                {
                    new DocumentType("xml", "Extensible Markup Language"),
                    new DocumentType("xliff", "XML Localization Interchange File Format"),
                    new DocumentType("tmx", "Translation Memory Exchange"),
                    new DocumentType("tbx", "TermBase eXchange"),
                    new DocumentType("json", "JavaScript Object Notation"),
                    new DocumentType("jsonl", "JSON Lines"),
                    new DocumentType("csv", "Comma-Separated Values"),
                    new DocumentType("econvo", "eSearch Conversation")
                };
            }
        }

        public static DocumentType[] AudioFormats
        {
            get
            {
                return new DocumentType[]
                {
                    new DocumentType("aa",      "Audible Audiobook"),
                    new DocumentType("aax",     "Audible Enhanced Audiobook"),
                    new DocumentType("aac",     "Advanced Audio Coding"),
                    new DocumentType("aiff",    "Audio Interchange File Format"),
                    new DocumentType("ape",     "Monkey's Audio"),
                    new DocumentType("dsf",     "Delusion Digital Sound File"),
                    new DocumentType("flac",    "Free Lossless Audio Codec"),
                    new DocumentType("m4a",     "MPEG-4 Audio"),
                    new DocumentType("m4b",     "MPEG-4 Audiobook"),
                    new DocumentType("m4p",     "iTunes Music Store Audio"),
                    new DocumentType("mp3",     "MP3 Audio"),
                    new DocumentType("mpc",     "Musepack Compressed Audio"),
                    new DocumentType("mpp",     "MPEGplus"),
                    new DocumentType("ogg",     "Ogg Vorbis Audio"),
                    new DocumentType("oga",     "Ogg Vorbis Audio"),
                    new DocumentType("wav",     "Waveform Audio"),
                    new DocumentType("wma",     "Windows Media Audio"),
                    new DocumentType("wv",      "WavePack Audio"),
                    new DocumentType("webm",    "WebM Audio"),
                };
            }
        }

        public static DocumentType[] ImageFormats
        {
            get
            {
                return new DocumentType[]
                {
                    new DocumentType("bmp",      "Bitmap Image"),
                    new DocumentType("dng",      "Digital Negative Image"),
                    new DocumentType("gif",      "Graphical Interchange Format"),
                    new DocumentType("jpeg",      "Joint Photographic Experts Group"),
                    new DocumentType("jpg",      "Joint Photographic Experts Group"),
                    new DocumentType("pbm",      "Portable Bitmap"),
                    new DocumentType("pgm",      "Portable Gray Map"),
                    new DocumentType("ppm",      "Portable Pixmap"),
                    new DocumentType("pnm",      "Portable Any Map"),
                    new DocumentType("pcx",      "Paintbrush Bitmap"),
                    new DocumentType("png",      "Portable Network Graphic"),
                    new DocumentType("svg",      "Scalable Vector Graphic"),
                    new DocumentType("tiff",     "Tagged Image File Format"),
                    new DocumentType("tif",     "Tagged Image File Format"),
                    new DocumentType("webp",     "WebP Image")
                };
            }
        }

        public static DocumentType[] VideoFormats
        {
            get
            {
                return new DocumentType[]
                {
                    new DocumentType("mkv",      "Matroska Video"),
                    new DocumentType("ogv",      "Ogg Video"),
                    new DocumentType("avi",      "Audio Video Interleave"),
                    new DocumentType("wmv",      "Windows Media Video"),
                    new DocumentType("asf",      "Advanced Systems Format"),
                    new DocumentType("mp4",      "MPEG-4 Video"),
                    new DocumentType("m4v",      "iTunes Video"),
                    new DocumentType("mpeg",     "MPEG Video"),
                    new DocumentType("mpg",      "MPEG Video"),
                    new DocumentType("mpe",      "MPEG Movie"),
                    new DocumentType("mpv",      "MPEG-2 Elementary Stream"),
                    new DocumentType("m2v",      "MPEG-2 Video"),
                    new DocumentType("webm",     "WebM Video"),
                };
            }
        }

    }
}
