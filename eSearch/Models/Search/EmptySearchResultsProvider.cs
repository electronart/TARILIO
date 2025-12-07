using eSearch.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search
{
    public class EmptySearchResultsProvider : IVirtualReadOnlyObservableCollectionProvider<ResultViewModel>
    {
        public ResultViewModel this[int index] => throw new ArgumentOutOfRangeException("There are no results");

        object? IList.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int Count => 0;

        public bool IsReadOnly => true;

        public bool IsFixedSize => true;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public int Add(object? value)
        {
            throw new NotSupportedException();
        }

        public void Add(ResultViewModel item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(object? value)
        {
            throw new NotSupportedException();
        }

        public bool Contains(ResultViewModel item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(ResultViewModel[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<ResultViewModel> GetEnumerator()
        {
            yield break;
        }

        public int IndexOf(ResultViewModel item)
        {
            return 0;
        }

        public int IndexOf(object? value)
        {
            if (value is ResultViewModel rvm)
            {
                return IndexOf(rvm);
            }
            throw new NotSupportedException();
        }

        public void Insert(int index, object? value)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, ResultViewModel item)
        {
            throw new NotSupportedException();
        }

        public void Remove(object? value)
        {
            throw new NotSupportedException();
        }

        public bool Remove(ResultViewModel item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield break;
        }
    }
}
