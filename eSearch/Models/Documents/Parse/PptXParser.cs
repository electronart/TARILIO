using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D = DocumentFormat.OpenXml.Drawing;

namespace eSearch.Models.Documents.Parse
{
    internal class PptXParser : IParser
    {
        public string[] Extensions
        {
            get { return new string[] { "pptx" }; }
        }

        public bool DoesParserExtractFiles => false;

        public bool DoesParserProduceSubDocuments => false;

        public void Parse(string filePath, out ParseResult parseResult)
        {
            parseResult = new();
            StringBuilder textContentsBuilder = new StringBuilder();
            using (PresentationDocument presentationDocument = PresentationDocument.Open(filePath, false))
            {
                #region Extract all Text From Slides
                var temp = GetSlideTitles(presentationDocument);
                List<string> titles = new List<string>();
                foreach(string title in titles)
                {
                    titles.Add(title);
                }
                int numSlides = CountSlides(presentationDocument);
                int s = 0;
                while (s < numSlides)
                {
                    textContentsBuilder.Append("Slide ").Append(s + 1).Append(" - ").AppendLine(titles.Count > 0 ? titles[s] : "Untitled").AppendLine();
                    string[]? slideTexts = GetAllTextInSlide(presentationDocument, s);
                    if (slideTexts == null)
                    {
                        foreach(string text in slideTexts)
                        {
                            textContentsBuilder.AppendLine(text);
                        }
                    }
                    textContentsBuilder.AppendLine();
                    ++s;
                }
                #endregion

                if (!string.IsNullOrEmpty(presentationDocument.PackageProperties.Creator))
                {
                    parseResult.Authors = new string[] { presentationDocument.PackageProperties.Creator };
                }
                if (!string.IsNullOrEmpty(presentationDocument.PackageProperties.Title))
                {
                    parseResult.Title = presentationDocument.PackageProperties.Title;
                } else
                {
                    parseResult.Title = Path.GetFileNameWithoutExtension(filePath);
                }
            }
        }

        #region Helper Methods from https://learn.microsoft.com/en-us/office/open-xml/how-to-get-all-the-text-in-a-slide-in-a-presentation - Slightly modified

        public static int CountSlides(PresentationDocument presentationDocument)
        {
            // Check for a null document object.
            if (presentationDocument == null)
            {
                throw new ArgumentNullException("presentationDocument");
            }

            int slidesCount = 0;

            // Get the presentation part of document.
            PresentationPart? presentationPart = presentationDocument.PresentationPart;
            // Get the slide count from the SlideParts.
            if (presentationPart != null)
            {
                slidesCount = presentationPart.SlideParts.Count();
            }
            // Return the slide count to the previous method.
            return slidesCount;
        }


        public static string[]? GetAllTextInSlide(PresentationDocument presentationDocument, int slideIndex)
        {
            // Verify that the presentation document exists.
            if (presentationDocument == null)
            {
                throw new ArgumentNullException("presentationDocument");
            }

            // Verify that the slide index is not out of range.
            if (slideIndex < 0)
            {
                throw new ArgumentOutOfRangeException("slideIndex");
            }

            // Get the presentation part of the presentation document.
            PresentationPart? presentationPart = presentationDocument.PresentationPart;

            // Verify that the presentation part and presentation exist.
            if (presentationPart != null && presentationPart.Presentation != null)
            {
                // Get the Presentation object from the presentation part.
                Presentation presentation = presentationPart.Presentation;

                // Verify that the slide ID list exists.
                if (presentation.SlideIdList != null)
                {
                    // Get the collection of slide IDs from the slide ID list.
                    var slideIds = presentation.SlideIdList.ChildElements;
                    if (slideIds != null)
                    {
                        // If the slide ID is in range...
                        if (slideIndex < slideIds.Count)
                        {
                            // Get the relationship ID of the slide.
                            var temp = slideIds[slideIndex];
                            if (temp != null)
                            {
                                SlideId? id = temp as SlideId;
                                if (id != null)
                                {
                                    string? slidePartRelationshipId = id.RelationshipId;
                                    if (slidePartRelationshipId != null)
                                    {
                                        // Get the specified slide part from the relationship ID.
                                        SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slidePartRelationshipId);

                                        // Pass the slide part to the next method, and
                                        // then return the array of strings that method
                                        // returns to the previous method.

                                        return GetAllTextInSlide(slidePart);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // Else, return null.
            return null;
        }

        public static string[]? GetAllTextInSlide(SlidePart slidePart)
        {
            // Verify that the slide part exists.
            if (slidePart == null)
            {
                throw new ArgumentNullException("slidePart");
            }

            // Create a new linked list of strings.
            LinkedList<string> texts = new LinkedList<string>();

            // If the slide exists...
            if (slidePart.Slide != null)
            {
                // Iterate through all the paragraphs in the slide.
                foreach (var paragraph in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                {
                    // Create a new string builder.                    
                    StringBuilder paragraphText = new StringBuilder();

                    // Iterate through the lines of the paragraph.
                    foreach (var text in paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                    {
                        // Append each line to the previous lines.
                        paragraphText.Append(text.Text);
                    }

                    if (paragraphText.Length > 0)
                    {
                        // Add each paragraph to the linked list.
                        texts.AddLast(paragraphText.ToString());
                    }
                }
            }

            if (texts.Count > 0)
            {
                // Return an array of strings.
                return texts.ToArray();
            }
            else
            {
                return null;
            }
        }

        public static IList<string> GetSlideTitles(PresentationDocument presentationDocument)
        {
            if (presentationDocument == null)
            {
                throw new ArgumentNullException("presentationDocument");
            }

            // Get a PresentationPart object from the PresentationDocument object.
            PresentationPart presentationPart = presentationDocument.PresentationPart;

            if (presentationPart != null &&
                presentationPart.Presentation != null)
            {
                // Get a Presentation object from the PresentationPart object.
                Presentation presentation = presentationPart.Presentation;

                if (presentation.SlideIdList != null)
                {
                    List<string> titlesList = new List<string>();

                    // Get the title of each slide in the slide order.
                    foreach (var slideId in presentation.SlideIdList.Elements<SlideId>())
                    {
                        SlidePart slidePart = presentationPart.GetPartById(slideId.RelationshipId) as SlidePart;

                        // Get the slide title.
                        string title = GetSlideTitle(slidePart);

                        // An empty title can also be added.
                        titlesList.Add(title);
                    }

                    return titlesList;
                }

            }

            return null;
        }

        // Get the title string of the slide.
        public static string GetSlideTitle(SlidePart slidePart)
        {
            if (slidePart == null)
            {
                throw new ArgumentNullException("presentationDocument");
            }

            // Declare a paragraph separator.
            string paragraphSeparator = null;

            if (slidePart.Slide != null)
            {
                // Find all the title shapes.
                var shapes = from shape in slidePart.Slide.Descendants<Shape>()
                             where IsTitleShape(shape)
                             select shape;

                StringBuilder paragraphText = new StringBuilder();

                foreach (var shape in shapes)
                {
                    // Get the text in each paragraph in this shape.
                    foreach (var paragraph in shape.TextBody.Descendants<D.Paragraph>())
                    {
                        // Add a line break.
                        paragraphText.Append(paragraphSeparator);

                        foreach (var text in paragraph.Descendants<D.Text>())
                        {
                            paragraphText.Append(text.Text);
                        }

                        paragraphSeparator = "\n";
                    }
                }

                return paragraphText.ToString();
            }

            return string.Empty;
        }

        // Determines whether the shape is a title shape.
        private static bool IsTitleShape(Shape shape)
        {
            var placeholderShape = shape.NonVisualShapeProperties.ApplicationNonVisualDrawingProperties.GetFirstChild<PlaceholderShape>();
            if (placeholderShape != null && placeholderShape.Type != null && placeholderShape.Type.HasValue)
            {
                switch ((PlaceholderValues)placeholderShape.Type)
                {
                    // Any title shape.
                    case PlaceholderValues.Title:

                    // A centered title.
                    case PlaceholderValues.CenteredTitle:
                        return true;

                    default:
                        return false;
                }
            }
            return false;
        }
        #endregion
    }
}
