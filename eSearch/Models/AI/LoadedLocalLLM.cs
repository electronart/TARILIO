using eSearch.Models.Configuration;
using eSearch.Models.Logging;
using LLama;
using LLama.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eSearch.Models.AI
{
    public class LoadedLocalLLM : IDisposable
    {
        /// <summary>
        /// IMPORTANT
        /// Loading an LLM is computationally expensive/time consuming and should be done infrequently and avoid doing it at startup.
        /// </summary>
        /// <param name="configuration">LLM to load</param>
        /// <returns></returns>
        public static async Task<LoadedLocalLLM> LoadLLM(LocalLLMConfiguration llm, CancellationToken cancellationToken, IProgress<float> progressReporter)
        {
            var parameters = new ModelParams(llm.ModelPath)
            {
                ContextSize = llm.ContextSize
            };

            LoadedLocalLLM l = new LoadedLocalLLM
            {
                weights = await LLamaWeights.LoadFromFileAsync(parameters, cancellationToken, progressReporter),
                llm = llm,
                modelParams = parameters
            };
            return l;
        }

        public static List<string> GetAvailableModels()
        {
            List<string> models = new List<string>();
            // Detect available models...
            List<DirectoryInfo> directories = new List<DirectoryInfo>();
            directories.Add(new DirectoryInfo(Program.ESEARCH_LLM_MODELS_DIR));

            foreach (var directory in directories)
            {
                if (directory.Exists)
                {
                    foreach (var file in directory.GetFiles("*.gguf"))
                    {
                        models.Add(file.FullName);
                    }
                }
            }
            return models;
        }

        

        public required LocalLLMConfiguration llm;
        // null until loaded but we don't return from LoadLLM until loaded.
        ModelParams? modelParams = null;
        public LLamaWeights? weights = null;

        [HandleProcessCorruptedStateExceptions]
        public LLamaContext GetNewContext()
        {
            if (weights == null || modelParams == null) { throw new ArgumentException("Model not loaded"); }
            try
            {
                MSLogger logger = new MSLogger(new DebugLogger());
                return weights.CreateContext(modelParams, logger);
            } catch (Exception ex)
            {
                Debug.WriteLine($"Caught in GetNewContext: {ex.ToString()}");
                throw;  // Re-throw to let outer catch handle
            }
        }

        public void Dispose()
        {
            weights?.Dispose();
            weights = null;
        }
    }
}
