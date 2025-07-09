using eSearch.Interop;
using eSearch.Models.Documents;
using eSearch.Models.Documents.Parse;
using eSearch.Models.Indexing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DesktopSearch2.Models.Configuration.ViewerConfig;

namespace eSearch.Models.Search
{
    public interface IResult : IDisposable
    {

        public int ResultIndex { get; }

        /// <summary>
        /// How many hits found in the document. Also includes Metadata hits.
        /// </summary>
        public int Score { get; }

        public string Title { get; }

        public string Content { get; }

        /// <summary>
        /// Try to get the field value of a given named field.
        /// Will return default value if the field is not found.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string GetFieldValue(string fieldName, string  defaultValue);

        public IEnumerable<Metadata> Metadata { get; }

        /// <summary>
        /// The Words of Context for this Search Result.
        /// </summary>
        public string Context { get; }

        /// <summary>
        /// Get words/paragraphs of context.
        /// </summary>
        /// <param name="amountOfContext"></param>
        /// <param name="amountType"></param>
        /// <returns></returns>
        public string[] GetContextExcerpts(int amountOfContext, OptionContextAmountType amountType);

        /// <summary>
        /// The Document that contains the hits.
        /// </summary>
        public IDocument Document { get; }

        /// <summary>
        /// The Index that contains this result.
        /// </summary>
        public IIndex Index { get; }

        public string[] GetHitsInContext(int maxParagraphs, string beforeHit, string afterHit);

        /// <summary>
        /// The parser that was used to index the text contents of the document in this result.
        /// </summary>
        public string Parser { get; }

        /// <summary>
        /// Extract a HTML Render, if supported.
        /// </summary>
        /// <param name="escape_html">Escapes any html entities in the document, but keeps highlight tags</param>
        /// <returns>Raw HTML</returns>
        /// <exception cref="NotSupportedException">Not all results may support html renders.</exception>
        public string ExtractHtmlRender(bool escape_html, bool hit_highlight);

        /// <summary>
        /// Hit highlight arbitrary text according to the query that produced this result.
        /// </summary>
        /// <param name="text">Text to highlight</param>
        /// <returns>Highlighted text. This will be the exact same text if there's nothing to highlight.</returns>
        public string HighlightTextAccordingToResultQuery(string text);

        /// <summary>
        /// Get Hit Highlight Ranges for arbitrary text according to the query that produced this result.
        /// </summary>
        /// <param name="textToHighlight"></param>
        /// <returns></returns>
        public List<(int Start, int End)> GetHighlightRanges(string textToHighlight);
    }
}
