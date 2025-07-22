using eSearch.Models.AI;
using J2N.Text;
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
            StringBuilder htmlOutput = new StringBuilder();
            StringBuilder markDownOutput = new StringBuilder();
            StringBuilder indexedTextBuilder = new StringBuilder();

            var pipeline = new MarkdownPipelineBuilder()
                .UsePipeTables()
                .Build();

            var firstMessage = convo.Messages.First();
            if (firstMessage != null)
            {
                markDownOutput.Append("**User:** ").AppendLine(firstMessage.User).AppendLine();
                markDownOutput.Append("**Machine:** ").AppendLine(firstMessage.Machine).AppendLine();

                htmlOutput.AppendLine(Markdig.Markdown.ToHtml(markDownOutput.ToString(), pipeline));


                indexedTextBuilder.AppendLine("User: ").AppendLine(firstMessage.User).AppendLine();
                indexedTextBuilder.AppendLine("Machine: ").AppendLine(firstMessage.Machine).AppendLine();
            }

            

            foreach (var message in convo.Messages)
            {
                markDownOutput.Clear();
                markDownOutput.AppendLine().AppendLine("****").AppendLine().AppendLine();
                markDownOutput.Append("**Time:** ").AppendLine(message.Time.ToShortDateString() + " " +  message.Time.ToShortTimeString()).AppendLine();
                markDownOutput.Append("**Role:** ").AppendLine(message.Role ?? "").AppendLine();
                markDownOutput.Append("**Content:** ").AppendLine(message.Content).AppendLine();
                if (!string.IsNullOrWhiteSpace(message.Note))
                {
                    markDownOutput.Append("**Note:** ").AppendLine(message.Note).AppendLine().Append(" ");
                }
                htmlOutput.AppendLine(Markdig.Markdown.ToHtml(markDownOutput.ToString(), pipeline));


                indexedTextBuilder.AppendLine("Time: "      + message.Time.ToShortDateString() + message.Time.ToShortTimeString());
                indexedTextBuilder.AppendLine("Role: " + message.Role);
                indexedTextBuilder.AppendLine("Content: " + message.Content);
                indexedTextBuilder.AppendLine("Note: "      + message.Note);
                
                indexedTextBuilder.AppendLine().AppendLine();

            }

            ParseResult result = new();
            result.ParserName = "econvo Parser";
            result.Title = Path.GetFileNameWithoutExtension(filePath);
            result.HtmlRender = htmlOutput.ToString();
            result.TextContent = indexedTextBuilder.ToString();
            result.Metadata =
            [
                new Metadata { 
                    Key = "User", 
                    Value = firstMessage?.User ?? "???" },
                new Metadata
                {
                    Key = "Machine",
                    Value = firstMessage?.Machine ?? "???"
                },
                new Metadata {
                    Key     = "Started",
                    Value   = (firstMessage?.Time ?? DateTime.MinValue).ToString("yyyy-MM-dd HH-mm-ss")
                }
            ];
            parseResult = result;
        }
    }
}
