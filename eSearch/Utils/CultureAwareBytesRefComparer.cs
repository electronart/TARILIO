using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Utils
{
    public class CultureAwareBytesRefComparer : Comparer<BytesRef>
    {
        private readonly StringComparer _stringComparer;

        public CultureAwareBytesRefComparer(CultureInfo culture, bool ignoreCase)
        {
            _stringComparer = StringComparer.Create(culture, ignoreCase);
        }

        public override int Compare(BytesRef? x, BytesRef? y)
        {
            if (x == null || y == null)
            {
                if (x == y) return 0;
                return x == null ? -1 : 1;
            }

            // Convert BytesRef to strings (assuming UTF-8 encoding)
            string xString = x.Utf8ToString();
            string yString = y.Utf8ToString();

            // Compare using the culture-specific StringComparer
            return _stringComparer.Compare(xString, yString);
        }
    }
}
