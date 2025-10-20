using DynamicData;
using eSearch.Models.Configuration;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
                MaxValue = 4.0m,
                InternalPropertyName = nameof(config.Temperature),
                FormatString = "0.00",
                Increment = 0.05d
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
                MaxValue = 16384,
                InternalPropertyName = nameof(config.MaxTokens),
                FormatString = "0",
                Increment = 32
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
                InternalPropertyName = nameof(config.TopP),
                FormatString = "0.00",
                Increment = 0.05,
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
                InternalPropertyName = nameof(config.TopK),
                FormatString = "0",
                Increment = 1
            });

            // Repeat Penalty
            model.SliderProperties.Add(new SliderPropertyViewModel
            {
                Title = S.Get("Repeat Penalty"),
                Description = S.Get("Penalizes repeated tokens to encourage diversity."),
                Value = config.PenaltyRepetition,
                SoftMinValue = 1.0m,
                SoftMaxValue = 2.0m,
                MinValue = 0.0m,
                MaxValue = 5.0m,
                InternalPropertyName = nameof(config.PenaltyRepetition),
                FormatString = "0.0",
                Increment = 0.5
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
                InternalPropertyName = nameof(config.PenaltyRepetitionRange),
                FormatString = "0",
                Increment = 1
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
                InternalPropertyName = nameof(config.PenaltyPresence),
                FormatString = "0.00",
                Increment = 0.05
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
                InternalPropertyName = nameof(config.PenaltyFrequency),
                FormatString = "0.00",
                Increment = 0.05
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
                InternalPropertyName = nameof(config.Seed),
                FormatString = "0",
                Increment = 1
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
                InternalPropertyName = nameof(config.MinP),
                FormatString = "0.00",
                Increment = 0.05
            });


            // New: Subscribe to PropertyChanged for each slider VM after adding them
            foreach (var slider in model.SliderProperties)
            {

                slider.PropertyChanged += model.OnSliderPropertyChanged;
            }

            return model;
        }


        // Handler to re-raise the aggregated event
        private void OnSliderPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SliderPropertyViewModel.Value))
            {
                AnyParameterChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? AnyParameterChanged; // New event for any slider value change


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
