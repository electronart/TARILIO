using eSearch.Models.Configuration;
using eSearch.Models.Documents.Parse;
using eSearch.Utils;
using ModelContextProtocol.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using S = eSearch.ViewModels.TranslationsViewModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using eSearch.Interop.AI;

namespace eSearch.Models.AI
{
    public class Completions
    {

        private static string GetCurrentChatModel(AISearchConfiguration aiConfig)
        {
            string model = string.Empty;
            switch (aiConfig.LLMService)
            {
                case LLMService.Perplexity:
                    model = aiConfig.PerplexityModel.GetDescription();
                    break;
                default:
                    model = aiConfig.Model;
                    break;
            }
            return model;
        }

        private static string GetOpenAIEndpointURL(AISearchConfiguration aiConfig)
        {
            switch(aiConfig.LLMService)
            {
                case LLMService.Perplexity:
                    return "https://api.perplexity.ai";
                case LLMService.ChatGPT:
                    return "https://api.openai.com/v1";
                default:
                    return aiConfig.ServerURL;
            }
        }

        /// <summary>
        /// Note that this method does not handle exceptions but is likely to trigger them as it uses external network resources. Always try/catch.
        /// </summary>
        /// <param name="startText"></param>
        /// <returns></returns>
        public static async Task<Completion> CompleteText(AISearchConfiguration aiConfig, string startText, CancellationToken cancellationToken = default)
        {
            switch(aiConfig.LLMService)
            {
                case LLMService.Perplexity:

                default:
                    break;
            }
            var completion = await GetCompletionViaOpenAIUrlAsync(aiConfig, startText, cancellationToken);
            return completion;
        }

        /// <summary>
        /// Note this is the NON-LOCALIZED version.
        /// </summary>
        public static string DEFAULT_SYSTEM_PROMPT = "Answer in English. You are a helpful research assistant";

        private static string GetSystemPrompt(AISearchConfiguration aiConfig)
        {
            if (!string.IsNullOrWhiteSpace(aiConfig.CustomSystemPrompt)) return aiConfig.CustomSystemPrompt;
            return S.Get(DEFAULT_SYSTEM_PROMPT);
        }

        private static async Task<Completion> GetCompletionViaOpenAIUrlAsync(
            AISearchConfiguration aiConfig, 
            string startText,
            CancellationToken cancellationToken = default)
        {

            

            Dictionary<string, string> systemMessage = new Dictionary<string, string>
            {
                { "role", aiConfig.SystemPromptRole.ToLower() },
                { "content", GetSystemPrompt(aiConfig) }
            };

            Dictionary<string, string> userMessage = new Dictionary<string, string>
            {
                { "role", "user" },
                { "content", startText }
            };

            Dictionary<string, object> requestParameters = new Dictionary<string, object>
            {
                { "messages", new List<Dictionary<string,string>> { systemMessage, userMessage } },
                { "model", GetCurrentChatModel(aiConfig) }
            };

            string body = JsonConvert.SerializeObject(requestParameters);
            string apiKey = Utils.Base64Decode(aiConfig.APIKey);
            string url = GetOpenAIEndpointURL(aiConfig) + "/chat/completions";
            string userAgent = "eSearch";


            string       completedText = "";
            List<string> citations = new List<string>();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                client.Timeout = TimeSpan.FromSeconds(10 * 60);

                HttpContent content = new StringContent(body, Encoding.UTF8, "application/json");
                string responseBody;
                try
                {
                    var response = await client.PostAsync(url, content, cancellationToken);
                    responseBody = await response.Content.ReadAsStringAsync();
                    response.EnsureSuccessStatusCode();

                    
                    var responseParameters = JObject.Parse(responseBody);
                    if (responseParameters.Property("choices")?.Value is JArray jArray)
                    {
                        if (jArray.Count > 0 && jArray[0] is JObject choice)
                        {
                            completedText = (string)choice["message"]["content"] ?? string.Empty;
                        }
                    }
                    if (responseParameters.Property("citations") != null) // This is not part of OpenAI Spec - It is a Perplexity Extension.
                    {
                        citations.AddRange(responseParameters["citations"].ToObject<List<string>>());
                    }
                    Completion completion = new Completion
                    {
                        Citations = citations,
                        Text = completedText,
                    };
                    return completion;
                }
                catch (HttpRequestException ex)
                {
                    Debug.WriteLine($"Req Error: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unexpected Error: {ex.Message}", ex);
                    throw;
                }
            }


        }

        List<string> outputsDebug;
        

        /// <param name="aiConfig"></param>
        /// <param name="startText"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="HttpRequestException"/>
        /// <returns></returns>
        public static async IAsyncEnumerable<string> GetCompletionStreamViaOpenAIUrlAsync(
            AISearchConfiguration aiConfig,
            string startText,
            CancellationToken cancellationToken = default)
        {

            List<string> outputsDebug = new List<string>();

            int _streamOutputCharCount = 0;

            // Prepare the request body (same as before)
            Dictionary<string, string> systemMessage = new Dictionary<string, string>
            {
                { "role", aiConfig.SystemPromptRole.ToLower() },
                { "content", GetSystemPrompt(aiConfig) }
            };

            Dictionary<string, string> userMessage = new Dictionary<string, string>
            {
                { "role", "user" },
                { "content", startText }
            };

            Dictionary<string, object> requestParameters = new Dictionary<string, object>
            {
                { "messages", new List<Dictionary<string, string>> { systemMessage, userMessage } },
                { "model", GetCurrentChatModel(aiConfig) },
                { "stream", true } // Enable streaming
            };

            string body = JsonConvert.SerializeObject(requestParameters);
            string apiKey = Utils.Base64Decode(aiConfig.APIKey);
            string url = GetOpenAIEndpointURL(aiConfig) + "/chat/completions";
            string userAgent = "eSearch";

            JObject? lastChunk = null;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                client.Timeout = TimeSpan.FromSeconds(10 * 60);

                HttpContent content = new StringContent(body, Encoding.UTF8, "application/json");

                // Send the request and get the response stream
                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string? line;
                    while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                    {
                        // OpenAI streams data in SSE format: "data: {json}"
                        if (line.StartsWith("data:"))
                        {
                            string jsonData = line.Substring("data:".Length).Trim();

                            // Check for stream termination
                            if (jsonData == "[DONE]")
                            {
                                break; // End the stream
                            }

                            // Parse the JSON chunk
                            lastChunk = JObject.Parse(jsonData);
                            outputsDebug.Add(jsonData);
                            if (lastChunk?["choices"] is JArray choices && choices.Count > 0)
                            {
                                
                                string? delta = lastChunk?["choices"]?[0]?["delta"]?["content"]?.ToString();
                                if (!string.IsNullOrEmpty(delta))
                                {
                                    _streamOutputCharCount += delta.Length;
                                    yield return delta; // Yield each chunk of text
                                }

                                string? finishReason = lastChunk?["choices"]?[0]?["finish_reason"]?.ToString();
                                if (finishReason == "stop")
                                {
                                    // Final chunk might be here; ensure no content is missed
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (lastChunk?["citations"] is JArray citations && citations.Count > 0)
            {
                StringBuilder citationsBuilder = new StringBuilder();
                citationsBuilder.Append("\n\n## Citations\n\n");
                foreach (var citation in citations)
                {
                    citationsBuilder.Append("1. <").Append( citation.ToString() ).Append("> \n");
                }
                yield return citationsBuilder.ToString();
            }
//#if DEBUG
//            yield return "\n\n\n# Debug\n\n";
//            foreach(var outputDebug in outputsDebug)
//            {
//                yield return "```json\n" + outputDebug + "\n```\n\n";
//            }
//            yield return "+-+-";
//#endif

        }


        public static Conversation GetDefaultConversationStarter(AISearchConfiguration aiConfig)
        {
            Conversation conversation = new Conversation();
            conversation.Messages.Add(new Message
            {
                Content = GetSystemPrompt(aiConfig),
                Role = aiConfig.SystemPromptRole.ToLower(),
                Model = aiConfig.Model ?? string.Empty,
            });
            return conversation;
        }

        public static async IAsyncEnumerable<string> GetCompletionStreamViaMCPAsync(
        AISearchConfiguration aiConfig,
        Conversation conversation,
        CancellationToken cancellationToken = default)
        {
            // Set up Semantic Kernel with OpenAI
            var builder = Kernel.CreateBuilder();

            string modelId = GetCurrentChatModel(aiConfig);
            Uri endpoint = new Uri(GetOpenAIEndpointURL(aiConfig));
            string apiKey = Utils.Base64Decode(aiConfig.APIKey);

            builder.AddOpenAIChatCompletion(modelId: modelId,
                                            endpoint: endpoint,
                                            apiKey: apiKey );

            var kernel = builder.Build();

            // Collect tools from all MCP servers
            var mcpServers = Program.ProgramConfig.GetAllAvailableMCPServers();
            var allKernelFunctions = new List<KernelFunction>();

            bool useKernelPlugins = false;

            foreach (var runningServer in mcpServers.Where(s => s.IsServerRunning))
            {
                var transport = runningServer.GetClientTransport();
                if (transport != null)
                {
                    string error = string.Empty;
                    try
                    {
                        var mcpClient = await McpClientFactory.CreateAsync(transport, null, null, cancellationToken);
                        var tools = await mcpClient.ListToolsAsync();
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                        var kernelFunctions = tools.Select(t => t.AsKernelFunction()).ToList();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                        if (kernelFunctions.Count > 0)
                        {
                            useKernelPlugins = true;
                            string semanticKernelPluginName = IESearchMCPServer.ToSemanticKernelSafePluginName(runningServer.DisplayName);
                            kernel.Plugins.AddFromFunctions(semanticKernelPluginName, kernelFunctions);
                        }
                    }
                    catch (Exception ex)
                    {
                        error = $"MCP Server {runningServer.DisplayName} threw an Exception: {ex.Message}";
                        Debug.WriteLine(ex.ToString());

                    }
                    if (!string.IsNullOrEmpty(error)) { yield return "```\nError: " + error + "\n```\n\n"; }
                }
            }

            var settings = new OpenAIPromptExecutionSettings
            {
                // Defaults.
            };

            if (useKernelPlugins)
            {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//                settings.ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions;
                settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true });

            }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // Convert conversation to Semantic Kernel format
            var chatHistory = new ChatHistory();
            foreach (var message in conversation.Messages)
            {
                chatHistory.AddMessage(
                    message.Role switch
                    {
                        "system" => AuthorRole.System,
                        "user" => AuthorRole.User,
                        "assistant" => AuthorRole.Assistant,
                        _ => throw new ArgumentException($"Invalid role: {message.Role}")
                    },
                    message.Content
                );
            }

            // Get chat completion service
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // Stream the response

            /*
             * 
             * 
             * I send a couple of 'signals' through the stream to help pretty-up completions
             * 
             * 1. "|||+-+NEWMESSAGE|||"
             *    Indicates the start of a new message in the stream that should be displayed seperately
             *    to previous output
             * 2. "|||+-+ROLE,Assistant|||"
             *    Indicates a role change.
             *    The value after the comma is the new role.
             *    
             */

            AuthorRole? lastRole = null;
            string? lastCompletionId = "X"; // This ensures there will always be a new message signal



            await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(
                chatHistory,
                settings,
                kernel,
                cancellationToken))
            {
                if (chunk != null)
                {

                    if (!string.IsNullOrEmpty(chunk.Content))
                    {
                        if (lastCompletionId != (string)(chunk.Metadata?["CompletionId"] ?? string.Empty))
                        {
                            lastCompletionId = (string)(chunk.Metadata?["CompletionId"] ?? string.Empty);
                            //yield return "|||+-+NEWMESSAGE|||"; // Send the new message signal..
                        }
                        if (chunk.Role != lastRole)
                        {
                            lastRole = chunk.Role;
                            //yield return "|||+-+ROLE," + chunk.Role.ToString() + "|||";

                        }
                        yield return chunk.Content;
                    }
                }
            }
        }



        public static async Task<Completion> CompleteText(string startText, CancellationToken cancellationToken = default)
        {
            var aiConfig = Program.ProgramConfig.GetSelectedConfiguration();
            return await GetCompletionViaOpenAIUrlAsync(aiConfig, startText, cancellationToken);
        }

        public static string GetCompletionAsHtmlDisplay(Completion completion)
        {
            string body = "\n<span class='ai-answer'>";
            body += MarkDownParserMarkDig.ToHtml(completion.Text);
            body += "</span>";
            if (completion.Citations.Count > 0)
            {
                body += "<hr>";
                body += "\n<ol class='ai-citations'>";
                foreach (var citation in completion.Citations)
                {

                    body += "<li>";
                    body += "<a href=\"" + HttpUtility.HtmlEncode(citation) + "\">" + HttpUtility.HtmlEncode(citation) + "</a>";
                    body += "</li>";
                }
                body += "</ol>";
            }
            return body;
        }




    }
}
