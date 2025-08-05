using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Indexing
{
    public class IndexSchedule
    {
        public DateTime     StartingFrom;
        public IntervalSize IntervalSize;
        public int          Interval;
    }

    public enum IntervalSize
    {
        Day,
        Week,
    }
}
