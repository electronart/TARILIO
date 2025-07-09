using eSearch.Interop.AI;
using eSearch.Models.AI.MCP.LocalTransport;
using eSearch.Models.Indexing;
using eSearch.ViewModels;
using jdk.nashorn.@internal.ir;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace eSearch.Models.AI.MCP.Tools
{
    public class MCPSearchServer : IESearchMCPServer
    {

        private readonly Channel<JsonRpcMessage> _clientToServerChannel = Channel.CreateUnbounded<JsonRpcMessage>();
        private readonly Channel<JsonRpcMessage> _serverToClientChannel = Channel.CreateUnbounded<JsonRpcMessage>();

        public MCPSearchServer()
        {
            
        }

        public string DisplayName => "eSearch Search";

        public bool IsServerRunning { get; set; } = false;

        public IReadOnlyList<string> ConsoleOutputDisplayLines
        {
            get
            {
                // TODO
                string output = IsServerRunning ? "Running" : "Not Running";
                return new List<string>() { output };
            }
        }

        public bool IsErrorState => false;

        public IClientTransport? GetClientTransport()
        {
            return new LocalClientTransport(_serverToClientChannel.Reader, _clientToServerChannel.Writer);
        }

        public async Task<bool> StartServer()
        {
            IsServerRunning = true;
            return true;
        }

        public async Task<bool> StopServer()
        {
            IsServerRunning = false;
            return true;
        }
    }


    [McpServerToolType]
    public static class MCPSearchServerTool
    {
        [McpServerTool, Description("Search an index for documents.")]
        public static async Task<string> PerformSearch(
            [Description("The name of the user's index to search")] string indexName,
            [Description("The Search Query to perform. Supports the Lucene Search Syntax")] string query
            )
        {
            if (!string.IsNullOrEmpty(query))
            {
                return "Error. Query was empty.";
            }
            if (string.IsNullOrEmpty(indexName))
            {
                return "Error. Index name must not be empty. Use the list indexes tool to discover index names";
            }
            var index = Program.IndexLibrary.GetIndex(indexName);
            if (index == null)
            {
                return $"Error. No index with the name ${indexName} was found.";
            }
            var qvm = new QueryViewModel(); // Use the application defaults..
            qvm.Query = query;
            var results = index.PerformSearch(qvm);
            int i = 0;

            StringBuilder sb = new StringBuilder();

            if (results.Count > 0)
            {
                sb.Append("Total Results: ").AppendLine(results.Count.ToString());
                sb.AppendLine("Showing hits in context for the top 5 results.");

                foreach (var result in results)
                {
                    if (i < 5)
                    {
                        sb.AppendLine();
                        sb.Append("Excerpt from result " + (i + 1)).Append(" - ").AppendLine(result.DisplayedTitle);
                        sb.Append("```");
                        sb.Append(result.GetResult().GetHitsInContext(5, "", "")).Append("```");
                    }
                    ++i;
                }

            }
            else
            {
                sb.AppendLine($"There were no results for the query ${query} in index ${indexName}");
            }
            return sb.ToString();
        }

        [McpServerTool, Description("Returns the names of all indexes that can be searched.")]
        public static async Task<string> ListIndexes()
        {
            StringBuilder sb = new StringBuilder();
            var indexes = Program.IndexLibrary.GetAllIndexes();
            if (indexes.Count == 0)
            {
                return "The user has not configured any indexes.";
            }
            sb.AppendLine("Available Indexes:");
            foreach (var index in indexes)
            {
                sb.AppendLine(index.Name);
            }
            return sb.ToString();
        }
    }
}
