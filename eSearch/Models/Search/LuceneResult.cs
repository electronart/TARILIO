
using DesktopSearch2.Models.Configuration;
using eSearch.Interop;
using eSearch.Models.Documents;
using eSearch.Models.Documents.Parse;
using eSearch.Models.Indexing;
using eSearch.ViewModels;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Search.VectorHighlight;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using LCN = Lucene.Net;

namespace eSearch.Models.Search
{
    public class LuceneResult : IResult
    {
        private int                     _resultIndex;
        private LuceneIndex             _index;
        private int                     _doc;
        private IndexSearcher           _searcher;
        private float                   _score;
        private LCN.Search.Query        _query;
        private LCN.Analysis.Analyzer   _analyzer;

        private StringBuilder HtmlBuilder {
            get
            {
                if (_htmlBuilder == null)
                {
                    _htmlBuilder = new StringBuilder();
                }
                return _htmlBuilder;
            }
        }

        private StringBuilder? _htmlBuilder = null;

        /// <summary>
        /// Build a reference to a Lucene Search Result.
        /// </summary>
        /// <param name="Doc">ScoreDoc ID</param>
        /// <param name="searcher">The searcher that generated these ScoreDocs</param>
        public LuceneResult(
            int doc, 
            IndexSearcher searcher, 
            float score, 
            LCN.Search.Query query,
            LCN.Analysis.Analyzer analyzer,
            LuceneIndex index,
            int resultIndex
            ) {

            _doc = doc;
            _searcher = searcher;
            _score = score;
            _query = query;
            _analyzer = analyzer;
            _index = index;
            _resultIndex = resultIndex;
        }

        public void Dispose()
        {
            _searcher = null;
            _query = null;
            _analyzer = null;
            _index = null;
        }


        // Avoid repeatedly calling searcher for the document has CPU penalty.
        private LCN.Documents.Document LuceneDocument
        {
            get
            {
                if (_luceneDocument == null)
                {
                    _luceneDocument = _searcher.Doc(_doc);
                }
                return _luceneDocument;
            }
        }

        private LCN.Documents.Document _luceneDocument = null;

        public IIndex Index
        {
            get
            {
                return _index;
            }
        }

        public int Score
        {
            get
            {
                return (int)Math.Floor(_score * 100);
                //return _score;
            }
        }

        public string Parser
        {
            get
            {
                var parserField = LuceneDocument.GetField("_Parser");
                if (parserField != null)
                {
                    string text = parserField.GetStringValue();
                    if (text != null) return text;
                }
                return "Unknown Parser";
            }
        }

        public string Context
        {
            get
            {
                string[] hits = GetHitsInContext(1, "<<HIT>>", "<</HIT>>");
                if (hits.Length > 0)
                {
                    return hits[0];
                } else
                {
                    return "";
                }
            }
        }

        public IDocument Document
        {
            get
            {
                return _index.ToIDocument(LuceneDocument);
            }
        }

        public string Title
        {
            get
            {
                var titleField = LuceneDocument.GetField("Title");
                if (titleField != null)
                {
                    string text = titleField.GetStringValue();
                    if (text != null) return text;
                }
                return Document?.DisplayName ?? string.Empty;
            }
        }

        public string Content
        {
            get
            {
                var contentField = LuceneDocument.GetField("Content");
                if (contentField != null)
                {
                    string content = contentField.GetStringValue();
                    if (content != null) return content;
                }
                return ExtractHtmlRender();
            }
        }

        public string[] HiddenFields =
        {
            "Content"
        };

        public IEnumerable<Metadata> Metadata
        {
            get
            {
                List<Metadata> metaData = new List<Metadata>();
                foreach(var field in LuceneDocument.Fields)
                {
                    string name  = field.Name;
                    if (HiddenFields.Contains(name)) { continue; } // Skip hidden fields.
                    string value = field.GetStringValue();
                    metaData.Add(new Metadata { Key = name, Value = value });
                }
                return metaData;
            }
        }

        public string ExtractHtmlRender(bool escape_html = false, bool hit_highlight = false)
        {
            
            HtmlBuilder.Clear();
            var formatter = new SimpleHTMLFormatter("<span class='doc-hit highlight'>", "</span>"); // The 'highlight' tag is not surplus to requirement. without it prism removes these spans on code blocks.

            Lucene.Net.Search.Highlight.Highlighter highlighter = new Highlighter(formatter, new QueryScorer(_query));
            // Use NullFragmenter to turn the whole document field into one snippet.
            // https://stackoverflow.com/questions/31194125/after-lucene-search-get-character-offsets-of-all-matched-words-in-document-no
            highlighter.TextFragmenter = new NullFragmenter();
            
            string docFSPath = LuceneDocument.GetField("_DocFSPath").GetStringValue();
            string fHTml = LuceneDocument.Get("_HtmlRender");
            string fText = LuceneDocument.Get("Content");

                if (escape_html)
            {
                fText = HttpUtility.HtmlEncode(fText);
            }

                if (fHTml != null && fHTml.Length > 0)
            {
                fText = fHTml;
            }

            if (hit_highlight)
            {
                HtmlBuilder.Append(HighlightTextAccordingToResultQuery(fText));
            } else
            {
                HtmlBuilder.Append(fText);
            }
            
                //LCN.Analysis.TokenStream tokenStream = TokenSources.GetAnyTokenStream(_searcher.IndexReader, _doc, field.Name, _analyzer);
                //var fragments = highlighter.GetBestTextFragments(tokenStream, text, mergeContiguousFragments: false, maxNumFragments: 10);
                //higher
                //foreach(var fragment in fragments)
                //{
                //    htmlBuilder.Append(fragment);
                //    htmlFormatter.HighlightTerm(text, tokenStream);
                //}

            return HtmlBuilder.ToString();
        }

        

        private Highlighter HtmlHighlighter
        {
            get
            {
                if (_highlighter == null)
                {
                    var formatter = new SimpleHTMLFormatter("<span class='doc-hit highlight'>", "</span>"); // The 'highlight' tag is not surplus to requirement. without it prism removes these spans on code blocks.

                    Lucene.Net.Search.Highlight.Highlighter highlighter = new Highlighter(formatter, new QueryScorer(_query));
                    // Use NullFragmenter to turn the whole document field into one snippet.
                    // https://stackoverflow.com/questions/31194125/after-lucene-search-get-character-offsets-of-all-matched-words-in-document-no
                    highlighter.TextFragmenter = new NullFragmenter();
                    _highlighter = highlighter;
                }
                return _highlighter;
            }
        }

        private Highlighter _highlighter = null;

        private string INTERNAL_HIGHLIGHT_START = "!!H_START!!";
        private string INTERNAL_HIGHLIGHT_END  = "!!H_END!!";

        private Highlighter InternalHighlighter
        {
            get
            {
                if (_internalHighlighter == null)
                {
                    var formatter = new SimpleHTMLFormatter(INTERNAL_HIGHLIGHT_START, INTERNAL_HIGHLIGHT_END); // The 'highlight' tag is not surplus to requirement. without it prism removes these spans on code blocks.

                    Lucene.Net.Search.Highlight.Highlighter highlighter = new Highlighter(formatter, new QueryScorer(_query));
                    // Use NullFragmenter to turn the whole document field into one snippet.
                    // https://stackoverflow.com/questions/31194125/after-lucene-search-get-character-offsets-of-all-matched-words-in-document-no
                    highlighter.TextFragmenter = new NullFragmenter();
                    _internalHighlighter = highlighter;
                }
                return _internalHighlighter;
            }
        }

        public int ResultIndex => _resultIndex;

        private Highlighter _internalHighlighter = null;

        public string HighlightTextAccordingToResultQuery(string textToHighlight)
        {
            HtmlHighlighter.MaxDocCharsToAnalyze = textToHighlight.Length;
            string text = HtmlHighlighter.GetBestFragment(_analyzer, "", textToHighlight);
            if (text != null)
            {
                return text;
            }
            return textToHighlight;
        }

        public List<(int Start, int End)> GetHighlightRanges(string textToHighlight)
        {
            // This function is particularly performance sensitive as it gets called
            // repeatedly whilst the user is waiting for the document to be highlighted

            // For short texts it's fast enough to use the internal highlighter directly
            // For long texts which this function sometimes receives, I create a temporary index.
            //if (textToHighlight.Length < 10000)
            //{
                // Short text.
                InternalHighlighter.MaxDocCharsToAnalyze = textToHighlight.Length;
                string text = InternalHighlighter.GetBestFragment(_analyzer, "", textToHighlight);
                return GetHighlightRangesFromInternalHighlightedTextRegex(text);
            //} else
            //{
            //    // Long text.
            //    Debug.WriteLine("Beeg text");
            //    return new List<(int Start, int End)>();
            //}
        }

        // Flawed - I was trying to use FastVectorHighlighter to speed things up but there were issues with this approach to do with how it fragments text..
        //private List<(int Start, int End)> GetHighlightRangesWithFastVectorHighlighter(string textToHighlight)
        //{
        //    // Create in-memory index
        //    using var directory = new RAMDirectory();
        //    var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        //    var indexWriterConfig = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);
        //    using var writer = new IndexWriter(directory, indexWriterConfig);

        //    // Index the text with term vectors
        //    var doc = new Lucene.Net.Documents.Document();
        //    var field = new Lucene.Net.Documents.TextField("content", textToHighlight, LCN.Documents.Field.Store.YES);
        //    field.SetTokenStream(_analyzer.GetTokenStream("content", new StringReader(textToHighlight)));
        //    doc.Add(field);
        //    writer.AddDocument(doc);
        //    writer.Commit();

        //    // Search
        //    using var reader = writer.GetReader(true);
        //    var searcher = new IndexSearcher(reader);
        //    var topDocs = searcher.Search(_query, 1);

        //    if (topDocs.ScoreDocs.Length == 0)
        //        return new List<(int Start, int End)>();

        //    var docId = topDocs.ScoreDocs[0].Doc;

        //    // Highlight with FastVectorHighlighter
        //    var fvh = new FastVectorHighlighter();
        //    var fieldQuery = fvh.GetFieldQuery(_query, reader);
        //    var fragments = fvh.GetBestFragments(fieldQuery, reader, docId, "content", int.MaxValue, new SimpleFragmentsBuilder());
        //    var fragment = fvh.GetBestFragment(fieldQuery, reader, docId, "content", int.MaxValue,
        //        new SimpleFragListBuilder(), new SimpleFragmentsBuilder(),
        //        new[] { INTERNAL_HIGHLIGHT_START }, new[] { INTERNAL_HIGHLIGHT_END },
        //        new DefaultEncoder());

        //    var ranges = new List<(int Start, int End)>();
        //    int offset = 0;
        //    int cumulativeTagLength = 0;
        //    while (true)
        //    {
        //        int startIdx = fragment.IndexOf(INTERNAL_HIGHLIGHT_START, offset);
        //        if (startIdx == -1)
        //            break;
        //        int trueStartIdx = startIdx - cumulativeTagLength;
        //        cumulativeTagLength += INTERNAL_HIGHLIGHT_START.Length;
        //        int endIdx = fragment.IndexOf(INTERNAL_HIGHLIGHT_END, startIdx + INTERNAL_HIGHLIGHT_START.Length);
        //        if (endIdx == -1)
        //            break;
        //        int trueEndIdx = endIdx - cumulativeTagLength;
        //        cumulativeTagLength += INTERNAL_HIGHLIGHT_END.Length;
        //        ranges.Add((trueStartIdx, trueEndIdx));
        //        offset = endIdx + INTERNAL_HIGHLIGHT_END.Length;
        //    }

        //    return ranges;
        //}

        private List<(int Start, int End)> GetHighlightRangesFromInternalHighlightedTextRegex(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<(int Start, int End)>();

            var pattern = Regex.Escape(INTERNAL_HIGHLIGHT_START) + @"(.*?" + Regex.Escape(INTERNAL_HIGHLIGHT_END) + @")";
            var matches = Regex.Matches(text, pattern, RegexOptions.Singleline);
            var ranges = new List<(int Start, int End)>();
            int cumulativeTagLength = 0;

            foreach (Match match in matches)
            {
                int startIdx = match.Index;
                int endIdx = match.Index + match.Length;
                int trueStartIdx = startIdx - cumulativeTagLength;
                int trueEndIdx = endIdx - cumulativeTagLength - INTERNAL_HIGHLIGHT_END.Length - INTERNAL_HIGHLIGHT_START.Length;
                ranges.Add((trueStartIdx, trueEndIdx));
                cumulativeTagLength += INTERNAL_HIGHLIGHT_START.Length + INTERNAL_HIGHLIGHT_END.Length;
            }

            return ranges;
        }


        private List<(int Start, int End)> GetHighlightRangesFromInternalHighlightedText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<(int Start, int End)>();

            var ranges = new List<(int Start, int End)>();
            int index = 0;
            int extraCharOffset = 0;

            while (index < text.Length)
            {
                int startIdx = text.IndexOf(INTERNAL_HIGHLIGHT_START, index);
                if (startIdx == -1)
                    break;

                int trueStartIdx = startIdx - extraCharOffset;
                extraCharOffset += INTERNAL_HIGHLIGHT_START.Length;

                int endIdx = text.IndexOf(INTERNAL_HIGHLIGHT_END, startIdx + INTERNAL_HIGHLIGHT_START.Length);
                if (endIdx == -1)
                    break; // Malformed input: no end marker

                int trueEndIdx = endIdx - extraCharOffset;
                extraCharOffset += INTERNAL_HIGHLIGHT_END.Length;

                ranges.Add((trueStartIdx, trueEndIdx));
                index = endIdx + INTERNAL_HIGHLIGHT_END.Length;
            }

            return ranges;
        }

        private List<Tuple<int,int>> GetHighlightRangesFromInternalHighlighedTextRecursive(string text, int startIndex = 0, int extraCharOffset = 0, List<Tuple<int, int>> ranges = null, int recursionNo = 0)
        {
            if (ranges == null) ranges = new List<Tuple<int, int>>();

            if (recursionNo < 1024)
            {
                int rawHighlightStartIdx = text.IndexOf(INTERNAL_HIGHLIGHT_START, startIndex);
                if (rawHighlightStartIdx != -1)
                {
                    // Found the next highlight start index.
                    int trueHighlightStartIdx = rawHighlightStartIdx - extraCharOffset;
                    extraCharOffset += INTERNAL_HIGHLIGHT_START.Length;
                    int rawHighlightEndIdx = text.IndexOf(INTERNAL_HIGHLIGHT_END, rawHighlightStartIdx);
                    if (rawHighlightEndIdx != -1)
                    {
                        // Found the next highlight end index.
                        int trueHighlightEndIndex = rawHighlightEndIdx - extraCharOffset;
                        extraCharOffset += INTERNAL_HIGHLIGHT_END.Length;
                        ranges.Add(new Tuple<int, int>(trueHighlightStartIdx, trueHighlightEndIndex));
                        ++recursionNo;
                        return GetHighlightRangesFromInternalHighlighedTextRecursive(text, rawHighlightEndIdx, extraCharOffset, ranges, recursionNo);
                    }
                }
            }
            return ranges;
        }

        public string[] GetHitsInContext(int maxFragments, string beforeHit, string afterHit)
        {
            var formatter = new SimpleHTMLFormatter(beforeHit, afterHit);
            Lucene.Net.Search.Highlight.Highlighter highlighter = new Highlighter(formatter, new QueryScorer(_query));
            string fText = LuceneDocument.Get("Content");
            string[] fragments = highlighter.GetBestFragments(_analyzer, "", fText, maxFragments);
            return fragments;
        }

        public string[] GetContextExcerpts(int amountOfContext, ViewerConfig.OptionContextAmountType amountType)
        {
            string[] hitsRaw = GetHitsInContext(64, "<span class='doc-hit'>", "</span>");
            int i = hitsRaw.Length;
            while (i --> 0)
            {
                string hit = hitsRaw[i];
                if (hit != null)
                {
                    // TODO Process hit to match amountType/amount of context.
                }
            }
            return hitsRaw;
        }

        public string GetFieldValue(string fieldName, string defaultValue)
        {
            string fText = LuceneDocument.Get(fieldName);
            if (fText != null)
            {
                return fText;
            } else
            {
                return defaultValue;
            }
        }

        
    }
}
