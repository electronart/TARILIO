using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search.LuceneCustomFieldComparers
{
    public interface IFieldValueProcessor
    {
        /// <summary>
        /// Transform a raw field value for sorting purposes
        /// 
        /// Must not throw exceptions, returns the original string on failure or nothing to process.
        /// </summary>
        /// <param name="fieldValue"></param>
        /// <returns></returns>
        public string ProcessFieldValueForSorting(string fieldValue);
    }
}
