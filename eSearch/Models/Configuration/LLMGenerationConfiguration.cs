using eSearch.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Configuration
{
    public class LLMGenerationConfiguration
    {
        #region Basic Settings
        public decimal Temperature = 1.0m;

        public int MaxTokens = 1024;

        public decimal TopP = 1.0m;
        #endregion
        #region Advanced Settings
        public int TopK = 30;

        public decimal PenaltyRepetition = 1;

        public decimal PenaltyRepetitionRange = 1024;

        public decimal PenaltyPresence = 0.0m;

        public decimal PenaltyFrequency = 0.0m;

        public decimal Seed = -1;

        public decimal MinP = 0.2m;
        #endregion

        public static LLMGenerationConfiguration FromViewModel(LLMGenerationParametersViewModel viewModel)
        {
            LLMGenerationConfiguration config = new LLMGenerationConfiguration
            {
                Temperature = viewModel.SliderProperties.First(s => s.InternalPropertyName == nameof(Temperature)).Value,
                TopP = (decimal)viewModel.SliderProperties.First(s => s.InternalPropertyName == nameof(TopP)).Value,
                TopK = (int)viewModel.SliderProperties.First(s => s.InternalPropertyName == nameof(TopK)).Value,
                PenaltyRepetition = viewModel.SliderProperties.First(s => s.InternalPropertyName == nameof(PenaltyRepetition)).Value,
                PenaltyPresence = viewModel.SliderProperties.First(s => s.InternalPropertyName == nameof(PenaltyPresence)).Value,
                Seed = viewModel.SliderProperties.First(s => s.InternalPropertyName == nameof(Seed)).Value,
                MinP = viewModel.SliderProperties.First(s => s.InternalPropertyName == nameof(MinP)).Value,
                PenaltyRepetitionRange = viewModel.SliderProperties.First(s => s.InternalPropertyName == nameof(PenaltyRepetitionRange)).Value,
                MaxTokens = (int)viewModel.SliderProperties.First(s => s.InternalPropertyName == nameof(MaxTokens)).Value,
            };
            return config;
        }
    }
}
