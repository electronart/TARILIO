using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search
{
    public interface IVirtualReadOnlyObservableCollectionProvider<T> : IEnumerable<T>, IList, INotifyCollectionChanged
    {

    }
}
