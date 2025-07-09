using com.github.junrar.unpack.decode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopSearch2.Models.Search
{
    public class QueryFilter
    {
        public enum FilterType
        {
            Text,
            Date // For later support
        }

        public FilterType Type = FilterType.Text;

        public string FieldName;

        public string SearchText;

    }
}
