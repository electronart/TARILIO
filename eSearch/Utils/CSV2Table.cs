using eSearch.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Utils
{
    public static class CSV2HtmlTable
    {
        public static string FromText(string csv_text, string seperator = "\n")
        {
            string[] lines = csv_text.Split(seperator);

            StringBuilder output = new StringBuilder();

            output.Append("<html><body><table>");
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (i < lines.Length - 1 || (i == lines.Length - 1 && !string.IsNullOrWhiteSpace(line)))
                {
                    output.Append("<tr><td>" + string.Join("</td><td>", line.Split(',')) + "</td></tr>");
                }
            }
            output.Append("</table></body></html>");

            return output.ToString();
        }
    }
}
