
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using com.sun.corba.se.spi.orb;
using eSearch.Interop;

namespace eSearch.Models.Documents.Parse
{
    /// <summary>
    /// This version uses Apache Tika Server, by using the server don't have to launch java/tika for every file, making indexing faster.
    /// </summary>
    internal class TikaParser3 : IParser
    {
        public string[] Extensions
        {
            get { 
                return new string[] { 
                        // application/java-vm
                    "class",
    
                    // audio/x-wav
                    "wav",
                    // audio/x-aiff
                    "aiff", "aif", "aifc",
                    // audio/basic
                    "au", "snd",
    
                    // application/x-midi, audio/midi
                    "mid", "midi", "kar",

                    // application/vnd.ms-htmlhelp
                    "chm",
    
                    // text/x-java-source
                    "java",
                    // text/x-c++src
                    "cpp", "cxx", "cc",
                    // text/x-groovy
                    "groovy",

                    // application/pkcs7-signature
                    "p7s",
                    // application/pkcs7-mime
                    "p7m", "p7c",

                    // application/dif+xml
                    "dif",

                    // image/vnd.dwg
                    "dwg",

                    // application/x-ibooks+zip
                    "ibooks",
                    // application/epub+zip
                    "epub",

                    // application/x-elf
                    "elf",
                    // application/x-sharedlib
                    "so",
                    // application/x-executable
                    "exe",
                    // application/x-msdownload
                    "exe", "dll",
                    // application/x-coredump
                    "core",
                    // application/x-object
                    "o", "obj",

                    // application/atom+xml
                    "atom",
                    // application/rss+xml
                    "rss",

                    // application/x-font-adobe-metric
                    "afm",

                    // application/x-font-ttf
                    "ttf",

                    // image/x-ozi
                    "ozi",
                    // application/x-snodas
                    "snodas",
                    // image/envisat
                    "envisat",
                    // application/fits
                    "fits",
                    // image/adrg
                    "adrg",
                    // image/gif
                    "gif",
                    // image/jp2
                    "jp2",
                    // image/hfa
                    "hfa",
                    // image/fits
                    "fits",
                    // image/raster
                    "raster",
                    // image/x-srp
                    "srp",
                    // image/arg
                    "arg",
                    // image/big-gif
                    "big-gif",
                    // image/ceos
                    "ceos",
                    // image/bmp
                    "bmp",
                    // image/ilwis
                    "ilwis",
                    // image/x-hdf5-image
                    "h5",
                    // image/sar-ceos
                    "sar",
                    // image/nitf
                    "nitf",
                    // image/png
                    "png",
                    // image/geotiff
                    "tif", "tiff",
                    // image/x-mff2
                    "mff",
                    // image/ida
                    "ida",
                    // image/jpeg
                    "jpg", "jpeg",
                    // image/bsb
                    "bsb",
                    // image/x-mff
                    "mff",
                    // image/x-dimap
                    "dimap",
                    // image/x-pcraster
                    "pcraster",
                    // image/sgi
                    "sgi",
                    // image/x-fujibas
                    "fujibas",
                    // image/x-airsar
                    "airsar",

                    // text/iso19139+xml
                    "iso19139",

                    // application/x-hdf
                    "hdf",

                    // application/x-asp
                    "asp",
                    // application/xhtml+xml
                    "xhtml",
                    // text/html
                    "html", "htm",

                    // image/bpg
                    "bpg",
                    // image/x-bpg
                    "bpg",

                    // image/x-ms-bmp
                    "bmp",
                    // image/x-icon
                    "ico",
                    // image/vnd.wap.wbmp
                    "wbmp",
                    // image/x-xcf
                    "xcf",

                    // image/vnd.adobe.photoshop
                    "psd",

                    // image/webp
                    "webp",

                    // text/vnd.iptc.anpa
                    "anpa",

                    // application/x-isatab
                    "isatab",

                    // application/vnd.apple.iwork
                    "iwork",
                    // application/vnd.apple.numbers
                    "numbers",
                    // application/vnd.apple.keynote
                    "key",
                    // application/vnd.apple.pages
                    "pages",

                    // message/rfc822
                    "eml",

                    // application/x-matlab-data
                    "mat",

                    // application/mbox
                    "mbox",

                    // application/vnd.ms-outlook-pst
                    "pst",

                    // application/x-msaccess
                    "mdb", "accdb",

                    // application/vnd.ms-excel
                    "xls", "xlsx",
                    // application/vnd.ms-powerpoint
                    "ppt", "pptx",
                    // application/msword
                    "doc", "docx",
                    // application/vnd.visio
                    "vsd", "vsdx",

                    // application/x-tnef
                    "tnef",
                    // application/vnd.ms-tnef
                    "tnef",

                    // audio/mpeg
                    "mp3",

                    // video/3gpp
                    "3gp",
                    // video/mp4
                    "mp4",
                    // video/quicktime
                    "mov",
                    // audio/mp4
                    "m4a",
                    // application/mp4
                    "mp4",

                    // image/tiff
                    "tif", "tiff",

                    // application/pdf
                    "pdf",

                    // application/zip
                    "zip",
                    // application/x-7z-compressed
                    "7z",
                    // application/x-rar-compressed
                    "rar",

                    // application/rtf
                    "rtf",

                    // text/plain
                    "txt",

                    // video/x-flv
                    "flv",

                    // application/xml
                    "xml",
                    // image/svg+xml
                    "svg",

                    // audio/x-flac
                    "flac",
                    // audio/ogg
                    "ogg",

                    // audio/opus
                    "opus",

                    // audio/speex
                    "spx",

                    // audio/vorbis
                    "ogg", "oga"
                }; 
            }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            if (TikaServer.TryExtractDocumentToHTML(filePath, out string extractedHTML))
            {
                HtmlParser parser = new HtmlParser();
                parser.ParseText(extractedHTML, out parseResult);
                parseResult.Title = Path.GetFileNameWithoutExtension(filePath);
                parseResult.ParserName += "TikaServer + " + parseResult.ParserName;
            } else
            {
                parseResult = new ParseResult
                {
                    ParserName = "TikaServer / TikaParser ",
                    SkipIndexingDocument = IDocument.SkipReason.ParseError,
                    TextContent = extractedHTML
                };
            }
        }
    }
}
