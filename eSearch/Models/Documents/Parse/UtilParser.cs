/*
using ICSharpCode.SharpZipLib.Zip;
using NPOI.SS.Formula.Functions;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Documents.Parse
{
    class UtilParser
    {

        private UtilParser()
        {
        }

        // does charset detection
        // does not close the given InputStream
        public static Source getSource(Stream inputStream)
        {
            Reader reader = new StringReader(CharsetDetectorHelper.toString(inputStream));
		    Source source = new Source(reader);
		    return source;
	    }

        public static Source getSource(ZipArchive file, String entryPath) 
        {
		    ZipArchiveEntry entry = file.GetEntry(entryPath);
		    if (entry == null) {
			    // Apparently, ZipFile.getEntry expects forward slashes even on Windows
			    entry = file.GetEntry(entryPath.Replace("\\", "/"));
			    if (entry == null) 
                {
				    throw new ArgumentException("Unknown file type");
                }
		    }

            Stream inputStream = entry.Open();
            try
            {
                Source source = getSource(inputStream);
            } finally { inputStream.Close(); }
            source.setLogger(null);
            return source;
	    }
	
	        
        public static String extract(Element e)
        {
            if (e == null)
            {
                return null;
            }
            return e.getContent().getTextExtractor().toString();
        }

        public static String render(Segment e)
        {
            return e.getRenderer().setIncludeHyperlinkURLs(false).toString();
        }

        public static void closeZipFile(ZipFile zipFile)
        {
            // We can't use Closeables.closeQuietly for ZipFiles because it doesn't
            // implement the Closeable interface on Mac OS X.
            if (zipFile == null)
                return;
            try
            {
                zipFile.close();
            }
            catch (IOException e)
            {
            }
        }
	
    }
}
*/
