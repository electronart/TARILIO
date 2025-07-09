using eSearch.Interop;
using eSearch.Interop.IDataSourceExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Documents
{
    internal class FileParserForPlugins : IESearchFileParser
    {
        

        public IDocument ParseFile(string fileName)
        {
            FileSystemDocument fsd = new FileSystemDocument();
            fsd.SetDocument(fileName);
            return fsd;
        }
    }
}
