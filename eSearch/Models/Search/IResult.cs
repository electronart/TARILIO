using DocumentFormat.OpenXml.ExtendedProperties;
using eSearch.Interop;
using eSearch.Models.Documents;
using eSearch.Models.Documents.Parse;
using eSearch.Models.Indexing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public IEnumerable<Run> GetHitsInContextAsRuns(int maxParagraphs)
        {

            string[] hitsInContextParagraphs = GetHitsInContext(maxParagraphs, "<<HIT>>", "<</HIT>>");
            foreach (var paragraph in hitsInContextParagraphs)
            {
                int index = 0;

                while (index < paragraph.Length)
                {
                    int start = paragraph.IndexOf("<<HIT>>", index, StringComparison.Ordinal);
                    if (start < 0)
                    {
                        // No more hits: return the remainder as non-hit text
                        var tail = paragraph.Substring(index);
                        if (tail.Length > 0)
                            yield return new Run(tail, false);
                        break;
                    }

                    // Emit the non-hit segment before the hit
                    if (start > index)
                    {
                        yield return new Run(paragraph.Substring(index, start - index), false);
                    }

                    // Move past the start tag
                    int hitContentStart = start + "<<HIT>>".Length;

                    int end = paragraph.IndexOf("<</HIT>>", hitContentStart, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        // Malformed input: treat the rest as hit text
                        yield return new Run(paragraph.Substring(hitContentStart), true);
                        break;
                    }

                    // Emit the hit content
                    yield return new Run(paragraph.Substring(hitContentStart, end - hitContentStart), true);

                    // Move past the end tag
                    index = end + "<</HIT>>".Length;
                }
            }
        }

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

        public class Run
        {
            public Run(string text, bool isHit)
            {
                this.Text = text;
                this.IsHit = isHit;
            }

            public readonly string Text;
            public readonly bool   IsHit;
        }
    }
}
