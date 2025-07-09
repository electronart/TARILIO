using com.sun.istack.@internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search
{
    public struct CustomSortingCellValue : IComparable
    {
        public required object value;

        public int CompareTo(object? obj)
        {
            if (this.value is long long1 && obj is CustomSortingCellValue compareCell && compareCell.value is long long2)
            {
                return long1.CompareTo(long2);
            }
            else
            {
                return (this.value?.ToString() ?? "-").CompareTo(obj?.ToString() ?? "-");
            }
        }

        public override string ToString()
        {
            return value?.ToString() ?? "-";
        }


    }
}
