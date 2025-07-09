using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Models.AI
{
    public enum LLMService
    {
        Perplexity,
        [Description("OpenAI")]
        ChatGPT,
        [Description("Custom...")]
        Custom
    }
}