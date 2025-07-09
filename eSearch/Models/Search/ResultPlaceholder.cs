using DesktopSearch2.Models.Configuration;
using eSearch.Interop;
using eSearch.Models.Documents.Parse;
using eSearch.Models.Indexing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search
{
    internal class ResultPlaceholder : IResult
    {
        public int ResultIndex { get; set; } = 0;

        public int Score { get; set; } = 0;

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public IEnumerable<Metadata> Metadata
        {
            get
            {
                yield break;
            }
        }

        public string Context { get; set; } = string.Empty;

        public IDocument Document { get; set; } = null;

        public IIndex Index { get; set; }

        public string Parser { get; set; } = string.Empty;

        public void Dispose()
        {
            
        }

        public string ExtractHtmlRender(bool escape_html, bool hit_highlight)
        {
            return Content;
        }

        public string[] GetContextExcerpts(int amountOfContext, ViewerConfig.OptionContextAmountType amountType)
        {
            return new string[] { };
        }

        public string GetFieldValue(string fieldName, string defaultValue)
        {
            return defaultValue;
        }

        public List<(int Start, int End)> GetHighlightRanges(string textToHighlight)
        {
            return new List<(int Start, int End)>();
        }

        public string[] GetHitsInContext(int maxParagraphs, string beforeHit, string afterHit)
        {
            return new string[0];
        }

        public string HighlightTextAccordingToResultQuery(string text)
        {
            return text;
        }
    }
}
