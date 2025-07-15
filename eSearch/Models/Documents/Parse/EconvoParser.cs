using eSearch.Models.AI;
using Markdig;
using Markdig.Parsers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Documents.Parse
{
    public class EconvoParser : IParser
    {
        public string[] Extensions
        {
            get { return new string[] { "econvo" }; }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            var convo = JsonConvert.DeserializeObject<Conversation>(System.IO.File.ReadAllText(filePath));
            if (convo == null) throw new NullReferenceException("Conversation null");
            StringBuilder markDownOutput = new StringBuilder();
            StringBuilder indexedTextBuilder = new StringBuilder();
            foreach (var message in convo.Messages)
            {
                markDownOutput.AppendLine("****").AppendLine();
                    
                markDownOutput.AppendLine("| Time | User | Machine | Notes |");
                markDownOutput.AppendLine("|-----|-----|--------|------|");
                markDownOutput.Append("|" + message.Time.ToShortDateString() + " " + message.Time.ToShortTimeString());
                markDownOutput.Append("|").Append(message.User).Append("|").Append(message.Machine).Append("|").Append(message.Note.Replace("|", "_")).Append("|");
                markDownOutput.AppendLine().AppendLine();
                markDownOutput.AppendLine(message.Role);
                markDownOutput.AppendLine().AppendLine(message.Content).AppendLine().AppendLine();

                indexedTextBuilder.AppendLine("Time: "      + message.Time.ToShortDateString() + message.Time.ToShortTimeString());
                indexedTextBuilder.AppendLine("User: "      + message.User ?? "");
                indexedTextBuilder.AppendLine("Machine: "   + message.Machine);
                indexedTextBuilder.AppendLine("Note: "      + message.Note);
                indexedTextBuilder.AppendLine("Role: "      + message.Role);
                indexedTextBuilder.AppendLine("Content: "   + message.Content);
                indexedTextBuilder.AppendLine().AppendLine();

            }

            var pipeline = new MarkdownPipelineBuilder()
                .UsePipeTables()
                .Build();

            string output = markDownOutput.ToString();

            var htmlRender = Markdig.Markdown.ToHtml(output, pipeline);
            ParseResult result = new();
            result.ParserName = "econvo Parser";
            result.Title = Path.GetFileNameWithoutExtension(filePath);
            result.HtmlRender = htmlRender;
            result.TextContent = indexedTextBuilder.ToString();
            parseResult = result;
        }
    }
}
