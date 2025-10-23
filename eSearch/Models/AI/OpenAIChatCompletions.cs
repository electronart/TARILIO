
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace eSearch.Models.AI
{
    /// <summary>
    /// Based on /chat/completions specs
    /// </summary>
    public class OpenAIChatCompletions
    {
        public class ChatMessage
        {
            [JsonProperty("role")]
            public string Role { get; set; } = string.Empty; // "system", "user", "assistant"

            [JsonProperty("content")]
            public string Content { get; set; } = string.Empty; // Simplified: string only (no array/vision support yet)

            [JsonProperty("tool_calls")]
            public List<ToolCall>? ToolCalls { get; set; }
        }

        public class ToolCall  // Simple schema
        {
            [JsonProperty("id")]
            public string Id { get; set; } = Guid.NewGuid().ToString();
            [JsonProperty("type")]
            public string Type { get; set; } = "function";
            [JsonProperty("function")]
            public FunctionCall Function { get; set; } = new FunctionCall();
        }

        public class FunctionCall
        {
            [JsonProperty("name")]
            public string Name { get; set; } = string.Empty;
            [JsonProperty("arguments")]
            public string Arguments { get; set; } = string.Empty;  // JSON string
        }

        public class ChatCompletionsRequest
        {
            [JsonProperty("messages")]
            public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>(); // Required

            [JsonProperty("model")]
            public string Model { get; set; } = string.Empty; // Required

            [JsonProperty("max_tokens")]
            public int? MaxTokens { get; set; } = 512;

            [JsonProperty("temperature")]
            public float? Temperature { get; set; } = 0.7f;

            [JsonProperty("stream")]
            public bool? Stream { get; set; } = false;

            [JsonProperty("seed")]
            public uint Seed { get; set; } = (uint)new Random().Next(5000);

            [JsonProperty("stop")]
            public List<string> Stop { get; set; } = new List<string>();

            // Advanced (stubbed for future; not implemented)
            [JsonProperty("frequency_penalty")]
            public float? FrequencyPenalty { get; set; }

            [JsonProperty("presence_penalty")]
            public float? PresencePenalty { get; set; }

            [JsonProperty("top_p")]
            public float? TopP { get; set; }

            [JsonProperty("logprobs")]
            public bool? Logprobs { get; set; }

            [JsonProperty("top_logprobs")]
            public int? TopLogprobs { get; set; }

            [JsonProperty("logit_bias")]
            public Dictionary<string, float>? LogitBias { get; set; }

            [JsonProperty("n")]
            public int? N { get; set; } = 1; // Only n=1 supported

            [JsonProperty("user")]
            public string? User { get; set; }

            [JsonProperty("tools")]
            public List<Tool>? Tools { get; set; } // Stub

            [JsonProperty("tool_choice")]
            [Newtonsoft.Json.JsonConverter(typeof(ToolChoiceConverter))]
            public ToolChoice? ToolChoice { get; set; } // Stub


            [JsonProperty("mcp_servers")]  // New: Client-provided MCP server URLs (HTTP/SSE/STDIO)
            public List<string>? McpServers { get; set; } = null;
        }

        public class ChatCompletionsResponse
        {
            [JsonProperty("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("object")]
            public string Object { get; set; } = "chat.completion";

            [JsonProperty("created")]
            public int Created { get; set; }

            [JsonProperty("model")]
            public string Model { get; set; } = string.Empty;

            [JsonProperty("choices")]
            public List<ChatChoice> Choices { get; set; } = new List<ChatChoice>();

            [JsonProperty("usage")]
            public Usage? Usage { get; set; }
        }

        public class ChatChoice
        {
            [JsonProperty("index")]
            public int Index { get; set; }

            [JsonProperty("message")]
            public ChatMessage? Message { get; set; } // For non-stream

            [JsonProperty("delta")]
            public ChatDelta? Delta { get; set; } // For stream chunks

            [JsonProperty("finish_reason")]
            public string? FinishReason { get; set; }

            [JsonProperty("logprobs")]
            public object? Logprobs { get; set; } // Stub: null if not requested
        }

        public class ChatDelta
        {
            [JsonProperty("role")]
            public string? Role { get; set; } // Only in first chunk

            [JsonProperty("content")]
            public string Content { get; set; } = string.Empty;


            [JsonProperty("tool_calls")]  // Add for tool call deltas
            public List<ToolCall>? ToolCalls { get; set; }
        }

        public class Usage
        {
            [JsonProperty("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonProperty("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonProperty("total_tokens")]
            public int TotalTokens { get; set; }
        }

        // OpenAI Tool specification
        public class Tool
        {
            [JsonProperty("type")]
            public string Type { get; set; } = "function"; // "function" or "code_interpreter" (only function supported)

            [JsonProperty("function")]
            public Function? Function { get; set; }
        }

        // OpenAI Function specification (for tool functions)
        public class Function
        {
            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("description")]
            public string? Description { get; set; }

            [JsonProperty("parameters")]
            public JsonElement? Parameters { get; set; } // JSON Schema object

            // Optional: Strict mode for OpenAI function calling (newer models)
            [JsonProperty("strict")]
            public bool? Strict { get; set; }
        }

        // OpenAI ToolChoice specification
        public class ToolChoice
        {
            public string? Type { get; set; } // "auto", "none", or "function"
            public FunctionChoice? Function { get; set; }

            public static ToolChoice Auto => new ToolChoice { Type = "auto" };
            public static ToolChoice None => new ToolChoice { Type = "none" };
            public static ToolChoice FunctionChoiceFactory(string functionName) => new ToolChoice
            {
                Type = "function",
                Function = new FunctionChoice { Name = functionName }
            };
        }

        public class FunctionChoice
        {
            [JsonProperty("name")]
            public string? Name { get; set; }
        }

        // Helper classes for deserialization variants (if needed for advanced scenarios)
        // Note: These are optional and depend on your specific needs
        public class AutoToolChoice
        {
            [JsonProperty("type")]
            public string Type { get; set; } = "auto";
        }

        public class NoneToolChoice
        {
            [JsonProperty("type")]
            public string Type { get; set; } = "none";
        }

        public class SpecificToolChoice : ToolChoice
        {
            public SpecificToolChoice()
            {
                Type = "function";
            }
        }

        public class ToolChoiceConverter : Newtonsoft.Json.JsonConverter<ToolChoice>
        {
            public override ToolChoice ReadJson(JsonReader reader, Type objectType, ToolChoice existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    string value = reader.Value.ToString();
                    if (value == "auto")
                        return ToolChoice.Auto;
                    if (value == "none")
                        return ToolChoice.None;
                    throw new JsonSerializationException($"Invalid tool_choice string value: {value}");
                }

                JObject obj = JObject.Load(reader);
                string? type = obj["type"]?.ToString();

                if (string.IsNullOrEmpty(type))
                    throw new JsonSerializationException("ToolChoice object missing 'type' field");

                var toolChoice = new ToolChoice { Type = type };

                if (type == "function")
                {
                    if (obj["function"] != null)
                    {
                        toolChoice.Function = obj["function"].ToObject<FunctionChoice>(serializer);
                    }
                    else
                    {
                        throw new JsonSerializationException("ToolChoice with type 'function' missing 'function' field");
                    }
                }

                return toolChoice;
            }

            public override void WriteJson(JsonWriter writer, ToolChoice value, Newtonsoft.Json.JsonSerializer serializer)
            {
                if (value.Type == "auto" || value.Type == "none")
                {
                    writer.WriteValue(value.Type);
                }
                else
                {
                    JObject obj = new JObject
                    {
                        ["type"] = value.Type
                    };
                    if (value.Function != null)
                    {
                        obj["function"] = JObject.FromObject(value.Function);
                    }
                    obj.WriteTo(writer);
                }
            }
        }
    }
}
