using eSearch.Models.Configuration;
using LLama;
using LLama.Common;
using System;
using System.Collections.Generic;
using System.Linq;
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

        

        public required LocalLLMConfiguration llm;
        // null until loaded but we don't return from LoadLLM until loaded.
        ModelParams? modelParams = null;
        LLamaWeights? weights = null;

        public LLamaContext GetNewContext()
        {
            if (weights == null || modelParams == null) { throw new ArgumentException("Model not loaded"); }
            return weights.CreateContext(modelParams);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
