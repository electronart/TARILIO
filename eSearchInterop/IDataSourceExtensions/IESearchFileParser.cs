using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Text;

namespace eSearch.Interop.IDataSourceExtensions
{
    public interface IESearchFileParser
    {
        /// <summary>
        /// Pass the path to a file on the filesystem for eSearch to Parse. It will return an IDocument.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public IDocument ParseFile(string fileName);
    }
}
