using com.sun.org.apache.bcel.@internal.generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Documents.Parse
{
    internal class PptParser : IParser
    {
        public string[] Extensions
        {
            get { return new string[] { }; }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            throw new NotImplementedException();
        }
    }
}
