using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using System;

namespace eSearch.Views
{
    public partial class CustomSliderControl : UserControl
    {
        public CustomSliderControl()
        {
            InitializeComponent();


            // Attach event handlers
            this.SliderTheValue.ValueChanged += OnSliderValueChanged;
            this.NumericUpDownTheValue.ValueChanged += OnNumericValueChanged;

            // Initial updates
            UpdateTexts();
            UpdateLimits();
            UpdateValues();
        }

        // Bindable properties
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<CustomSliderControl, string>(nameof(Title), "Control Title");

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly StyledProperty<string> DescriptionProperty =
            AvaloniaProperty.Register<CustomSliderControl, string>(nameof(Description), "Control Description");

        public string Description
        {
            get => GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly StyledProperty<double> MinimumProperty =
            AvaloniaProperty.Register<CustomSliderControl, double>(nameof(Minimum), 0.0);

        public double Minimum
        {
            get => GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly StyledProperty<double> MaximumProperty =
            AvaloniaProperty.Register<CustomSliderControl, double>(nameof(Maximum), 100.0);

        public double Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly StyledProperty<double> SoftMinimumProperty =
            AvaloniaProperty.Register<CustomSliderControl, double>(nameof(SoftMinimum), 0.0);

        public double SoftMinimum
        {
            get => GetValue(SoftMinimumProperty);
            set => SetValue(SoftMinimumProperty, value);
        }

        public static readonly StyledProperty<double> SoftMaximumProperty =
            AvaloniaProperty.Register<CustomSliderControl, double>(nameof(SoftMaximum), 100.0);

        public double SoftMaximum
        {
            get => GetValue(SoftMaximumProperty);
            set => SetValue(SoftMaximumProperty, value);
        }

        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<CustomSliderControl, double>(
                nameof(Value),
                0.0,
                false,
                BindingMode.TwoWay);

        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        // Property changed handlers
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TitleProperty || change.Property == DescriptionProperty)
            {
                UpdateTexts();
            }
            else if (change.Property == MinimumProperty || change.Property == MaximumProperty ||
                     change.Property == SoftMinimumProperty || change.Property == SoftMaximumProperty)
            {
                UpdateLimits();
                UpdateValues(); // Re-clamp if needed
            }
            else if (change.Property == ValueProperty)
            {
                UpdateValues();
            }
        }

        private void UpdateTexts()
        {
            TextBlockSliderTitle.Text = Title;
            TextBlockSliderDescription.Text = Description;
        }

        private void UpdateLimits()
        {
            // Assume SoftMin/Max are within Min/Max; you can add validation if needed
            NumericUpDownTheValue.Minimum = (decimal)Minimum;
            NumericUpDownTheValue.Maximum = (decimal)Maximum;

            SliderTheValue.Minimum = SoftMinimum;
            SliderTheValue.Maximum = SoftMaximum;
        }

        private void UpdateValues()
        {
            // Update numeric directly
            if (NumericUpDownTheValue.Value != (decimal)Value)
            {
                NumericUpDownTheValue.Value = (decimal)Value;
            }

            // Update slider to clamped value
            double clampedValue = Math.Clamp(Value, SoftMinimum, SoftMaximum);
            if (SliderTheValue.Value != clampedValue)
            {
                SliderTheValue.Value = clampedValue;
            }
        }

        private void OnSliderValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            // Slider always within soft range, so set Value directly
            if (Value != e.NewValue)
            {
                Value = e.NewValue;
            }
        }

        private void OnNumericValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            // Numeric can be outside soft, but within hard min/max
            if ((decimal)Value != e.NewValue)
            {
                Value = (double)e.NewValue;
            }
        }
    }
}