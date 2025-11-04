using eSearch.Models.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static eSearch.Models.AI.OpenAIChatCompletions;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Models.AI
{
    public class LocalLLMServer : IDisposable
    {
        private readonly int _port;
        private WebApplication? _app;
        private Task? _runTask;
        private LoadedLocalLLM? ServerLoadedLLM;
        private readonly object _llmLock = new object(); // Synchronization lock

        public LocalLLMServer(int port = 5000)
        {
            _port = port;
        }

        public async Task<bool> StartAsync()
        {
            bool success = false;
            await Task.Run(() =>
            {
                try
                {
                    var builder = WebApplication.CreateBuilder();
                    builder.WebHost.UseUrls(new string[] { $"http://0.0.0.0:{_port}" });
                    _app = builder.Build();

                    // Map the completions endpoint
                    _app.MapPost("/v1/completions", HandleCompletionsAsync);
                    _app.MapPost("/v1/chat/completions", HandleChatCompletionsAsync);
                    _app.MapGet("/", HandleRootGetRequest);
                    _runTask = _app.RunAsync();
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, String.Format(S.Get("Server Started. Listening on Port {0}"), _port));
                    success = true;
                } catch (Exception ex)
                {
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.ERROR, "Could not start Server.", ex);
                }
            });
            return success;
            
        }

        public async Task StopAsync()
        {
            if (_app != null)
            {
                await _app.StopAsync();
                _app = null;
                _runTask = null;
                Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, S.Get("Server Stopped"));
            }
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        private async Task HandleRootGetRequest(HttpContext context)
        {
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("eSearch Server is running");
            return;
        }

        private async Task HandleCompletionsAsync(HttpContext context)
        {
            string IPAddress = context.Connection.RemoteIpAddress?.ToString() ?? "UNKOWN/LOCAL";
            // Check for X-Forwarded-For header if behind a proxy
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                IPAddress = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ?? "UNKNOWN";
            }
            try
            {
                // Parse request body
                string requestBody;
                using (var reader = new StreamReader(context.Request.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                var request = JsonConvert.DeserializeObject<CompletionsRequest>(requestBody);

                if (request == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid/Malformed Request");
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: 400 Invalid/Malformed Request");
                    return;
                }

                // Validate basics (add more as needed)
                if (string.IsNullOrEmpty(request.Prompt))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Missing 'prompt'");
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: 400 Missing 'prompt'");
                    return;
                }

                if (string.IsNullOrEmpty(request.Model))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Missing 'model'");
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: 400 Missing 'model'");
                    return;
                }

                #region Check the requested model is available
                List<string> availableModels = LoadedLocalLLM.GetAvailableModels();
                string? model = availableModels.FirstOrDefault(m => Path.GetFileNameWithoutExtension(m) == (request.Model ?? "$$$$$$$$"));
                if (model == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync($"Unrecognized Model: '{request.Model}'");
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: 400 Unrecognized Model: '{request.Model}'");
                    return;
                }
                #endregion

                var temperature = request.Temperature ?? 0.7f;
                var maxTokens = request.MaxTokens ?? 512;

                #region Handle Model Loading
                lock (_llmLock)
                {
                    if (ServerLoadedLLM != null)
                    {
                        if (ServerLoadedLLM.llm.ModelPath != model)
                        {
                            ServerLoadedLLM.Dispose();
                            ServerLoadedLLM = null;
                        }
                        else
                        {
                            ServerLoadedLLM.llm.Seed = request.Seed;
                        }
                    }
                }


                if (ServerLoadedLLM == null)
                {
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: Loading Model {request.Model}...");
                    Stopwatch sw = Stopwatch.StartNew();
                    LocalLLMConfiguration config = new LocalLLMConfiguration
                    {
                        ContextSize = 4096,
                        ModelPath = model,
                        Seed = request.Seed
                    };
                    Progress<float> progress = new Progress<float>();
                    progress.ProgressChanged += (sender, progress) =>
                    {
                        // TODO Logging
                    };
                    ServerLoadedLLM = await LoadedLocalLLM.LoadLLM(config, context.RequestAborted, progress);
                    sw.Stop();
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{(int)sw.Elapsed.TotalMilliseconds} ms spent loading model");
                }
                #endregion

                

                if (request.Stream == true)
                {
                    // Streaming response (SSE)
                    context.Response.ContentType = "text/event-stream";
                    context.Response.Headers.CacheControl = "no-cache";
                    context.Response.Headers.Connection = "keep-alive";

                    string id = Guid.NewGuid().ToString();
                    int index = 0;


                    Stopwatch sw = Stopwatch.StartNew();
                    int tokens = 0;
                    await foreach (var token in Completions.GetCompletionViaLocalLLMForPrompt(
                        ServerLoadedLLM, request.Prompt, temperature, maxTokens, request.Stop, context.RequestAborted))
                    {
                        ++tokens;
                        var chunk = new CompletionsResponse
                        {
                            Id = id,
                            Object = "text_completion.chunk",
                            Created = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            Model = request.Model,
                            Choices = new List<Choice>
                    {
                        new Choice { Index = index++, Text = token, FinishReason = null }
                    }
                        };

                        await context.Response.WriteAsync($"data: {JsonConvert.SerializeObject(chunk)}\n\n");
                        await context.Response.Body.FlushAsync();
                    }

                    // Final chunk
                    var finalChunk = new CompletionsResponse
                    {
                        Id = id,
                        Object = "text_completion.chunk",
                        Created = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        Model = request.Model,
                        Choices = new List<Choice>
                    {
                        new Choice { Index = index, Text = "", FinishReason = "stop" }
                    }
                    };
                    await context.Response.WriteAsync($"data: {JsonConvert.SerializeObject(finalChunk)}\n\n");
                    await context.Response.WriteAsync("data: [DONE]\n\n");
                    sw.Stop();
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: {tokens} tokens generated (streaming). Response took {(int)sw.Elapsed.TotalMilliseconds} ms");
                }
                else
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    // Non-streaming: Collect all tokens
                    var sb = new System.Text.StringBuilder();
                    int tokens = 0;
                    await foreach (var token in Completions.GetCompletionViaLocalLLMForPrompt(
                        ServerLoadedLLM, request.Prompt, temperature, maxTokens, request.Stop, context.RequestAborted))
                    {
                        ++tokens;
                        sb.Append(token);
                    }

                    var response = new CompletionsResponse
                    {
                        Id = Guid.NewGuid().ToString(),
                        Object = "text_completion",
                        Created = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        Model = request.Model,
                        Choices = new List<Choice>
                    {
                        new Choice { Index = 0, Text = sb.ToString(), FinishReason = "stop" }
                    }
                    };

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
                    sw.Stop();
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: {tokens} tokens generated (non-streaming). Response took {sw.Elapsed.TotalMilliseconds} ms");
                }
            } catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Internal Error");
#if DEBUG
                Debug.WriteLine($"Exception handling request: {ex.ToString()}");
#endif
                Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.ERROR, $"{IPAddress}: {ex.Message}", ex);
            }
        }

        private async Task HandleChatCompletionsAsync(HttpContext context)
        {
            string IPAddress = context.Connection.RemoteIpAddress?.ToString() ?? "UNKNOWN/LOCAL";
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                IPAddress = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ?? "UNKNOWN";
            }

            try
            {
                // Parse request body
                string requestBody;
                using (var reader = new StreamReader(context.Request.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                var request = JsonConvert.DeserializeObject<ChatCompletionsRequest>(requestBody);

                if (request == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid/Malformed Request");
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: 400 Invalid/Malformed Request (chat)");
                    return;
                }

                // Basic validation
                if (string.IsNullOrEmpty(request.Model))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Missing 'model'");
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: 400 Missing 'model' (chat)");
                    return;
                }

                if (request.Messages == null || request.Messages.Count == 0)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Missing or empty 'messages'");
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: 400 Missing/empty 'messages' (chat)");
                    return;
                }

                // Validate roles (system/user/assistant only)
                var validRoles = new HashSet<string> { "system", "user", "assistant" };
                foreach (var msg in request.Messages)
                {
                    if (string.IsNullOrEmpty(msg.Role) || !validRoles.Contains(msg.Role.ToLower()))
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync($"Invalid role in messages: '{msg.Role}'");
                        Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: 400 Invalid role '{msg.Role}' (chat)");
                        return;
                    }
                }

                bool useMCP = request.McpServers != null && request.McpServers.Count > 0;

                if (request.Logprobs == true || request.TopLogprobs > 0)
                {
                    context.Response.StatusCode = 501;
                    await context.Response.WriteAsync("Logprobs not implemented");
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: 501 Logprobs not implemented (chat)");
                    return;
                }

                if (request.N > 1)
                {
                    context.Response.StatusCode = 501;
                    await context.Response.WriteAsync("n > 1 not implemented");
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: 501 n > 1 not implemented (chat)");
                    return;
                }

                // Model availability check (reuse from completions)
                List<string> availableModels = LoadedLocalLLM.GetAvailableModels();
                string? modelPath = availableModels.FirstOrDefault(m => Path.GetFileNameWithoutExtension(m) == request.Model);
                if (modelPath == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync($"Unrecognized Model: '{request.Model}'");
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: 400 Unrecognized Model: '{request.Model}' (chat)");
                    return;
                }

                //var temperature = request.Temperature ?? 0.7f;
                //var maxTokens = request.MaxTokens ?? 512;

                // Handle model loading (reuse lock and logic)
                lock (_llmLock)
                {
                    if (ServerLoadedLLM != null)
                    {
                        if (ServerLoadedLLM.llm.ModelPath != modelPath)
                        {
                            ServerLoadedLLM.Dispose();
                            ServerLoadedLLM = null;
                        }
                        else
                        {
                            ServerLoadedLLM.llm.Seed = request.Seed;
                        }
                    }
                }

                if (ServerLoadedLLM == null)
                {
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: Loading Model {request.Model} (chat)...");
                    Stopwatch sw = Stopwatch.StartNew();
                    LocalLLMConfiguration config = new LocalLLMConfiguration
                    {
                        ContextSize = 4096, // Or make configurable
                        ModelPath = modelPath,
                        Seed = request.Seed
                    };
                    Progress<float> progress = new Progress<float>();
                    progress.ProgressChanged += (sender, prog) => { /* Logging if needed */ };
                    ServerLoadedLLM = await LoadedLocalLLM.LoadLLM(config, context.RequestAborted, progress);
                    sw.Stop();
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{(int)sw.Elapsed.TotalMilliseconds} ms spent loading model (chat)");
                }

                var tokenStream = Completions.GetChatCompletionViaLocalLLM(ServerLoadedLLM, request, context.RequestAborted);

                if (request.Stream == true)
                {
                    // Streaming (SSE)
                    context.Response.ContentType = "text/event-stream";
                    context.Response.Headers.CacheControl = "no-cache";
                    context.Response.Headers.Connection = "keep-alive";

                    string id = Guid.NewGuid().ToString();
                    bool firstChunk = true;
                    int promptTokens = 0; // Estimate: Sum message lengths / 4 (avg token len); improve with LLama tokenizer
                    foreach (var msg in request.Messages) promptTokens += msg.Content.Length / 4;
                    int completionTokens = 0;

                    Stopwatch sw = Stopwatch.StartNew();


                    // Generate completion ID and timestamp
                    string completionId = $"chatcmpl-{Guid.NewGuid().ToString("N")}";
                    long createdTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    string modelName = request.Model;

                    await foreach (var token in tokenStream)
                    {
                        ++completionTokens;

                        var delta = new ChatDelta
                        {
                            Content = token,
                            Role = firstChunk ? "assistant" : null
                        };
                        firstChunk = false;

                        var chunk = new ChatCompletionsResponse
                        {
                            Id = completionId,
                            Object = "chat.completion.chunk",
                            Created = (int)createdTimestamp,
                            Model = modelName,
                            Choices = new List<ChatChoice>
                            {
                                new ChatChoice
                                {
                                    Index = 0,
                                    Delta = delta,
                                    FinishReason = null
                                }
                            },
                            Usage = null
                        };
                        await context.Response.WriteAsync($"data: {JsonConvert.SerializeObject(chunk)}\n\n");
                        await context.Response.Body.FlushAsync();
                    }

                    // Final chunk
                    var finalChunk = new ChatCompletionsResponse
                    {
                        Id = id,
                        Object = "chat.completion.chunk",
                        Created = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        Model = request.Model,
                        Choices = new List<ChatChoice>
                        {
                            new ChatChoice { Index = 0, Delta = new ChatDelta { Content = "" }, FinishReason = "stop", Logprobs = null }
                        }
                    };
                    await context.Response.WriteAsync($"data: {JsonConvert.SerializeObject(finalChunk)}\n\n");
                    await context.Response.WriteAsync("data: [DONE]\n\n");
                    await context.Response.Body.FlushAsync();

                    sw.Stop();
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: {completionTokens} tokens generated (chat streaming). Response took {(int)sw.Elapsed.TotalMilliseconds} ms");
                }
                else
                {
                    // Non-streaming
                    Stopwatch sw = Stopwatch.StartNew();
                    var sb = new System.Text.StringBuilder();
                    int promptTokens = 0;
                    foreach (var msg in request.Messages) promptTokens += msg.Content.Length / 4;
                    int completionTokens = 0;

                    await foreach (var token in tokenStream)
                    {
                        completionTokens += 1;
                        sb.Append(token);
                    }

                    var response = new ChatCompletionsResponse
                    {
                        Id = Guid.NewGuid().ToString(),
                        Object = "chat.completion",
                        Created = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        Model = request.Model,
                        Choices = new List<ChatChoice>
                        {
                            new ChatChoice
                            {
                                Index = 0,
                                Message = new ChatMessage { Role = "assistant", Content = sb.ToString() },
                                FinishReason = "stop",
                                Logprobs = null
                            }
                        },
                        Usage = new Usage
                        {
                            PromptTokens = promptTokens,
                            CompletionTokens = completionTokens,
                            TotalTokens = promptTokens + completionTokens
                        }
                    };

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
                    sw.Stop();
                    Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.INFO, $"{IPAddress}: {completionTokens} tokens generated (chat non-streaming). Response took {(int)sw.Elapsed.TotalMilliseconds} ms");
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Internal Error");
#if DEBUG
                Debug.WriteLine($"Exception handling chat request: {ex}");
#endif
                Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.ERROR, $"{IPAddress}: {ex.Message} (chat)", ex);
            }
        }

    }

    // Helper classes to match OpenAI schema (expand as needed)
    public class CompletionsRequest
    {
        [JsonProperty("prompt")]
        public string Prompt { get; set; } = string.Empty; // Or List<string> for multi

        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        [JsonProperty("max_tokens")]
        public int? MaxTokens { get; set; } = 512;

        [JsonProperty("temperature")]
        public float? Temperature { get; set; } = 1.0f;

        [JsonProperty("stream")]
        public bool? Stream { get; set; } = false;

        [JsonProperty("seed")]
        public uint Seed { get; set; } = (uint)new Random().Next(5000);

        [JsonProperty("user")]
        public string? User { get; set; } = null;

        [JsonProperty("stop")]
        public List<string> Stop { get; set; } = new List<string>();
    }

    public class CompletionsResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("object")]
        public string Object { get; set; } = string.Empty;

        [JsonProperty("created")]
        public int Created { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        [JsonProperty("choices")]
        public List<Choice> Choices { get; set; } = new List<Choice>();
    }

    public class Choice
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;

        [JsonProperty("finish_reason")]
        public string? FinishReason { get; set; }
    }
}