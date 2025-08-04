using eSearch.Models.Documents;
using eSearch.ViewModels;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Phonetic;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.QueryParsers.Classic;
using System;
using System.Collections.Generic;
using LCN = Lucene.Net;
using eSearch.Models.Search;
using System.Diagnostics;
using eSearch.Utils;
using Newtonsoft.Json;
using Lucene.Net.Analysis.Phonetic.Language;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.CharFilters;
using System.IO;
using System.Threading.Tasks;
using Lucene.Net.Analysis.Synonym;
using System.Linq;
using eSearch.Interop;
using eSearch.Models.Search.LuceneCustomFieldComparers;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Models.Indexing
{
    public class LuceneIndex : IIndex
    {

        SoundexDictionary? _activeSoundexDictionary;

        public LuceneIndex(string name, string description, string id, string location, int size)
        {
            Name = name;
            Description = description;
            Id = id;
            Location = location;
            Size = size;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Id { get; set; }

        public string Location { get; set; }

        public string GetAbsolutePath()
        {
            if (Path.IsPathRooted(Location)) return Location;
            string libFileLoc = Program.ESEARCH_INDEX_LIB_FILE ?? "";
            string libDir = Path.GetDirectoryName(libFileLoc);
            string absPath = Path.GetFullPath(Location, libDir);
            return absPath;
        }

        public int Size { get; set; }

        private IBits? _liveDocs = null;

        [JsonProperty]
        public List<string> KnownFieldNames
        {
            get
            {
                if (_knownFieldNames == null)
                {
                    _knownFieldNames = new List<string>();
                }
                return _knownFieldNames;
            }
            set
            {
                _knownFieldNames = value;
            }
        }

        private List<string> _knownFieldNames;

        [JsonIgnore]
        public IWordWheel? WordWheel
        {
            get
            {
                try
                {
                    if (_wordWheel == null)
                    {
                        ensureReaderOpen();
                        var luceneWordWheel = new LuceneWordWheel(this);
                        _wordWheel = luceneWordWheel;
                    }
                    return _wordWheel;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error getting WordWheel:");
                    Debug.WriteLine(ex.ToString());
                    return null;
                }
            }
        }

        private IWordWheel _wordWheel;

        #region Lucene Vars

        private bool _open;
        private FSDirectory? _fsDirectory;
        private IndexWriter? _indexWriter;
        private IndexReader? _indexReader;
        private IndexSearcher? _indexSearcher;
        //private Analyzer?   _searchAnalyser;
        private TotalHitCountCollector? _totalHitCountCollector;
        private ScoreDoc[] _scoreDocs;
        //private FileSystemDocument _fsd = new FileSystemDocument(); // Reused.
        private List<ResultViewModel> _results = new List<ResultViewModel>();




        private Analyzer GetDoubleMetaPhoneAnalyser()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new StandardTokenizer(LuceneVersion.LUCENE_48, reader);
                TokenStream stream = new PhoneticFilter(
                    input: tokenizer,
                    encoder: new DoubleMetaphone(),
                    inject: false
                );
                if (TryGetStopWordsCharArraySet(out var set))
                {
                    stream = new StopFilter(LuceneVersion.LUCENE_48, stream, set);
                }
                return new TokenStreamComponents(tokenizer, stream);
            });
            return analyzer;
        }

        private Analyzer GetSoundexAnalyser()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new StandardTokenizer(LuceneVersion.LUCENE_48, reader);
                TokenStream stream = new PhoneticFilter(
                    input: tokenizer,
                    encoder: new Soundex(),
                    inject: false
                );
                if (TryGetStopWordsCharArraySet(out var set))
                {
                    stream = new StopFilter(LuceneVersion.LUCENE_48, stream, set);
                }
                return new TokenStreamComponents(tokenizer, stream);
            });
            return analyzer;
        }



        private bool TryGetStopWordsCharArraySet(out CharArraySet set)
        {
            var indexConfig = Program.IndexLibrary.GetConfiguration(this);
            if (indexConfig.SelectedStopWordFiles != null
                && indexConfig.SelectedStopWordFiles.Count > 0)
            {
                StopWords stopWords = StopWords.FromFiles(indexConfig.SelectedStopWordFiles.ToArray());
                set = new CharArraySet(LuceneVersion.LUCENE_48, stopWords.GetWords(), true);
                return true;
            }
            set = null;
            return false;
        }

        private Analyzer GetSearchAnalyser(QueryViewModel query)
        {
            return GetAnonymousAnalyzer(query);
        }

        private Analyzer GetIndexingAnalyser()
        {
            return GetAnonymousAnalyzer(null);
        }

        private Analyzer GetAnonymousAnalyzer(QueryViewModel? query)
        {
            var indexConfig = Program.IndexLibrary.GetConfiguration(this);
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new StandardTokenizer(LuceneVersion.LUCENE_48, reader);
                TokenStream tokenStream = new StandardFilter(LuceneVersion.LUCENE_48, tokenizer);

                #region Case Insensitive
                if (!indexConfig.IsIndexCaseSensitive) // Default is case insensitive.
                {
                    tokenStream = new LowerCaseFilter(LuceneVersion.LUCENE_48, tokenStream);
                }
                #endregion

                #region Stop Words
                if (TryGetStopWordsCharArraySet(out var set))
                {
                    tokenStream = new StopFilter(LuceneVersion.LUCENE_48, tokenStream, set);
                }
                #endregion

                #region Max Word Length
                int maxChars = 125;
                if (indexConfig.MaximumIndexedWordLength > 0) maxChars = indexConfig.MaximumIndexedWordLength;
                tokenStream = new LengthFilter(LuceneVersion.LUCENE_48, tokenStream, 1, maxChars);
                #endregion


                return new TokenStreamComponents(tokenizer, tokenStream);
            });
            return analyzer;
        }

        #endregion
        public void AddDocument(IDocument document)
        {
            _liveDocs = null;
            if (_indexWriter == null) throw new Exception("Index Writer was not opened");
            var doc = CreateLuceneDocument(document);
            _indexWriter.AddDocument(doc);
        }

        public void AddDocuments(IEnumerable<IDocument> documents)
        {
            if (_indexWriter == null) throw new Exception("Index Writer was not opened");
            var luceneDocs = new List<Document>();
            foreach (var document in documents)
            {
                luceneDocs.Add(CreateLuceneDocument(document));
            }
            _indexWriter.AddDocuments(luceneDocs);

        }

        private Document CreateLuceneDocument(IDocument document)
        {
            var typeName = document.GetType().Name;

            // Note - TextFields get tokenized and won't fully sort
            // Use StringField to allow sort..

            var doc = new Document
            {
                new StringField("Title", document.DisplayName ?? "", Field.Store.YES),
                new TextField("Content", document.Text ?? "", Field.Store.YES),
                new StringField("_IDocumentType", typeName, Field.Store.YES),
                new StringField("_DateCreated", DateUtils.Serialize(document.CreatedDate), Field.Store.YES),
                new StringField("_DateModified", DateUtils.Serialize(document.ModifiedDate), Field.Store.YES),
                new StringField("_DateIndexed", DateUtils.Serialize(DateTime.Now.ToUniversalTime()), Field.Store.YES),
                new StringField("_DateAccessed", DateUtils.Serialize(document.AccessedDate), Field.Store.YES),
                new Int64Field("_FileSize", document.FileSize, Field.Store.YES),
                new StringField("_Parser", document.Parser ?? "Unknown Parser", Field.Store.YES),
                new StringField("_IsVirtualDoc", document.IsVirtualDocument.ToString(), Field.Store.YES),
                new StringField("_DocFSPath", document.FileName ?? string.Empty, Field.Store.YES)
                //new TextField( "_Identifier", document.Identifier, Field.Store.YES)
            };

            if (document.MetaData != null)
            {
                foreach (var metadata in document.MetaData.Where(m => m != null))
                {
                    if (metadata != null && metadata.Key != null && metadata.Value != null)
                    {
                        Field field;
                        if (metadata.Value?.Length <= 256)
                        {
                            field = new StringField(metadata.Key, metadata.Value, Field.Store.YES);
                        }
                        else
                        {
                            field = new TextField(metadata.Key, metadata.Value, Field.Store.YES);
                        }

                        doc.Fields.Add(field);
                        if (!IsKnownField(metadata.Key)) KnownFieldNames.Add(metadata.Key);
                    }
                }
            }
            if (document.HtmlRender != null && document.HtmlRender.Length > 0)
            {
                doc.Fields.Add(new TextField("_HtmlRender", document.HtmlRender, Field.Store.YES));
            }
            return doc;
        }

        // I am caching the lower case versions of field names as they are requested
        // This is for performance reasons, without this IsKnownField method is a hot
        // path within the application during indexing.
        private HashSet<string> _knownFieldNamesLowerCaseCache = new HashSet<string>();

        private bool IsKnownField(string name)
        {
            string nameLowerCase = name.ToLower();
            #region First try against the cache.
            if (_knownFieldNamesLowerCaseCache.Contains(nameLowerCase)) return true;
            #endregion
            foreach (string knownFieldName in KnownFieldNames)
            {
                if (knownFieldName.ToLower() == nameLowerCase)
                {
                    _knownFieldNamesLowerCaseCache.Add(knownFieldName.ToLower());
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return Name;
        }

        public void CloseWrite()
        {
            _indexWriter?.Flush(triggerMerge: false, applyAllDeletes: false);
            _indexWriter?.Commit();
            _indexWriter?.Dispose();
            // TODO Figure out how to close this properly.
            _indexWriter = null;
            _fsDirectory = null;
            _open = false;
            _wordWheel = null; // Invalidates the wheel.
        }

        public void EnsureClosed()
        {
            _indexSearcher = null;
            _indexReader?.Dispose();
            _indexWriter?.Dispose();
            _indexWriter = null;
            _indexReader = null;
            _fsDirectory?.Dispose();
            _fsDirectory = null;
            _open = false;
            _wordWheel = null; // Invalidate the word wheel.
        }

        /// <summary>
        ///  May return null if not currently open.
        /// </summary>
        /// <returns></returns>
        public IndexReader? GetCurrentReader()
        {
            return _indexReader;
        }

        public bool OpenWrite(bool create)
        {
            if (_open) return false;
            #region Debugging a crash...  https://stackoverflow.com/questions/65641448/can-not-create-instance-of-lucene-net-standardanalyzer
            //List<string> list = new List<string>();
            //list.Add("hello world");
            #endregion
            const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
            _fsDirectory = FSDirectory.Open(GetAbsolutePath());


            var analyser = GetIndexingAnalyser();
            var indexConfig = Program.IndexLibrary.GetConfiguration(this);

            var indexWriterConfig = new IndexWriterConfig(AppLuceneVersion, analyser);
            indexWriterConfig.RAMBufferSizeMB = 256;
            if (create)
            {
                indexWriterConfig.OpenMode = OpenMode.CREATE;
            }
            else
            {
                indexWriterConfig.OpenMode = OpenMode.APPEND;
            }



            _indexWriter = new IndexWriter(_fsDirectory, indexWriterConfig);
            return true;
        }

        /// <summary>
        /// Ensure that the index is open for reading otherwise this method will fail with -1.
        /// </summary>
        /// <returns></returns>
        public int GetTotalDocuments()
        {
            return _indexReader?.NumDocs ?? -1;
        }

        public IDocument? GetDocument(int n)
        {
            var liveDocs = MultiFields.GetLiveDocs(_indexReader);
            if (liveDocs != null)
            {
                for (int i = n; i < _indexReader.MaxDoc; i++)
                {
                    if (liveDocs.Get(i))
                    {

                        return ToIDocument(_indexReader.Document(n));
                    }
                }
            }
            return null;
        }

        public void RemoveDocument(IDocument document)
        {
            _indexWriter.DeleteDocuments(
                new Term("_DocFSPath", document.FileName)
            );
        }

        LCN.Search.Query _lcnQuery;

        public IVirtualReadOnlyObservableCollectionProvider<ResultViewModel> PerformSearch(QueryViewModel query)
        {
            DataColumn? sortColumn      = GetAvailableColumns().FirstOrDefault(c => c.Header == query.SortBy);
            bool sortAscending = query.SortAscending;

            return new LuceneSearchResultProvider { 
                LuceneIndex = this, 
                QueryViewModel = query,
                SortColumn = sortColumn,
                SortAscending = sortAscending,
            };
        }

        int LUCENE_MAX_RESULTS = 2147483391;

        public LuceneResultsNfo GetLuceneResultsBlocking(QueryViewModel query, int page = 0, DataColumn? sortColumn = null, bool sortAscending = true)
        {
            List<ResultViewModel> results = new List<ResultViewModel>();
            ensureReaderOpen();
            int max_documents = Math.Min(
                1024 * 1024, GetTotalDocuments());
            max_documents  = Math.Min(
                max_documents,
                query.LimitResults? query.LimitResultsEndAt : LUCENE_MAX_RESULTS); // Additionally, cap max documents if using a results limit.
            bool limitResults         = query.LimitResults;
            int  limitResultsStartAt  = query.LimitResults ? query.LimitResultsStartAt : 0;
            int  limitResultsEndAt    = query.LimitResults ? query.LimitResultsEndAt   : LUCENE_MAX_RESULTS;

            

            int totalResults = 0;

            try
            {
                _lcnQuery = GetLCNQuery(query);

                ITopDocsCollector collector;
                if (sortColumn != null && sortColumn.BindTo != nameof(ResultViewModel.Score))
                {
                    IFieldValueProcessor? fieldValueProcessor = null;

                    switch(sortColumn.BindTo)
                    {
                        case nameof(ResultViewModel.FileName):
                            fieldValueProcessor = new FieldValueProcessorFileName();
                            break;
                        case nameof(ResultViewModel.FileExtension):
                            fieldValueProcessor = new FieldValueProcessorFileType();
                            break;
                    }

                    var internalSortFieldName = sortColumn.GetInternalFieldName();

                    Sort? sort;

                    // TODO It would be nice to have a better way of handling non text columns...
                    if (sortColumn.Header != S.Get("Size"))
                    {
                        var comparer = new NumericStringComparerSource(sortAscending, fieldValueProcessor);
                        var sortField = new SortField(internalSortFieldName, comparer);
                        sort = new Sort(sortField);
                    } else
                    {
                        var sortField = new SortField(internalSortFieldName, SortFieldType.INT64, sortAscending);
                        sort = new Sort(sortField);
                    }


                    
                    collector = TopFieldCollector.Create(
                        sort, 1024 * 1024, 
                        fillFields: true, 
                        trackDocScores: false, 
                        trackMaxScore: false, 
                        docsScoredInOrder: false
                    );
                } else
                {
                    collector = TopScoreDocCollector.Create(max_documents, true);
                    
                    
                }
                _indexSearcher?.Search(_lcnQuery, collector);

                totalResults = collector.TotalHits;
                if (totalResults > 0)
                {
                    var topDocs = collector.GetTopDocs(limitResultsStartAt + (query.ResultsPerPage * page), query.ResultsPerPage);
                    _scoreDocs = topDocs.ScoreDocs;


                    if (_scoreDocs != null)
                    {
                        int startIndex = (page * query.ResultsPerPage);
                        int endIndex = Math.Min(startIndex + query.ResultsPerPage, limitResultsEndAt);
                        int r = startIndex;
                        int d = 0;
                        while (r < endIndex && d < _scoreDocs.Length)
                        {
                            ScoreDoc scoreDoc = _scoreDocs[d];
                            Document document = _indexSearcher.Doc(scoreDoc.Doc);



                            //foreach(var field in document.Fields)
                            //{
                            // TODO
                            //}
                            //LCN.Analysis.Analyzer a = _searchAnalyser;

                            int resultIndex = (page * query.ResultsPerPage) + d;

                            LuceneResult res = new LuceneResult
                                (scoreDoc.Doc,
                                _indexSearcher,
                                scoreDoc.Score,
                                ToIDocument(document),
                                _lcnQuery,
                                GetSearchAnalyser(query),
                                this,
                                resultIndex);
                            results.Add(new ResultViewModel(res));
                            ++r;
                            ++d;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("!!!!!!!!!!!!!!!!!!!!! _topDocs is null....");
                    }
                }
            }
            catch (NullReferenceException nre)
            {
                // Hushing the compiler
                Debug.WriteLine("Yay?");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception performing search...");
                Debug.WriteLine(ex.ToString());
            }
            return new LuceneResultsNfo
            {
                Results = results,
                TotalResults = Math.Min(totalResults, max_documents)
            };
        }

        public class LuceneResultsNfo
        {
            public required List<ResultViewModel>   Results;
            public required int                     TotalResults;
        }

        public void OpenRead()
        {
            ensureReaderOpen();
        }

        private SynonymMap GetSoundexSynonymMapForQuery(string query)
        {
            var punctuation = query.Where(char.IsPunctuation).Distinct().ToArray();
            var words = query.Split().Select(x => x.Trim(punctuation));

            var builder = new SynonymMap.Builder(true);
            foreach (var word in words)
            {
                var synonyms = _activeSoundexDictionary?.GetSoundexMatchesForWord(word);
                if (synonyms != null)
                {
                    foreach (var synonym in synonyms)
                    {
                        if (synonym != word) builder.Add(new CharsRef(word), new CharsRef(synonym), true);
                    }
                }
            }
            return builder.Build();
        }

        private void ensureReaderOpen()
        {
            if (_fsDirectory == null)
            {
                _fsDirectory = FSDirectory.Open(GetAbsolutePath());
            }
            if (_indexReader == null)
            {
                try
                {
                    _indexReader = IndexReader.Open(_fsDirectory);
                }
                catch (DirectoryNotFoundException dnfe)
                {
                    // Catch the case that the directory was since deleted from the disk.
                    OpenWrite(true);
                    CloseWrite();
                    _fsDirectory = FSDirectory.Open(GetAbsolutePath());
                    _indexReader = IndexReader.Open(_fsDirectory);
                }
                catch (IndexNotFoundException infe)
                {
                    OpenWrite(true);
                    CloseWrite();
                    _fsDirectory = FSDirectory.Open(GetAbsolutePath());
                    _indexReader = IndexReader.Open(_fsDirectory);
                }
            }
            if (_indexSearcher == null)
            {
                _indexSearcher = new IndexSearcher(_indexReader);
                _indexSearcher.Similarity = new BM25Similarity();
            }
        }

        private LCN.Search.Query GetLCNQuery(QueryViewModel qvm)
        {
            if (qvm.JustListAllDocuments())
            {
                LCN.Search.Query query = new MatchAllDocsQuery();
                return query;
            }
            else
            {
                List<string> fieldNames = new List<string>();
                if (qvm.SearchWithinDocumentMetadata)
                {
                    var fields = MultiFields.GetFields(_indexReader);
                    if (fields != null) // Can be null.
                    {
                        var enumerator = fields.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            string fieldName = enumerator.Current;

                            if (!fieldName.StartsWith("_")) fieldNames.Add(enumerator.Current);
                        }
                    }
                }
                else
                {
                    fieldNames.Add("Content");
                }
                var queryParser = new MultiFieldQueryParser(LuceneVersion.LUCENE_48, fieldNames.ToArray(), GetSearchAnalyser(qvm));
                Operator op = Operator.AND;
                switch (qvm.SelectedSearchType)
                {
                    case QueryViewModel.SearchType.AllWords:
                        op = Operator.AND; break;
                    case QueryViewModel.SearchType.AnyWords:
                        op = Operator.OR; break;
                    case QueryViewModel.SearchType.Boolean:
                        op = Operator.AND; break;
                }
                queryParser.DefaultOperator = op;

                // Use text from query textbox.
                string strQuery = qvm.GetProcessedQuery(this);
                return GetLCNQuery(qvm, queryParser, strQuery);

            }
        }

        private LCN.Search.Query GetLCNQuery(QueryViewModel qvm, QueryParser queryParser, string finalQuery)
        {
            LCN.Search.Query query = queryParser.Parse(finalQuery);
            return qvm.UseSoundex ? SoundexSynonymExpandQuery(query) : query; // If soundex is enabled, soundex expand the query before returning it.
        }

        private LCN.Search.Query SoundexSynonymExpandQuery(LCN.Search.Query originalQuery)
        {
            BooleanQuery expandedQuery = new BooleanQuery();

            if (originalQuery is BooleanQuery booleanQuery)
            {
                return SoundexSynonymExpandBooleanQuery(booleanQuery);
            }
            else if (originalQuery is TermQuery termQuery)
            {
                return SoundexSynonymExpandTermQuery(termQuery);
            }
            else if (originalQuery is PhraseQuery phraseQuery)
            {
                return SoundexSynonymExpandPhraseQuery(phraseQuery);
            }
            else
            {
                // Some other type of query such as WildcardQuery, PrefixQuery, FuzzyQuery
                // This could happen if the user enters special syntax such as `car*` `car~`
                return originalQuery;
            }
        }

        private LCN.Search.Query SoundexSynonymExpandBooleanQuery(BooleanQuery originalQuery)
        {
            BooleanQuery expandedQuery = new BooleanQuery();

            foreach (var clause in originalQuery.Clauses)
            {
                expandedQuery.Add(new BooleanClause(SoundexSynonymExpandQuery(clause.Query), clause.Occur));
            }

            return expandedQuery;
        }

        private LCN.Search.Query SoundexSynonymExpandTermQuery(TermQuery termQuery)
        {
            string termText = termQuery.Term.Text;
            if (_activeSoundexDictionary != null)
            {
                var soundex_synonyms = _activeSoundexDictionary.GetSoundexMatchesForWord(termText);
                if (soundex_synonyms?.Count > 1)
                {
                    BooleanQuery termExpansion = new BooleanQuery();
                    termExpansion.Add(new TermQuery(termQuery.Term), Occur.SHOULD);
                    foreach (var soundex_synonym in soundex_synonyms)
                    {
                        termExpansion.Add(
                            new TermQuery(new Term(termQuery.Term.Field, soundex_synonym)),
                            Occur.SHOULD);
                    }
                    return termExpansion;
                }
            }
            return termQuery; // Fall back in case of no expansions.
        }

        private LCN.Search.Query SoundexSynonymExpandPhraseQuery(PhraseQuery phraseQuery)
        {
            MultiPhraseQuery multiPhraseQuery = new MultiPhraseQuery();
            bool hasSynonyms = false;
            if (_activeSoundexDictionary != null)
            {
                foreach (Term term in phraseQuery.GetTerms())
                {
                    List<Term> termAlternatives = new List<Term> { term }; // Initialize the list with the original term.
                    var soundex_synonyms = _activeSoundexDictionary.GetSoundexMatchesForWord(term.Text);
                    if (soundex_synonyms != null)
                    {
                        if (soundex_synonyms?.Count > 1) hasSynonyms = true;
                        foreach (var soundex_synonym in soundex_synonyms)
                        {
                            termAlternatives.Add(new Term(term.Field, soundex_synonym));
                        }
                    }
                    multiPhraseQuery.Add(termAlternatives.ToArray());
                }
            }

            return hasSynonyms ? multiPhraseQuery : phraseQuery; // Only use multiPhrase query if we had synonyms, otherwise use phraseQuery to avoid performance impact.
        }

        private IDocument ToIDocument(Document document)
        {
            try
            {
                if (document.GetField("_IsVirtualDoc")?.GetStringValue() != "True")
                {
                    var pathField = document.GetField("_DocFSPath");
                    string path = pathField.GetStringValue();
                    FileSystemDocument fsd = new FileSystemDocument();
                    fsd.SetDocument(path);
                    fsd.IndexedDate = DateUtils.Deserialize(document.GetField("_DateIndexed")?.GetStringValue() ?? null);
                    return fsd;
                }
                else
                {
                    InMemoryDocument memoryDoc = new InMemoryDocument
                    {
                        DisplayName = document.GetField("Title")?.GetStringValue() ?? "Untitled",
                        Text = document.GetField("Content")?.GetStringValue() ?? string.Empty,
                        CreatedDate = DateUtils.Deserialize(document.GetField("_DateCreated").GetStringValue()),
                        ModifiedDate = DateUtils.Deserialize(document.GetField("_DateModified").GetStringValue()),
                        AccessedDate = DateUtils.Deserialize(document.GetField("_DateAccessed").GetStringValue()),
                        FileSize = document.GetField("_FileSize")?.GetInt64Value() ?? 0,
                        Parser = document.GetField("_Parser")?.GetStringValue() ?? "Unknown Parser",
                    };
                    List<IMetaData> metaData = new List<IMetaData>();
                    foreach (var field in document.Fields)
                    {
                        bool exclude = false;
#if !DEBUG
                    if (field.Name.StartsWith("_")) {
                        exclude = true;
                    }
#endif
                        if (!exclude)
                        {
                            string fieldValue = field.GetStringValue() ?? "";
                            if (fieldValue != "")
                            {
                                metaData.Add(new Documents.Parse.Metadata { Key = field.Name, Value = fieldValue });
                            }
                        }
                    }
                    memoryDoc.MetaData = metaData;
                    return memoryDoc;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                InMemoryDocument memoryDoc = new InMemoryDocument
                {
                    DisplayName = "Corrupt Doc",
                    Text = "Error reading this document from index " + ex.ToString()
                };
                return memoryDoc;
            }
        }

        public DataColumn[] GetAvailableColumns()
        {

            var columns = DataColumn.GetStandardColumns();


            for (int i = 0; i < KnownFieldNames.Count; i++)
            {
                string fieldName = KnownFieldNames[i];
                columns.Add(new DataColumn(-1, fieldName, false, 250, "[" + i + "]"));
            }
            foreach (var knownFieldName in KnownFieldNames)
            {
                columns.Add(new DataColumn(-1, knownFieldName, true, 250, "[" + Utils.GetChecksum(knownFieldName.ToLower()) + "]"));
            }
            return columns.ToArray();
        }

        public void SetActiveSoundexDictionary(SoundexDictionary soundexDictionary)
        {
            _activeSoundexDictionary = soundexDictionary;
        }
    }
}
