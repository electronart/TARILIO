using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search
{
    public interface IDataColumnSortable
    {
        public void SortOnDataColumn(DataColumn column, bool Ascending);
    }
}
