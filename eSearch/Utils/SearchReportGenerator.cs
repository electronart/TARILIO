using eSearch;
using eSearch.ViewModels;
using NetOdt;
using NetOdt.Enumerations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DesktopSearch2.Utils
{
    public class SearchReportGenerator
    {

        // https://github.com/TobiasSekan/NetOdt
        public static async void GenerateSearchReport(QueryViewModel query, IEnumerable<ResultViewModel> results)
        {

            string outputDir = "C:/Users/Tommer/Documents/temp/test_report.odt";

            using var odtDocument = new OdtDocument();
            
            // Set global font for the all text passages for complete document
            odtDocument.SetGlobalFont("Liberation Serif", FontSize.Size12);

            // Set global colors for the all text passages for the complete document
            odtDocument.SetGlobalColors(Color.Red, Color.Transparent);

            // Set header and footer
            odtDocument.SetHeader("eSearch Search Report", TextStyle.Center);

            odtDocument.AppendImage(@"E:\Tommer\source\repos\DesktopSearch2\esearch_temp_icon.png", 10, 10);


            var tempPictureDir = Path.Combine(odtDocument.TempWorkingUri.LocalPath, "Pictures");
            

            // Append a title
            AppendResults(odtDocument, results);

            if (!Directory.Exists(tempPictureDir))
            {
                Debug.WriteLine("Creating Directory " + tempPictureDir);
                Directory.CreateDirectory(tempPictureDir);
            } else
            {
                Debug.WriteLine("Directory does exist " + tempPictureDir);
            }

            odtDocument.SaveAs(outputDir, true);

            Debug.WriteLine("Saved!");


            //odtDocument.SaveAs(@"C:\Users\Tommer\Documents\temp\test_report.odt");

            // The automatic dispose call (from the using syntax) do the rest of the work
            // (save document, delete temporary folder, free all used resources)

            // thats it :-)

            // Append a image with a width of 22.5 cm and a height of 14.1 cm
            //odtDocument.AppendImage(path: "E:/picture1.jpg", width: 22.5, height: 14.1);

        }
        private static string XmlEscape(string unescaped)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode node = doc.CreateElement("root");
            node.InnerText = unescaped;
            return node.InnerXml;
        }

        private static void AppendResults(OdtDocument document, IEnumerable<ResultViewModel> results)
        {
            foreach(var result in results)
            {
                document.AppendLine(XmlEscape(result.Title), TextStyle.HeadingLevel02, Color.Black, Color.Transparent);
                document.AppendLine(XmlEscape(result.Score + ""), TextStyle.None, Color.Black, Color.Transparent);
                document.AppendEmptyLines(1);

                var contextParagraphs = result.GetResult().GetHitsInContext(10, "!BEFORE!", "!AFTER!");
                for (int i = 0; i < contextParagraphs.Length; i++)
                {
                    var contextParagraph = contextParagraphs[i];
                    document.AppendLine( XmlEscape(contextParagraph) , TextStyle.None, Color.Black, Color.Transparent);
                }

            }
        }

        private static int GetHitCount(ResultViewModel result)
        {
            return 0; // TODO;
        }
    }
}
