using DynamicData;
using eSearch.Models.Configuration;
using System.Collections.ObjectModel;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.ViewModels
{
    public class LLMGenerationParametersViewModel : ViewModelBase
    {
        public ObservableCollection<SliderPropertyViewModel> SliderProperties { get; set; } = new ObservableCollection<SliderPropertyViewModel>();

        public static LLMGenerationParametersViewModel FromConfiguration(LLMGenerationConfiguration config)
        {
            LLMGenerationParametersViewModel model = new LLMGenerationParametersViewModel();

            model.SliderProperties.Add(new SliderPropertyViewModel
            {
                Title = S.Get("Temperature"),
                Description = S.Get("Controls randomness - lower for more focused output, higher for creativity."),
                Value = config.Temperature,
                SoftMinValue = 0.1m,
                SoftMaxValue = 1.5m,
                MinValue = 0.0m,
                MaxValue = 2.0m,
                InternalPropertyName = nameof(config.Temperature)
            });

            // Max Tokens
            model.SliderProperties.Add(new SliderPropertyViewModel
            {
                Title = S.Get("Max Tokens"),
                Description = S.Get("Maximum number of tokens to generate."),
                Value = config.MaxTokens,
                SoftMinValue = 1m,
                SoftMaxValue = 512m,
                MinValue = 1m,
                MaxValue = 4096m,
                InternalPropertyName = nameof(config.MaxTokens)
            });

            // Top P
                model.SliderProperties.Add(new SliderPropertyViewModel
            {
                Title = S.Get("Top P"),
                Description = S.Get("Nucleus sampling—limits to tokens with cumulative probability above this value."),
                Value = config.TopP,
                SoftMinValue = 0.1m,
                SoftMaxValue = 0.95m,
                MinValue = 0.0m,
                MaxValue = 1.0m,
                InternalPropertyName = nameof(config.TopP)
            });

            // Advanced Parameters

            // Top K
            model.SliderProperties.Add(new SliderPropertyViewModel
            {
                Title = S.Get("Top K"),
                Description = S.Get("Limits sampling to the top K most likely tokens."),
                Value = config.TopK,
                SoftMinValue = 1m,
                SoftMaxValue = 100m,
                MinValue = 1m,
                MaxValue = 1000m,
                InternalPropertyName = nameof(config.TopK)
            });

            // Repeat Penalty
            model.SliderProperties.Add(new SliderPropertyViewModel
            {
                Title = S.Get("Repeat Penalty"),
                Description = S.Get("Penalizes repeated tokens to encourage diversity."),
                Value = config.PenaltyRepetition,
                SoftMinValue = 1.0m,
                SoftMaxValue = 2.0m,
                MinValue = 0.1m,
                MaxValue = 5.0m,
                InternalPropertyName = nameof(config.PenaltyRepetition)
            });

            // Repeat Last N
            model.SliderProperties.Add(new SliderPropertyViewModel
            {
                Title = S.Get("Repeat Last N"),
                Description = S.Get("Number of recent tokens to apply repeat penalty to."),
                Value = config.PenaltyRepetitionRange,
                SoftMinValue = 32m,
                SoftMaxValue = 512m,
                MinValue = 1m,
                MaxValue = 2048m,
                InternalPropertyName = nameof(config.PenaltyRepetitionRange)
            });

            // Presence Penalty
            model.SliderProperties.Add(new SliderPropertyViewModel
            {
                Title = S.Get("Presence Penalty"),
                Description = S.Get("Penalizes new tokens if they've appeared in the text so far."),
                Value = config.PenaltyPresence,
                SoftMinValue = 0.0m,
                SoftMaxValue = 1.0m,
                MinValue = -2.0m,
                MaxValue = 2.0m,
                InternalPropertyName = nameof(config.PenaltyPresence)
            });

            // Frequency Penalty
            model.SliderProperties.Add(new SliderPropertyViewModel
            {
                Title = S.Get("Frequency Penalty"),
                Description = S.Get("Penalizes tokens based on how often they've appeared."),
                Value = config.PenaltyFrequency,
                SoftMinValue = 0.0m,
                SoftMaxValue = 1.0m,
                MinValue = -2.0m,
                MaxValue = 2.0m,
                InternalPropertyName = nameof(config.PenaltyFrequency)
            });

            // Seed
            model.SliderProperties.Add(new SliderPropertyViewModel
            {
                Title = S.Get("Seed"),
                Description = S.Get("Random seed for reproducibility (-1 for random)."),
                Value = config.Seed,
                SoftMinValue = 0m,
                SoftMaxValue = 1000000m,
                MinValue = -1m,
                MaxValue = 2147483647m,
                InternalPropertyName = nameof(config.Seed)
            });

            // Min P
            model.SliderProperties.Add(new SliderPropertyViewModel
            {
                Title = S.Get("Min P"),
                Description = S.Get("Minimum probability threshold for tokens to be considered."),
                Value = config.MinP,
                SoftMinValue = 0.05m,
                SoftMaxValue = 0.5m,
                MinValue = 0.0m,
                MaxValue = 1.0m, 
                InternalPropertyName = nameof(config.MinP)
            });

            return model;
        }


        
    }

    public class DesignLLMGenerationParametersViewModel : LLMGenerationParametersViewModel
    {
        public DesignLLMGenerationParametersViewModel()
        {
            var vm = LLMGenerationParametersViewModel.FromConfiguration(new LLMGenerationConfiguration());
            this.SliderProperties.AddRange(vm.SliderProperties);
        }
    }
}
