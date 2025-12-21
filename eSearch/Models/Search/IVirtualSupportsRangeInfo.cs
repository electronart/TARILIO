using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search
{
    public interface IVirtualSupportsRangeInfo
    {
        public void GetRangeInformation(out int ResultsStartAt, out int ResultsEndAt, out int totalResults);
    }
}
