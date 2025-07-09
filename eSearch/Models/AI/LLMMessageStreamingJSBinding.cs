using eSearch.Models.Configuration;
using eSearch.Models.Documents.Parse;
using eSearch.ViewModels;
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
    public class LLMMessageStreamingJSBinding
    {

        private CancellationTokenSource aiSearchCancellationTokenSource = new CancellationTokenSource();

        private string streamOutputBuff     = string.Empty;
        private int    currentCharIndex = 0;

        /// <summary>
        /// Null when the stream hasn't yet started. Check for null first.
        /// </summary>
        IAsyncEnumerable<string>? currentStream = null;

        private bool isFinishedStreaming = false;

        private string errorString = string.Empty;
        

        public LLMMessageStreamingJSBinding(LLMMessageViewModel messageViewModel)
        {
            currentStream = messageViewModel.GetMessageStreamEnumerator();
            ReadStreamToOutputBufferAsync();
        }

        public void RequestCancel()
        {
            aiSearchCancellationTokenSource.Cancel();
            errorString = S.Get("Cancelled");
            isFinishedStreaming = true;
            currentStream = null;
        }

        /// <summary>
        /// Will return empty string sometimes, does not necessarily indicate end of the stream, just that the next characters haven't loaded yet.
        /// Check IsFinishedStreaming to determine if it has finished streaming. Also check GetErrorString 
        /// </summary>
        /// <returns></returns>
        public string GetNextCharacters()
        {
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
                } else
                {

                }
            }
            catch (OperationCanceledException)
            {
                streamOutputBuff += "\n\n" + S.Get("Generation Cancelled");
            }
            catch (Exception ex)
            {
                // TODO Error handling.
                errorString = ex.Message;
                Debug.WriteLine(ex.ToString());
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
