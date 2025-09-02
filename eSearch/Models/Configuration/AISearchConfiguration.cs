using eSearch.Models.AI;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eSearch.Models.Configuration
{
    public class AISearchConfiguration
    {
        public override string ToString()
        {
            return GetDisplayName();
        }

        public string Id
        {
            get
            {
                if (_id == null)
                {
                    _id = Guid.NewGuid().ToString();
                }
                return _id;
            }
            set
            {
                _id = value;
            }

        }

        private string? _id = null;

        public string CustomDisplayName { get; set; } = string.Empty;


        public string GetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(CustomDisplayName))
            {
                return CustomDisplayName.Trim();
            }

            switch(LLMService)
            {
                case LLMService.Perplexity:
                    return "Perplexity";
                case LLMService.ChatGPT:
                    return "OpenAI";
                case LLMService.Custom:
                    try
                    {
                        return new System.Uri(ServerURL).Host;
                    } catch (Exception ex)
                    {
                        return ServerURL;
                    }
            }
            return "???"; // Should be impossible.
        }

        public bool IsConfigured()
        {
            if (LLMService == LLMService.Perplexity)
            {
                // Return false if the model is obsolete.
                if (this.PerplexityModel.IsObsolete())
                {
                    return false;
                }
            }
            if (APIKey != string.Empty || ServerURL != string.Empty) { return true; }
            return false;
        }

        public LLMService LLMService { get; set; } = LLMService.Perplexity;

        public LocalLLMConfiguration? LocalLLMConfiguration { get; set; } = null;

    #region The following settings are only to be used if LocalLLMConfiguration is null, otherwise they should be ignored.
        public string ServerURL { get; set; } = string.Empty;

        public string APIKey { get; set; } = string.Empty;

        public PerplexityModel PerplexityModel { get; set; } = PerplexityModel.Sonar;

        public string Model { get; set; } = string.Empty;

    #endregion

        /// <summary>
        /// The role to use when providing system prompts to LLM. Might be 'System' or 'Developer'
        /// </summary>
        public string SystemPromptRole { get; set; } = "System";

        /// <summary>
        /// May be null. When not null, overrides the System Prompt.
        /// </summary>
        public string? CustomSystemPrompt = null;

        


        /// <summary>
        /// Test for success response. Will attempt to call the API.
        /// </summary>
        /// <returns>true if success, false otherwise with an error string</returns>
        public async Task<Tuple<bool,string>> Test()
        {
            try
            {
                Debug.WriteLine("Testing configuration...");
                Conversation testConvo = new Conversation();
                testConvo.Messages.Add(new Message { Content = "You are a helpful assistant", Role = "system", Model = ""});
                testConvo.Messages.Add(new Message { Content = "User: Testing the system. Respond with a quick and short message", Role = "user", Model = "" });

                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

                await foreach(var str in Completions.GetCompletionStreamViaMCPAsync(this, testConvo, cts.Token))
                {
                    cts.Cancel();
                    return new Tuple<bool, string>(true, "success");
                }
                throw new Exception("Empty Response Message?");

            } catch (Exception ex)
            {
                Debug.WriteLine($"An exception ocurred: {ex.ToString()}");
                return new Tuple<bool, string>(false, ex.Message);
            }
        }
    }
}
