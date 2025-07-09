using com.healthmarketscience.jackcess.impl;
using DesktopSearch2.Models.Search;
using eSearch.Models.Indexing;
using eSearch.Models.Search;
using eSearch.Models.Search.Stemming;
using eSearch.Models.Search.Synonyms;
using jdk.nashorn.@internal.ir;
using Newtonsoft.Json;
using ReactiveUI;
using sun.misc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{

    [JsonObject(MemberSerialization.OptIn)]
    public class QueryViewModel : ViewModelBase
    {
        private int _results_per_page = 300;

        [JsonProperty]
        public int ResultsPerPage {
            get { return _results_per_page; }
            set
            {
                this.RaiseAndSetIfChanged(ref _results_per_page, value);
            }
        }

        [JsonProperty]
        public bool LimitResults
        {
            get
            {
                if (!Program.ProgramConfig.IsProgramRegistered())
                {
                    return true;
                }

                return _limitResults;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _limitResults, value);
            }
        }

        private bool _limitResults = false;

        public bool SearchWithinDocumentMetadata
        {
            get
            {
                return _searchWithinDocumentMetadata;
            }
            set
            {
                
                this.RaiseAndSetIfChanged(ref _searchWithinDocumentMetadata, value);
            }
        }

        private bool _searchWithinDocumentMetadata = false;

        [JsonProperty]
        public bool UseAISearch
        {
            get
            {
                if (Program.WasLaunchedWithAIDisabledArgument) return false;
                return _useAISearch;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _useAISearch, value);
            }
        }

        private bool _useAISearch = false;


        [JsonProperty]
        public int LimitResultsStartAt
        {
            get
            {
                return 0;
                /*
                if (!Program.ProgramConfig.IsProgramRegistered())
                {
                    return 1;
                }
                return _limitResultsStartAt;
                */
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _limitResultsStartAt, value);
            }
        }

        private int _limitResultsStartAt = 1;


        /// <summary>
        /// When Empty String, use default engine sort.
        /// Otherwise, Use Column Name as displayed on user interface.
        /// </summary>
        [JsonProperty]
        public string SortBy
        {
            get
            {
                return _sortBy;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _sortBy, value);
            }
        }

        private string _sortBy = string.Empty;

        [JsonProperty]
        public bool SortAscending
        {
            get
            {
                return _sortAscending;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _sortAscending, value);
            }
        }

        private bool _sortAscending = true;


        [JsonProperty]
        public int LimitResultsEndAt
        {
            get {
                if (!Program.ProgramConfig.IsProgramRegistered())
                {
                    return 10;
                }
                return _limitResultsEndAt;
            
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _limitResultsEndAt, value);
            }
        }

        private int _limitResultsEndAt = 100;

        [JsonProperty]
        public string QueryListFilePath
        {
            get
            {
                return _queryListFilePath;
            }
            set
            {
                if (value == null)
                {
                    _queryListFromFile  = null;
                }
                else
                {
                    _queryListFromFile = new QueryListFromFile(value);
                    SelectedSearchType = SearchType.AnyWords;
                }
                this.RaiseAndSetIfChanged(ref _queryListFilePath, value);
                this.RaisePropertyChanged(nameof(Query));
                this.RaisePropertyChanged(nameof(QueryTextBoxEnabled));
            }
        }

        private string              _queryListFilePath = null;
        private QueryListFromFile   _queryListFromFile = null;
        private string              _queryUser = "";

        public QueryListFromFile GetQueryListFromFile()
        {
            if (QueryListFilePath == null) return null;
            if (_queryListFromFile == null)
            {
                _queryListFromFile = new QueryListFromFile(QueryListFilePath);
            }
            return _queryListFromFile;
        }

        /// <summary>
        /// Raw Query Text as set in text box.
        /// Note, this shouldn't be directly used for searches. Use GetProcessedQuery.
        /// </summary>
        [JsonProperty]
        public string Query
        {
            get {
                if (QueryListFilePath == null)
                {
                    return _queryUser; // Regular query.
                } else
                {
                    var queryList = GetQueryListFromFile();
                    var temp = queryList.GetQueries();
                    string[] strQueries = new string[temp.Count];

                    for (int i = 0; i < temp.Count; i++)
                    {
                        strQueries[i] = "(" + temp[i].Trim() + ")";
                    }
                    switch(SelectedSearchType)
                    {
                        case SearchType.AnyWords:
                            return string.Join(" OR ", strQueries);
                        default:
                            return string.Join(" AND ", strQueries);
                    }
                }
            }
            set
            {
                string query = ProcessQueryInput(value);
                this.RaiseAndSetIfChanged(ref _queryUser, query);
            }
        }

        [JsonProperty]
        public List<QueryFilter> QueryFilters = new List<QueryFilter>();

        public string ProcessQueryInput(string query)
        {
            if (UseAISearch) return query; // In AI Mode, do not change the query. Pass it along raw.
            string[] operators = { "and", "or", "not" };
            List<string> words = new List<string>();
            int i = 0;
            int len = query.Length;

            var strBuff = new StringBuilder();

            bool isPhrase = false;


            string word;
            while (i < len)
            {
                char c = query[i];
                if (c == '"') isPhrase = !isPhrase;
                if (c == ' ')
                {
                    word = strBuff.ToString();
                    strBuff.Clear();
                    if (!isPhrase)
                    {
                        if (operators.Contains(word))
                        {
                            // Uppercase AND, OR, NOT
                            word = word.ToUpperInvariant();
                        }
                    }
                    words.Add(word);
                } else
                {
                    strBuff.Append(c);
                }
                
                ++i;
            }
            if (strBuff.Length > 0)
            {
                words.Add(strBuff.ToString());
            }
            string queryProcessed = string.Join(" ", words);
            if (query.EndsWith(" "))
            {
                queryProcessed += " ";
            }
            return queryProcessed;
        }

        public bool QueryTextBoxEnabled
        {
            get
            {
                return QueryListFilePath == null;
            }
        }

        private QueryExpander? _queryExpander;

        public string GetProcessedQuery(IIndex index)
        {
            if (this.QueryListFilePath == null)
            {
                // Use text from query textbox.
                string strQuery = ExpandRawQueryAccordingToSettings(index, Query.Trim());
                return strQuery;
               // return GetLCNQuery(qvm, queryParser, strQuery);
            }
            else
            {
                // Use Word List file.
                var queryList = GetQueryListFromFile();
                var temp = queryList.GetQueries();
                string[] strQueries = new string[temp.Count];
                for (int i = 0; i < temp.Count; i++)
                {
                    strQueries[i] = "(" + ExpandRawQueryAccordingToSettings(index, temp[i]).Trim() + ")";
                }

                string finalQuery;
                switch (SelectedSearchType)
                {
                    case QueryViewModel.SearchType.AnyWords:
                        finalQuery = string.Join(" OR ", strQueries);
                        break;
                    default:
                        finalQuery = string.Join(" AND ", strQueries);
                        break;

                }
                return finalQuery;
            }
        }

        private string ExpandRawQueryAccordingToSettings(IIndex index, string strQuery)
        {
            if (UseSynonyms || UseStemming)
            {
                if (_queryExpander == null)
                {
                    _queryExpander = new QueryExpander(index);
                }
                _queryExpander.SetQuery(strQuery, this);
                strQuery = _queryExpander.Expand();
            }
           return strQuery;
        }

        /// <summary>
        /// The connector. Eg All Words, Any Words, Boolean
        /// </summary>
        [JsonProperty]
        public SearchType SelectedSearchType
        {
            get { return _searchType; }
            set
            {
                this.RaiseAndSetIfChanged(ref _searchType, value);
                this.RaisePropertyChanged(nameof(Query));
            }
        }

        [JsonProperty]
        public bool UseStemming
        {
            get
            {
                return _useStemming;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _useStemming, value);
                this.RaisePropertyChanged(nameof(Query));
            }
        }

        private bool _useStemming = false;

        /// <summary>
        /// Note that setting stemming rules here does not persist. To persist save to ProgramConfig.StemmingConfig
        /// </summary>
        public StemmingRules? StemmingRules
        {
            get
            {
                if (_stemmingRules == null)
                {
                    _stemmingRules = Program.ProgramConfig.StemmingConfig.LoadActiveStemmingRules();
                }
                return _stemmingRules;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _stemmingRules, value);
            }
        }

        private StemmingRules _stemmingRules = null;

        [JsonProperty]
        public bool UseSynonyms
        {
            get { return _useSynonyms; }
            set
            {
                this.RaiseAndSetIfChanged(ref _useSynonyms, value);
                this.RaisePropertyChanged(nameof(Query));
            }
        }

        [JsonProperty]
        public bool UseSoundex
        {
            get { return _useSoundex; }
            set
            {
                this.RaiseAndSetIfChanged(ref _useSoundex, value);
                this.RaisePropertyChanged(nameof(Query));
            }
        }

        private bool _useSoundex = false;

        public IThesaurus[] Thesauruses
        {
            get
            {
                if (UseSynonyms)
                {
                    
                    if (_thesauruses == null)
                    {
                        _thesauruses = Program.ProgramConfig.SynonymsConfig.GetActiveThesauri();
                    }
                    return _thesauruses;
                } else
                {
                    return new IThesaurus[0];
                }
            }
        }

        private IThesaurus[] _thesauruses = null;

        public void InvalidateCachedThesauri()
        {
            _thesauruses = null;
            this.RaisePropertyChanged(nameof(Thesauruses));
        }


        private bool _useSynonyms = false;

        /// <summary>
        /// When this returns true, the query passed to the Search Engine should be one that simply retrieves all documents in the index.
        /// </summary>
        /// <returns></returns>
        public bool JustListAllDocuments()
        {
            if (QueryListFilePath != null) return false;
            if (Query == null || Query.Trim().Length == 0)
            {
                return Program.ProgramConfig.ListContentsOnEmptyQuery;
            }
            return false;
        }

        SearchType _searchType = QueryViewModel.SearchType.AllWords;

        public enum SearchType
        {
            AllWords = 0,
            AnyWords = 1,
            Boolean = 2
        }
    }
}
