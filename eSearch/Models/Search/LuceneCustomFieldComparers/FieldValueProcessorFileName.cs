using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search.LuceneCustomFieldComparers
{
    internal class FieldValueProcessorFileName : IFieldValueProcessor
    {
        public string ProcessFieldValueForSorting(string fieldValue)
        {
            try
            {
                return Path.GetFileName(fieldValue).ToLowerInvariant();
            }
            catch { return fieldValue; }
        }
    }
}
