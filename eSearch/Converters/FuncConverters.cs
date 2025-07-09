using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Converters
{
    public static class FuncConverters
    {
        public static readonly IMultiValueConverter SelectString = new FuncMultiValueConverter<object, string>(x =>
        {
            var values = x.ToList();
            if (values[0] is bool b)
            {
                if (values[1] is string s1 && values[2] is string s2)
                {
                    return b? s1 : s2;
                }
            }
            return "";
            throw new NotSupportedException("Invalid Parameters. First Parameter should be a bool, 2nd and 3rd strings");
        });
    }
}
