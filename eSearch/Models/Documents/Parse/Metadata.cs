using eSearch.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace eSearch.Models.Documents.Parse
{
    public class Metadata : IMetaData
    {
        public required string Key { get; set; }
        /// <summary>
        /// Value may be the following types:
        /// - string
        /// - Metadata
        /// - null
        /// </summary>
        public required string Value { get; set; }

    }
}
