using eSearch.Models.Configuration;
using eSearch.Models.Documents.Parse;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;


namespace eSearch.Models.AI
{
    public class CompletionStreamingJSBinding
    {

        private string? initialQuery;

        private AISearchConfiguration   aiSearchConfig;
        private CancellationTokenSource aiSearchCancellationTokenSource = new CancellationTokenSource();

        private string streamOutputBuff     = string.Empty;
        private int    currentCharIndex = 0;

        /// <summary>
        /// Null when the stream hasn't yet started. Check for null first.
        /// </summary>
        IAsyncEnumerable<string>? currentStream = null;

        private bool isFinishedStreaming = false;

        private string errorString = string.Empty;

        private Conversation conversation;
        

        public CompletionStreamingJSBinding(string initialQuery, AISearchConfiguration aiSearchConfig)
        {
            this.initialQuery = initialQuery;
            this.aiSearchConfig = aiSearchConfig;
        }

        public CompletionStreamingJSBinding(Conversation initialConversation, AISearchConfiguration aiSearchConfig)
        {
            this.initialQuery   = null;
            this.conversation   = initialConversation;
            this.aiSearchConfig = aiSearchConfig;
        }

        public void RequestCancel()
        {
            aiSearchCancellationTokenSource.Cancel();
            errorString = S.Get("Cancelled");
            isFinishedStreaming = true;
            currentStream = null;
        }

        public void StartNewCompletion()
        {
            if (currentStream != null)
            {
                throw new Exception("A completion stream is already in progress");
            }
            currentStream = Completions.GetCompletionStreamViaMCPAsync(aiSearchConfig, conversation, null, aiSearchCancellationTokenSource.Token);
            ReadStreamToOutputBufferAsync();
        }

        /// <summary>
        /// Will return empty string sometimes, does not necessarily indicate end of the stream, just that the next characters haven't loaded yet.
        /// Check IsFinishedStreaming to determine if it has finished streaming. Also check GetErrorString 
        /// </summary>
        /// <returns></returns>
        public string GetNextCharacters()
        {
            if (currentStream == null)
            {
                return string.Empty;
            }
            string returnStr = string.Empty;
            while ( currentCharIndex < streamOutputBuff.Length )
            {
                returnStr += streamOutputBuff[currentCharIndex];
                currentCharIndex++;
            }
            return returnStr;
        }

        public string GetErrorString()
        {
            return errorString;
        }

        public string Markdown2Html(string markdown)
        {
            try
            {
                return MarkDownParserMarkDig.ToHtml(markdown);
            }
            catch (Exception ex)
            {
                return markdown;
            }
        }

        private async void ReadStreamToOutputBufferAsync()
        {
            try
            {
                if (currentStream != null)
                {
                    await foreach (var str in currentStream)
                    {
                        if (str != null)
                        {
                            streamOutputBuff += str;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO Error handling.
                errorString = ex.ToString();
                Debug.WriteLine(errorString);
            } finally
            {
                isFinishedStreaming = true;
                currentStream = null;
            }
        }

        public bool IsFinishedStreaming()
        {
            return isFinishedStreaming;
        }
    }
}
