using Avalonia.Controls;
using com.sun.jarsigner;
using org.apache.pdfbox.cos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis.TtsEngine;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.Export;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig.Util;

namespace eSearch.Models.Documents.Parse
{
    public class PdfParserPDFPig : IParser
    {
        public string[] Extensions
        {
            get { return new string[] { "pdf" }; }
        }

        public bool DoesParserExtractFiles => false;

        public bool DoesParserProduceSubDocuments => false;

        public void ParseOld(string filePath, out ParseResult parseResult)
        {
            parseResult = new ParseResult();
            
            parseResult.Title = Path.GetFileNameWithoutExtension(filePath);
            parseResult.ParserName = "PdfParserPDFPig";
            StringBuilder sb = new StringBuilder();
            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                foreach(var page in document.GetPages())
                {
                    sb.AppendLine("\t<p>").AppendLine(page.Text).AppendLine("</p>");
                }

                parseResult.Title = document.Information.Title ?? parseResult.Title;
                parseResult.Authors = new string[]{ document.Information.Author ?? "Unknown Author" };
                parseResult.TextContent = sb.ToString();
            }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            parseResult = new ParseResult();

            parseResult.Title = Path.GetFileNameWithoutExtension(filePath);
            parseResult.ParserName = "PdfParserPDFPig";

            HOcrTextExporter hocrTextExporter = new HOcrTextExporter(
                DefaultWordExtractor.Instance,
                DocstrumBoundingBoxes.Instance);

            ParsingOptions parseOptions = new ParsingOptions();
            parseOptions.UseLenientParsing = true;

            

            using (PdfDocument document = PdfDocument.Open(filePath,parseOptions))
            {
                string[] textContent = new string[document.NumberOfPages];
                Parallel.ForEach(document.GetPages(), page =>
                {
                    var words = page.GetWords();
                    var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
                    var pageContents = new StringBuilder();
                    foreach (var block in blocks)
                    {
                        pageContents.AppendLine(block.Text);
                        
                    }
                    textContent[page.Number - 1] = pageContents.ToString();
                });

                parseResult.Title = document.Information.Title ?? parseResult.Title;
                parseResult.Authors = new string[] { document.Information.Author ?? "Unknown Author" };
                parseResult.TextContent = string.Join("\n\n", textContent);
                parseResult.HtmlRender = AsHtml(filePath);
            }

        }

        public string AsHtml(string filePath)
        {
            //StringBuilder sb = new StringBuilder();
            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                var wordExtractorOptions = new NearestNeighbourWordExtractor.NearestNeighbourWordExtractorOptions()
                {
                    Filter = (pivot, candidate) =>
                    {
                        if (string.IsNullOrWhiteSpace(candidate.Value))
                        {
                            // pivot and candidate letters cannot belong to the same word 
                            // if candidate letter is null or white space.
                            // ('FilterPivot' already checks if the pivot is null or white space by default)
                            return false;
                        }

                        // check for height diff
                        var maxHeight = Math.Max(pivot.PointSize, candidate.PointSize);
                        var minHeight = Math.Min(pivot.PointSize, candidate.PointSize);
                        if (minHeight != 0 && maxHeight / minHeight > 2.0)
                        {
                            // pivot and candidate letters cannot belong to the same word 
                            // if one letter is more than twice the size of the other.
                            return false;
                        }

                        // check for color diff
                        var pivotRgb = pivot.Color.ToRGBValues();
                        var candidateRgb = candidate.Color.ToRGBValues();
                        if (!pivotRgb.Equals(candidateRgb))
                        {
                            // pivot and candidate letters cannot belong to the same word 
                            // if they don't have the same colour.
                            return false;
                        }
                        return true;
                    }
                };

                string[] fragments = new string[document.NumberOfPages];

                Parallel.ForEach(document.GetPages(), page =>
                {
                    var pageSB = new StringBuilder();
                    pageSB.Append("<hr class='pagebreak'><p><sub>Page " + page.Number + "</sub></p><br>");

                    #region Get Text Blocks in Docstrum bounding boxes method
                    var letters = page.Letters;

                    

                    var wordExtractor = new NearestNeighbourWordExtractor(wordExtractorOptions);

                    var words = wordExtractor.GetWords(letters);

                    var pageSegmenterOptions = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions()
                    {

                    };

                    var pageSegmenter = new DocstrumBoundingBoxes(pageSegmenterOptions);

                    var textBlocks = pageSegmenter.GetBlocks(words);

                    var readingOrder = UnsupervisedReadingOrderDetector.Instance;
                    var orderedTextBlocks = readingOrder.Get(textBlocks);
                    #endregion

                    List<int> fontSizes = GetFontSizes(orderedTextBlocks);
                    foreach (var textBlock in orderedTextBlocks)
                    {
                        pageSB.Append(TextBlock2Html(textBlock, fontSizes));
                    }
                    fragments[page.Number - 1] = pageSB.ToString();
                });

                return string.Join(string.Empty, fragments);
            }
        }

        /// <summary>
        /// Gets an array of up to 7 unique font sizes
        /// The first element should be the paragraph font size.
        /// The next elements are headers, smallest to largest.
        /// </summary>
        /// <param name="textBlocks"></param>
        /// <returns></returns>
        public List<int> GetFontSizes(IEnumerable<UglyToad.PdfPig.DocumentLayoutAnalysis.TextBlock> textBlocks)
        {
            List<int>    fontSizes = new List<int>();
            HashSet<int> uniqueFontSizes = new HashSet<int>();
            foreach(var textBlock in textBlocks)
            {
                foreach(var line in textBlock.TextLines)
                {
                    foreach(var word in line.Words)
                    {
                        foreach(var letter in word.Letters)
                        {
                            uniqueFontSizes.Add(Convert.ToInt32(letter.FontSize));
                            fontSizes.Add(Convert.ToInt32(letter.FontSize));
                        }
                    }
                }
            }

            // Consider the most frequent font size (the mode) in the pdf document to be the 'paragraph' font size.
            if (fontSizes.Count > 0)
            {
                int font_size_mode_value = fontSizes
                                            .GroupBy(n => n)
                                            .OrderByDescending(g => g.Count())
                                            .ThenBy(g => g.Key)
                                            .First().Key;

                List<int> biggerUniqueFonts = new List<int>();
                foreach (var uniqueFontSize in uniqueFontSizes)
                {
                    if (uniqueFontSize > font_size_mode_value)
                    {
                        biggerUniqueFonts.Add(uniqueFontSize);
                    }
                }
                biggerUniqueFonts.Sort();

                int totalHeaders = Math.Min(6, biggerUniqueFonts.Count);


                List<int> returnSizes = new List<int>();
                returnSizes.Add(font_size_mode_value);
                if (biggerUniqueFonts.Count > 6)
                {
                    List<List<int>> clustered_sizes = GetClusters(biggerUniqueFonts, 6);
                    foreach (List<int> cluster in clustered_sizes)
                    {
                        returnSizes.Add(Convert.ToInt32(cluster.Average()));
                    }
                }
                else
                {
                    returnSizes.AddRange(biggerUniqueFonts);
                }
                return returnSizes;
            } else {
                // No fontsizes?
                return new List<int> { 1 };
            
            }
            
            

        }

        public static int FindClosest(List<int> numbers, int target)
        {
            return numbers.OrderBy(n => Math.Abs(n - target)).First();
        }

        public static List<List<int>> GetClusters(List<int> numbers, int clusterCount)
        {

            // Sort the numbers to simplify clustering
            numbers.Sort();

            // Initialize cluster centers evenly spaced across the range of numbers
            var min = numbers.First();
            var max = numbers.Last();
            var centers = Enumerable.Range(0, clusterCount)
                                    .Select(i => min + i * (max - min) / (clusterCount - 1))
                                    .ToList();

            List<List<int>> clusters;
            bool hasChanged;

            do
            {
                // Assign numbers to the closest cluster center
                clusters = centers.Select(_ => new List<int>()).ToList();
                foreach (var number in numbers)
                {
                    int closestCenterIndex = centers
                        .Select((center, index) => new { Center = center, Index = index })
                        .OrderBy(x => Math.Abs(x.Center - number))
                        .First().Index;

                    clusters[closestCenterIndex].Add(number);
                }

                // Recalculate centers
                var newCenters = clusters
                    .Where(cluster => cluster.Count > 0) // Avoid dividing by zero
                    .Select(cluster => cluster.Sum() / cluster.Count) // Compute the average
                    .ToList();

                hasChanged = !centers.SequenceEqual(newCenters);
                centers = newCenters;

            } while (hasChanged);

            return clusters;
        }


        public string TextBlock2Html(UglyToad.PdfPig.DocumentLayoutAnalysis.TextBlock textBlock, List<int> fontSizes)
        {
            StringBuilder sb = new StringBuilder();
            int headingLevel = -1;

            bool bold       = false;
            bool italic     = false;
            //bool underline  = false;

            foreach (var line in textBlock.TextLines)
            {
                
                foreach(var word in line.Words)
                {
                    if (word.Letters.Count > 0)
                    {
                        int fontSize  = FindClosest(fontSizes, Convert.ToInt32(word.Letters[0].FontSize));
                        int idx = fontSizes.IndexOf(fontSize);
                        if (fontSizes.Count < 6 && idx != 0)
                        {
                            idx += (6 - fontSizes.Count); // So that it prefers larger headings.
                        }
                        if (idx != headingLevel)
                        {
                            if (headingLevel != -1)
                            {
                                sb.Append(getHeadingEnd(headingLevel));
                            }
                            headingLevel = idx;
                            sb.Append(getHeadingStart(headingLevel));
                        }



                        foreach(var letter in word.Letters)
                        {
                            if (letter.Font.IsBold && !bold)
                            {
                                bold = true;
                                sb.Append("<b>");
                            }
                            if (bold && !letter.Font.IsBold)
                            {
                                bold = false;
                                sb.Append("</b>");
                            }
                            if (letter.Font.IsItalic && !italic)
                            {
                                italic = true;
                                sb.Append("<i>");
                            }
                            if (italic && !letter.Font.IsItalic)
                            {
                                italic = false;
                                sb.Append("</i>");
                            }
                            sb.Append(HttpUtility.HtmlEncode(letter.Value));
                        }
                        sb.Append(" ");

                    }
                }
            }

            if (bold)
            {
                sb.Append("</b>");
            }
            if (italic)
            {
                sb.Append("</i>");
            }

            if (headingLevel != -1)
            {
                sb.Append(getHeadingEnd(headingLevel));
            }

            return sb.ToString();
        }

        string getHeadingStart(int headingLevel)
        {
            List<string> headings = new List<string>{ "<p>", "<h6>", "<h5>", "<h4>", "<h3>", "<h2>", "<h1>" };
            return headings[headingLevel];
        }

        string getHeadingEnd(int headingLevel)
        {
            List<string> headings = new List<string> { "</p>", "</h6>", "</h5>", "</h4>", "</h3>", "</h2>", "</h1>" };
            return headings[headingLevel];
        }

    }
}
