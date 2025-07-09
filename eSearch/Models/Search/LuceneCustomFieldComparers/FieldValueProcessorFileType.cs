using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search.LuceneCustomFieldComparers
{
    internal class FieldValueProcessorFileType : IFieldValueProcessor
    {
        public string ProcessFieldValueForSorting(string fieldValue)
        {
            try
            {
                return Path.GetExtension(fieldValue);
            } catch
            {
                return fieldValue;
            }
        }
    }
}
