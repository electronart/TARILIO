using eSearch.Interop.AI;
using eSearch.Models.Configuration;
using eSearch.Models.Documents;
using eSearch.Models.Documents.Parse;
using eSearch.Models.Logging;
using eSearch.Utils;
using LLama;
using LLama.Abstractions;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using LLamaSharp.SemanticKernel;
using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static eSearch.Models.AI.OpenAIChatCompletions;
using MSK = Microsoft.SemanticKernel;
using S = eSearch.ViewModels.TranslationsViewModel;

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

        /// <summary>
        /// Attempt to list the models available at the given endpoint. 
        /// </summary>
        /// <param name="endpointURL">eg. "https://api.openai.com/v1"</param>
        /// <param name="api_key">API Key or null</param>
        /// <returns>May return null on error.</returns>
        public static async Task<List<string>?> TryGetAvailableModelIds(AISearchConfiguration config, string? api_key, CancellationToken cancellationToken)
        {
            
            try
            {
                var modelIds = new List<string>();
                var options     = new OpenAIClientOptions { Endpoint = new Uri(GetOpenAIEndpointURL(config)) };
                var credentials = (api_key != null) ? new ApiKeyCredential(api_key) : null ;
                var client = new OpenAIClient(credentials, options);
                var modelClient = client.GetOpenAIModelClient();
                var models = await modelClient.GetModelsAsync(cancellationToken);
                
                
                foreach(var model in models.Value)
                {
                    modelIds.Add(model.Id);
                }
                return modelIds;

            } catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        public static async Task<List<string>> TryGetAvailableModels(string endpointURL, string? api_key, CancellationToken cancellationToken)
        {
            try
            {
                List<string> models = new List<string>();
                string url = $"{endpointURL}/models";

                using HttpClient client = new HttpClient();
                if (!string.IsNullOrEmpty(api_key))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api_key);
                }
                else
                {
                    throw new ArgumentException("API key is required.");
                }

                HttpResponseMessage httpResponse = await client.GetAsync(url, cancellationToken);
                httpResponse.EnsureSuccessStatusCode(); // Throws if the response is not successful

                string jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                JObject response = JObject.Parse(jsonResponse);
                var data = response.ContainsKey("data") ? response["data"] : null;
                if (data == null) throw new FormatException($"Unexpected Response Format - Couldn't find 'data' key in \n{jsonResponse}\n");
                if (data is JArray array)
                {
                    foreach(var arrayItem in array)
                    {
                        if (arrayItem is JObject model)
                        {
                            string obj_type = model.ContainsKey("object") ? model["object"].ToString() : string.Empty;
                            if (obj_type == "model")
                            {
                                string model_id = model.ContainsKey("id") ? model["id"].ToString() : string.Empty;
                                if (!string.IsNullOrEmpty(model_id))
                                {
                                    models.Add(model_id);
                                }
                            }
                        }
                    }
                }


                return models;
            } catch (Exception ex)
            {
                Debug.WriteLine($"Error listing models {ex.ToString()}");
                throw;
            }
        }

        public static string GetOpenAIEndpointURL(AISearchConfiguration aiConfig)
        {
            if (aiConfig.LLMService == LLMService.Custom) return aiConfig.ServerURL;
            return GetOpenAIEndpointURL(aiConfig.LLMService);
        }

        /// <summary>
        /// Note this method does not support Custom sources, uses the method that takes an AISearchConfiguration argument instead.
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static string GetOpenAIEndpointURL(LLMService service)
        {
            switch (service)
            {
                case LLMService.OpenRouter:
                    return "https://openrouter.ai/api/v1";
                case LLMService.LMStudio:
                    return "http://127.0.0.1:1234/v1";
                case LLMService.Ollama:
                    return "http://127.0.0.1:11434/v1";
                case LLMService.Perplexity:
                    return "https://api.perplexity.ai";
                case LLMService.ChatGPT:
                    return "https://api.openai.com/v1";
                default:
                    return string.Empty;
            }
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
                Model = aiConfig.GetDisplayedModelName()
            });
            return conversation;
        }

        public static async IAsyncEnumerable<string> GetCompletionStreamViaMCPAsync(
        AISearchConfiguration aiConfig,
        Conversation conversation,
        IEnumerable<FileSystemDocument>? attachments,
        CancellationToken cancellationToken = default)
        {
            var generationConfig = (aiConfig.GenerationConfiguration != null) ? aiConfig.GenerationConfiguration : new LLMGenerationConfiguration();
            var inferenceParams = GetInferenceParamsBasedOnConfig(generationConfig);
            if (aiConfig.LocalLLMConfiguration != null)
            {
                // Using LocalLama
                await foreach(var token in GetCompletionViaLocalLLM(aiConfig.LocalLLMConfiguration, conversation, inferenceParams, cancellationToken))
                {
                    yield return token;
                }
                yield break;
            }
            // Set up Semantic Kernel with OpenAI

            var customHttpClient = new HttpClient();
            customHttpClient.DefaultRequestHeaders.Add("Referer", "https://github.com/electronart/esearch-project");
            customHttpClient.DefaultRequestHeaders.Add("X-Title", Program.ProgramConfig.GetProductTagText());

            var builder = Kernel.CreateBuilder();

            string modelId = GetCurrentChatModel(aiConfig);
            Uri endpoint = new Uri(GetOpenAIEndpointURL(aiConfig));
            string apiKey = Utils.Base64Decode(aiConfig.APIKey);

            builder.AddOpenAIChatCompletion(modelId: modelId,
                                            endpoint: endpoint,
                                            apiKey: apiKey,
                                            httpClient: customHttpClient);

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

            var genConfig = aiConfig.GenerationConfiguration != null ? aiConfig.GenerationConfiguration : new LLMGenerationConfiguration();

            var random = new Random();

            var settings = new OpenAIPromptExecutionSettings
            {
                // Defaults.
                Temperature = (double)genConfig.Temperature,
                MaxTokens = genConfig.MaxTokens,
                TopP = (double?)genConfig.TopP,
                FrequencyPenalty = (double?)genConfig.PenaltyFrequency,
                PresencePenalty = (double?)genConfig.PenaltyPresence,
                Seed = genConfig.Seed == -1 ? random.Next(9000) : (long)genConfig.Seed,
            };

            if (useKernelPlugins)
            {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//                settings.ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions;
                settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true });

            }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // Convert conversation to Semantic Kernel format
            var chatHistory = new MSK.ChatCompletion.ChatHistory();
            



            foreach (var message in conversation.Messages)
            {
                chatHistory.AddMessage(
                    message.Role switch
                    {
                        "system" => MSK.ChatCompletion.AuthorRole.System,
                        "user" => MSK.ChatCompletion.AuthorRole.User,
                        "assistant" => MSK.ChatCompletion.AuthorRole.Assistant,
                        _ => throw new ArgumentException($"Invalid role: {message.Role}")
                    },
                    message.Content
                );
                if (message.Role == "system")
                {
                    #region Handle attachments, if any
                    if (attachments != null && attachments.Any())
                    {
                        List<MSK.TextContent> txtAttachments = new List<TextContent>();
                        foreach (var attachment in attachments)
                        {
                            var txtContent = attachment.Text; // HEAVY This will trigger the parser to extract contents if its not already parsed.
                            var fileName = Path.GetFileName(attachment.FileName);
                            Dictionary<string, object?> metaData = new Dictionary<string, object?>();
                            metaData.Add("Filename", Path.GetFileName(fileName));
                            TextContent content = new TextContent { Text = txtContent, Metadata = metaData };
                            txtAttachments.Add(content);
                        }

                        var attachmentCollection = new ChatMessageContentItemCollection();
                        foreach (var attachment in txtAttachments)
                        {
                            attachmentCollection.Add(attachment);
                        }
                        foreach (var attachment in txtAttachments)
                        {
                            chatHistory.Add(new ChatMessageContent(MSK.ChatCompletion.AuthorRole.System, attachmentCollection));
                        }
                    }
                    #endregion
                }
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

            MSK.ChatCompletion.AuthorRole? lastRole = null;
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

        public static async IAsyncEnumerable<string> GetCompletionViaLocalLLM(
            LocalLLMConfiguration llmConfig, 
            Conversation conversation,
            IInferenceParams inferenceParams,
            CancellationToken cancellationToken = default,
            LoadedLocalLLM llm = null)
        {
            #region Some basic validation...
            if (string.IsNullOrEmpty(llmConfig.ModelPath) || !File.Exists(llmConfig.ModelPath))
            {
                throw new ArgumentException("Invalid or missing model path.", nameof(llmConfig.ModelPath));
            }
            if (llmConfig.ContextSize <= 0)
            {
                throw new ArgumentException("Context size must be positive.", nameof(llmConfig.ContextSize));
            }
            #endregion

            ILogger? logger = null;
#if DEBUG
            logger = new MSLogger(new DebugLogger());
#endif
            if (llm == null)
            {
                llm = await Program.GetOrLoadLocalLLM(llmConfig, cancellationToken);
            }
            var executor = new InteractiveExecutor(llm.GetNewContext(),logger);
            var chatHistory = new LLama.Common.ChatHistory();
            for (int i = 0; i < conversation.Messages.Count - 1; i++)
            {
                var msg = conversation.Messages[i];
                LLama.Common.AuthorRole role = msg.Role switch
                {
                    "system" =>     LLama.Common.AuthorRole.System,
                    "user" =>       LLama.Common.AuthorRole.User,
                    "assistant" =>  LLama.Common.AuthorRole.Assistant,
                    _ => throw new ArgumentException($"Invalid role: {msg.Role}")
                };
                chatHistory.AddMessage(role, msg.Content);
            }

            var lastMsg = conversation.Messages.Last();
            var userMessage = new LLama.Common.ChatHistory.Message(LLama.Common.AuthorRole.User, lastMsg.Content);

            // Create chat session with history
            var session = new ChatSession(executor, chatHistory);
            session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
            new string[] { "User:", "Assistant:", "System:" },
            redundancyLength: 8));

            var temperature = 0.7f;
            int chars = 0;
            int maxRetries = 5;
            int retries = 0;
            

        retryPoint:
            // Stream the response tokens
            
            await foreach (var token in session.ChatAsync(userMessage, inferenceParams, cancellationToken))
            {
                chars += token?.Length ?? 0;
                yield return token ?? "";
            }
            if (chars == 0 && retries < maxRetries)
            {
                temperature -= 0.1f;
                ++retries;
                goto retryPoint;
            }
            if (retries >= maxRetries && chars == 0)
            {
                yield return S.Get("Sorry, the model didn't generate a response. Try rephrasing your query.");
            }
        }


        public static async IAsyncEnumerable<string> GetChatCompletionViaLocalLLM(
            LoadedLocalLLM llm,
            ChatCompletionsRequest chatCompletionsRequest,
            CancellationToken cancellationToken = default,
            ILogger logger = null
        )
        {
            // Extract key params from request
            var temperature = chatCompletionsRequest.Temperature ?? 0.7f;
            var maxTokens = chatCompletionsRequest.MaxTokens ?? 512;
            bool useTools = (chatCompletionsRequest.Tools != null && chatCompletionsRequest.Tools.Count > 0) ||
                            (chatCompletionsRequest.McpServers != null && chatCompletionsRequest.McpServers.Count > 0);

            // Build Semantic Kernel
            var builder = Kernel.CreateBuilder();
            var executor = new InteractiveExecutor(llm.GetNewContext(), logger);
            var chatCompletionService = new LLamaSharpChatCompletion(
                executor,
                new LLamaSharpPromptExecutionSettings
                {
                    MaxTokens = maxTokens,
                    Temperature = temperature,
                    StopSequences = chatCompletionsRequest.Stop ?? new List<string>()
                }
            );
            builder.Services.AddSingleton<IChatCompletionService>(chatCompletionService);

            var kernel = builder.Build();

            // If tools/MCP present, register them
            if (useTools)
            {
                // MCP servers
                foreach (var mcpUrl in chatCompletionsRequest.McpServers ?? new List<string>())
                {
                    string? connectionError = null;
                    try
                    {
                        var transportOptions = new SseClientTransportOptions
                        {
                            Endpoint = new Uri(mcpUrl), // Required: HTTP/HTTPS URI
                            ConnectionTimeout = TimeSpan.FromSeconds(30), // Default
                            UseStreamableHttp = false, // Use standard SSE (per spec)
                            Name = $"McpClient_{mcpUrl.GetHashCode()}", // Optional: for logging
                            AdditionalHeaders = null // Optional: add auth headers if needed
                        };
                        var transport = new SseClientTransport(transportOptions);
                        var mcpClient = await McpClientFactory.CreateAsync(transport, null, null, cancellationToken);

                        var tools = await mcpClient.ListToolsAsync();
#pragma warning disable SKEXP0001 // Experimental API
                        var kernelFunctions = tools.Select(t => t.AsKernelFunction()).ToList();
#pragma warning restore SKEXP0001
                        if (kernelFunctions.Count > 0)
                        {
                            string pluginName = $"McpPlugin_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                            kernel.Plugins.AddFromFunctions(pluginName, kernelFunctions);
                            Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"Registered {kernelFunctions.Count} tools from MCP server {mcpUrl}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.ERROR, $"Failed to connect to MCP server {mcpUrl}: {ex.Message}", ex);
                        connectionError = $"Error connecting to MCP server {mcpUrl}: {ex.Message}\n";
                    }
                    if (connectionError != null)
                    {
                        yield return connectionError;
                    }
                }

                // Static tools from request (if any; for hybrid)
                // Static tools from request
                if (chatCompletionsRequest.Tools != null && chatCompletionsRequest.Tools.Any())
                {
                    try
                    {
                        var kernelFunctions = new List<KernelFunction>();
                        foreach (var tool in chatCompletionsRequest.Tools)
                        {
                            if (tool.Type != "function" || tool.Function == null)
                            {
                                Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.WARNING, $"Skipping invalid tool: type={tool.Type}, function={tool.Function}");
                                continue;
                            }

                            var function = tool.Function;
                            var functionName = function.Name;
                            if (string.IsNullOrEmpty(functionName))
                            {
                                Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.WARNING, "Skipping tool with empty function name");
                                continue;
                            }

                            var description = function.Description ?? $"Function {functionName}";
                            var parametersSchema = function.Parameters.HasValue ? function.Parameters.Value : JsonDocument.Parse("{\"type\":\"object\",\"properties\":{}}").RootElement;

                            // Parse JSON Schema parameters into KernelParameterMetadata
                            var parameters = new List<KernelParameterMetadata>();
                            if (parametersSchema.ValueKind == JsonValueKind.Object &&
                                parametersSchema.TryGetProperty("properties", out var properties))
                            {
                                foreach (var prop in properties.EnumerateObject())
                                {
                                    var paramName = prop.Name;
                                    var paramType = typeof(string); // Default to string
                                    string paramDescription = "No description";
                                    bool isRequired = false;

                                    if (prop.Value.TryGetProperty("type", out var typeElement))
                                    {
                                        paramType = typeElement.GetString() switch
                                        {
                                            "string" => typeof(string),
                                            "number" => typeof(double),
                                            "integer" => typeof(int),
                                            "boolean" => typeof(bool),
                                            "array" => typeof(List<object>),
                                            "object" => typeof(Dictionary<string, object>),
                                            _ => typeof(string)
                                        };
                                    }

                                    if (prop.Value.TryGetProperty("description", out var descElement))
                                    {
                                        paramDescription = descElement.GetString() ?? paramDescription;
                                    }

                                    if (parametersSchema.TryGetProperty("required", out var requiredElement) &&
                                        requiredElement.EnumerateArray().Any(e => e.GetString() == paramName))
                                    {
                                        isRequired = true;
                                    }

                                    parameters.Add(new KernelParameterMetadata(paramName)
                                    {
                                        Description = paramDescription,
                                        ParameterType = paramType,
                                        IsRequired = isRequired
                                    });
                                }
                            }

                            // Create KernelFunction with no-execution logic (since static tools are client-executed; throw if unexpectedly invoked)
                            var kernelFunction = KernelFunctionFactory.CreateFromMethod(
                                method: (Kernel kernel, KernelArguments arguments) =>
                                {
                                    throw new NotSupportedException("Static tools are intended for client-side execution and cannot be invoked on the server.");
                                },
                                functionName: functionName,
                                description: description,
                                parameters: parameters,
                                returnParameter: new KernelReturnParameterMetadata
                                {
                                    Description = "Result of the client-executed tool"
                                }
                            );

                            kernelFunctions.Add(kernelFunction);
                        }

                        if (kernelFunctions.Any())
                        {
                            string pluginName = $"StaticToolPlugin_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                            kernel.Plugins.AddFromFunctions(pluginName, kernelFunctions);
                            Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"Registered {kernelFunctions.Count} static tools from request");
                        }
                        else
                        {
                            Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.WARNING, "No valid tools registered from request");
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"Failed to process static tools: {ex.Message}\n";
                        Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.ERROR, errorMessage, ex);
                        throw;
                    }
                }
            }

            // Build ChatHistory from request messages
            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            foreach (var msg in chatCompletionsRequest.Messages ?? new List<ChatMessage>())
            {
                chatHistory.AddMessage(
                    msg.Role switch
                    {
                        "system" => MSK.ChatCompletion.AuthorRole.System,
                        "user" => MSK.ChatCompletion.AuthorRole.User,
                        "assistant" => MSK.ChatCompletion.AuthorRole.Assistant,
                        _ => throw new ArgumentException($"Invalid role: {msg.Role}")
                    },
                    msg.Content
                );
            }

            // Execution settings
            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = temperature,
                MaxTokens = maxTokens,
                ToolCallBehavior = useTools ? ToolCallBehavior.AutoInvokeKernelFunctions : null
            };

            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // Stream the response (yields tokens/chunks)
            await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(
                chatHistory, settings, kernel, cancellationToken))
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    yield return chunk.Content;  // Yield text tokens
                }
                // If you need to yield tool call info (e.g., for OpenAI-style deltas), check chunk.Metadata
                // e.g., if (chunk.Metadata?.ContainsKey("ToolCalls") == true) { yield special marker }
            }
        }

        private static IInferenceParams GetInferenceParamsBasedOnConfig(LLMGenerationConfiguration generationConfig)
        {
            System.Random random = new System.Random();
            var samplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = (float)generationConfig.Temperature,
                TopP = (float)generationConfig.TopP,
                TopK = generationConfig.TopK,
                RepeatPenalty = (float)generationConfig.PenaltyRepetition,
                FrequencyPenalty = (float)generationConfig.PenaltyFrequency,
                PresencePenalty = (float)generationConfig.PenaltyPresence,
                Seed = (generationConfig.Seed == -1) ? ((uint)random.Next()) : ((uint)generationConfig.Seed),
                MinP = (float)generationConfig.MinP
            };
            var inferenceParams = new InferenceParams
            {
                MaxTokens = generationConfig.MaxTokens,
                AntiPrompts = new List<string> { "User:", "\nUser:" }, // Stop at the next user prompt
                SamplingPipeline = samplingPipeline
            };
            return inferenceParams;
        }

        public static async IAsyncEnumerable<string> GetCompletionViaLocalLLMForPrompt(
            LoadedLocalLLM llm,
            string prompt,
            float temperature = 0.7f,
            int maxTokens = 512,
            IReadOnlyList<string>? antiPrompts = null,
            CancellationToken cancellationToken = default
        )
        {


            ILogger? logger = null;
#if DEBUG
            logger = new MSLogger(new DebugLogger());
#endif
            var context = llm.GetNewContext();
            // Use StatelessExecutor for pure completion (prompt continuation)
            var executor = new StatelessExecutor(llm.weights!, context.Params); // Assumes weights are accessible; adjust if private.

            // Tokenize the prompt
            var tokens = context.Tokenize(prompt);

            int chars = 0;
            int maxRetries = 5;
            int retries = 0;
            Random random = new Random();

        retryPoint:
            uint seed = (uint)random.Next();
            var samplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = temperature,
                Seed = seed,
            };

            // Inference parameters
            var inferenceParams = new InferenceParams
            {
                MaxTokens = maxTokens,
                AntiPrompts = antiPrompts ?? new List<string> { },
                SamplingPipeline = samplingPipeline
            };

            // Stream the response tokens
            await foreach (var token in executor.InferAsync(prompt, inferenceParams, cancellationToken))
            {
                //var text = executor.Context.Detokenize(new[] { token });
                chars += token.Length;
                yield return token;
            }

            if (chars == 0 && retries < maxRetries)
            {
                temperature -= 0.1f;
                ++retries;
                goto retryPoint;
            }
            if (retries >= maxRetries && chars == 0)
            {
                yield return S.Get("Sorry, the model didn't generate a response. Try rephrasing your query.");
            }
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
