using com.sun.org.apache.bcel.@internal.generic;
using eSearch.Models.Documents;
using eSearch.Models.Indexing;
using eSearch.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eSearch.Models.Search
{
    public class LuceneSearchResultProvider : IVirtualReadOnlyObservableCollectionProvider<ResultViewModel>, IDataColumnSortable, IVirtualSupportsCount
    {
        public required LuceneIndex         LuceneIndex;
        public required QueryViewModel      QueryViewModel;

        public required DataColumn?         SortColumn      = null;
        public required bool                SortAscending   = true;
        public required CancellationToken   CancellationToken;


        public ResultViewModel this[int index]
        {
            get 
            {
                int resultsPerPage = QueryViewModel.ResultsPerPage;
                int page = index / resultsPerPage;
                var cachedItem = PageCache.FirstOrDefault(cachePage => cachePage?.Page == page, null);
                if (cachedItem == null)
                {

                    var nfo = LuceneIndex.GetLuceneResultsBlocking(QueryViewModel, CancellationToken, page, SortColumn, SortAscending);
                    
                    var results = nfo.Results;
                    if (_numKnownResults == null)
                    {
                        _numKnownResults = nfo.TotalResults;
                    }
                    
                    cachedItem = new CachedLucenePageResults { Page = page, PageResults = results };
                    PageCache.Add(cachedItem);
                    if (PageCache.Count > 10) PageCache.RemoveAt(0);
                }

                int pageItemIndex = (index - (page * resultsPerPage));
                if (pageItemIndex >= 0 && pageItemIndex < cachedItem.PageResults.Count)
                {
                    return cachedItem.PageResults[pageItemIndex];
                } else
                {
                    Debug.WriteLine("BREAK");
                    throw new ArgumentOutOfRangeException();
                }
                
            }
        }

        public int Count
        {
            get
            {
                if (_numKnownResults == null)
                {
                    try
                    {
                        var ignored = this[0]; // This will populate _numKnownResults usually.
                    } catch (ArgumentOutOfRangeException)
                    {
                        return 0;
                    }
                    if (_numKnownResults == null) return 0;
                }
                return _numKnownResults ?? 0;
            }
        }

        public bool IsReadOnly => true;

        public bool IsFixedSize => true;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        object? IList.this[int index] { get => this[index]; set => throw new NotSupportedException(); }


        private int? _numKnownResults = null;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public IEnumerator<ResultViewModel> GetEnumerator()
        {
            int i = 0;
            while (i < Count)
            {
                yield return this[i];
                ++i;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(ResultViewModel item)
        {
            return item.ResultIndex;
        }

        public int Add(object? value)
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

        public int IndexOf(object? value)
        {
            if (value is ResultViewModel rvm)
            {
                return this.IndexOf(rvm);
            }
            return -1;
        }

        public void Insert(int index, object? value)
        {
            throw new NotSupportedException();
        }

        public void Remove(object? value)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotSupportedException();
        }

        public void SortOnDataColumn(DataColumn? column, bool Ascending)
        {
            SortColumn      = column;
            SortAscending   = Ascending;
            PageCache.Clear();
            _numKnownResults = null;
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        List<CachedLucenePageResults> PageCache = new List<CachedLucenePageResults>();

        private class CachedLucenePageResults
        {
            public required int Page;
            public required List<ResultViewModel> PageResults;
        }


    }
}
