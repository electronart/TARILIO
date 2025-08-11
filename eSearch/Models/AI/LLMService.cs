using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Models.AI
{
    // Note - Add new enums to the bottom it seems to save the index when serialized instead of the value name...
    public enum LLMService
    {
        
        Perplexity,
        [Description("OpenAI")]
        ChatGPT,
        [Description("Custom...")]
        Custom,
        OpenRouter,
        Ollama,
        LMStudio,
    }
}