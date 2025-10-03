using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using eSearch.Models.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using S = eSearch.ViewModels.TranslationsViewModel;
using System.Diagnostics;
using org.openxmlformats.schemas.spreadsheetml.x2006.main;
using com.sun.tools.javac.parser;

namespace eSearch.Models.AI
{
    public class LocalLLMServer : IDisposable
    {
        private readonly int _port;
        private WebApplication? _app;
        private Task? _runTask;
        private LoadedLocalLLM? ServerLoadedLLM;

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
                    builder.WebHost.UseUrls(new string[] { $"http://localhost:{_port}" });
                    _app = builder.Build();

                    // Map the completions endpoint
                    _app.MapPost("/v1/completions", HandleCompletionsAsync);
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
                Program.LLMServerSessionLog.Log(Interop.ILogger.Severity.ERROR, $"{IPAddress}: {ex.Message}", ex);
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