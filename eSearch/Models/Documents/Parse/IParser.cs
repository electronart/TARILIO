
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Documents.Parse
{
    internal interface IParser
    {
        /// <summary>
        /// Supported File Extensions of this Parser.
        /// </summary>
        string[] Extensions { get; }

        /// <summary>
        /// Parse a document.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="parseResult"></param>
        void Parse(string filePath, out ParseResult parseResult);

        bool DoesParserExtractFiles { get; }

        bool DoesParserProduceSubDocuments { get; }



    }
}
