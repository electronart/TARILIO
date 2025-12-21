using com.sun.org.apache.bcel.@internal.generic;
using eSearch.Models.Documents;
using eSearch.Models.Indexing;
using eSearch.ViewModels;
using javax.print.@event;
using Lucene.Net.Search;
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
    public class LuceneSearchResultProvider : IVirtualReadOnlyObservableCollectionProvider<ResultViewModel>, IDataColumnSortable, IVirtualSupportsRangeInfo
    {
        public required LuceneIndex         LuceneIndex;
        public required QueryViewModel      QueryViewModel;

        public required DataColumn?         SortColumn      = null;
        public required bool                SortAscending   = true;
        public required CancellationToken   CancellationToken;

        public SearchResultsCache ResultsCache
        {
            get
            {
                if (_searchResultsCache == null)
                {
                    _searchResultsCache = new SearchResultsCache(LuceneIndex, QueryViewModel, SortColumn, SortAscending, CancellationToken);
                }
                return _searchResultsCache;
            }
            set
            {
                _searchResultsCache = value;
            }
        }

        private SearchResultsCache? _searchResultsCache = null;


        public ResultViewModel this[int index]
        {
            get 
            {
                return ResultsCache.GetResult(index);
            }
        }

        public int Count
        {
            get
            {
                return ResultsCache.GetTotalViewableResults();
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
            while (i < ResultsCache.GetTotalViewableResults())
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
            _searchResultsCache?.Dispose();
            _searchResultsCache = null;
            _numKnownResults = null;
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void GetRangeInformation(out int ResultsStartAt, out int ResultsEndAt, out int totalResults)
        {
            ResultsCache.GetRangeInfoForDisplay(out ResultsStartAt, out ResultsEndAt, out totalResults);
        }

        public class SearchResultsCache : IDisposable
        {

            private LuceneIndex       Index;
            private QueryViewModel    Query;

            private DataColumn?       SortColumn = null;
            private bool              SortAscending = true;
            private CancellationToken CancellationToken;
            private int? _totalResults = null;

            private int LUCENE_MAX_RESULTS = 2147483391;

            private int limitResultsStartAt;
            private int limitResultsEndAt;

            List<CachedPage> CachedPages = new List<CachedPage>();

            public SearchResultsCache(LuceneIndex index, QueryViewModel query, DataColumn? sortColumn, bool sortAscending, CancellationToken cancellationToken)
            {
                this.Index = index;
                this.Query = query;
                this.SortAscending = sortAscending;
                this.SortColumn = sortColumn;
                this.CancellationToken = cancellationToken;

                int max_documents = Math.Min(
                    10000,
                    query.LimitResults ? query.LimitResultsEndAt : LUCENE_MAX_RESULTS); // Additionally, cap max documents if using a results limit.

                limitResultsStartAt = query.LimitResults ? query.LimitResultsStartAt : 0;
                limitResultsEndAt   = query.LimitResults ? query.LimitResultsEndAt : max_documents;
            }

            public void GetRangeInfoForDisplay(out int _limitResultsStartAt, out int _limitResultsEndAt, out int _totalResults)
            {
                _limitResultsStartAt = limitResultsStartAt;
                _limitResultsEndAt = Math.Min(limitResultsEndAt, GetTotalResults());
                _totalResults = GetTotalResults();
            }

            public int GetTotalViewableResults()
            {
                int firstResult = limitResultsStartAt;
                int lastResult = Math.Min(GetTotalResults(), limitResultsEndAt);
                return Math.Max(lastResult - firstResult, 0);
            }

            public int GetTotalResults()
            {
                if (_totalResults == null)
                {
                    var res = Index.GetLuceneResultsBlocking(Query, CancellationToken, null, 0, SortColumn, SortAscending);
                    _totalResults = res.TotalResults;
                    if (CachedPages.Count == 0)
                    {
                        AddToPageCache(new CachedPage { lastScoreDoc = res.LastScoreDoc, StartIndex = 0, Results = res.Results });
                    }
                }
                return (int)_totalResults ;
            }

            public ResultViewModel GetResult(int resultIndex)
            {
                resultIndex += limitResultsStartAt;
                var page = GetResultsPageForIndex(resultIndex);
                return page.GetResultViewModels().First(p => p.ResultIndex == resultIndex);
            }

            private CachedPage GetResultsPageForIndex(int resultIndex)
            {
                if (CachedPages.Count > 0)
                {
                    if (CachedPages[0].StartIndex > resultIndex)
                    {
                        CachedPages.Clear(); // Requested a result that is before the cached range.
                    }

                    foreach (var page in CachedPages)
                    {

                        if (page.StartIndex > resultIndex)
                        {
                            // Requested a result that is before the cached range.
                            CachedPages.Clear();
                        }

                        if (page.StartIndex <= resultIndex
                            && page.StartIndex + (page.Results.Length - 1) >= resultIndex)
                        {
                            return page;
                        }

                    }
                }
                // Result is not cached..
                CachedPage? cachedPage = null;
                if (CachedPages.Count > 0) {
                    cachedPage = CachedPages.Last(p => p.StartIndex < resultIndex);
                }
                ScoreDoc? searchAfter = null;
                while (cachedPage == null || cachedPage.StartIndex + (cachedPage.Results.Length - 1) < resultIndex) {
                    searchAfter = cachedPage?.lastScoreDoc ?? null;
                    int resultIndexAfter = (cachedPage?.StartIndex ?? 0) + (cachedPage?.Results.Length ?? 0);
                    var results = Index.GetLuceneResultsBlocking(Query, CancellationToken, searchAfter, resultIndexAfter, SortColumn, SortAscending);
                    cachedPage = new CachedPage { lastScoreDoc = results.LastScoreDoc, StartIndex = resultIndexAfter, Results = results.Results};
                    AddToPageCache(cachedPage);
                    if (cachedPage.StartIndex <= resultIndex && cachedPage.StartIndex + (cachedPage.Results.Length - 1) >= resultIndex)
                    {
                        return cachedPage;
                    }
                }
                throw new ArgumentOutOfRangeException();
            }

            private void AddToPageCache(CachedPage page)
            {
                CachedPages.Add(page);
            }

            public void Dispose()
            {
                CachedPages.Clear();
            }

            private class CachedPage
            {
                public required int StartIndex;
                public required LuceneResult[] Results;
                public required ScoreDoc? lastScoreDoc;

                private ResultViewModel[]? _cachedResultViewModels = null;

                public ResultViewModel[] GetResultViewModels()
                {
                    if (_cachedResultViewModels == null)
                    {
                        _cachedResultViewModels = new ResultViewModel[Results.Length];
                        Parallel.For(0, Results.Length,
                            new ParallelOptions { MaxDegreeOfParallelism = 8 },
                            index => {
                                _cachedResultViewModels[index] = new ResultViewModel(Results[index]);
                            }
                        );
                    }
                    return _cachedResultViewModels;
                }
            }
        }

    }
}
