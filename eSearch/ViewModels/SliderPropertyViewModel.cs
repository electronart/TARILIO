
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class SliderPropertyViewModel : ViewModelBase
    {
        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set
            {
                this.RaiseAndSetIfChanged(ref _title, value);
            }
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set
            {
                this.RaiseAndSetIfChanged(ref _description, value);
            }
        }

        private decimal _value = 0;
        public decimal Value
        {
            get => _value;
            set
            {
                this.RaiseAndSetIfChanged(ref _value, value);
            }
        }

        private decimal _minValue = 0;
        public decimal MinValue
        {
            get => _minValue;
            set
            {
                this.RaiseAndSetIfChanged(ref _minValue, value);
            }
        }

        private decimal _maxValue = 0;
        public decimal MaxValue
        {
            get => _maxValue;
            set
            {
                this.RaiseAndSetIfChanged(ref _maxValue, value);
            }
        }

        private decimal _softMinValue = 0;
        public decimal SoftMinValue
        {
            get => _softMinValue;
            set
            {
                this.RaiseAndSetIfChanged(ref _softMinValue, value);
            }
        }

        private decimal _softMaxValue = 0;
        public decimal SoftMaxValue
        {
            get => _softMaxValue;
            set
            {
                this.RaiseAndSetIfChanged(ref _softMaxValue, value);
            }
        }

        private string _internalPropertyName = string.Empty;
        public string InternalPropertyName
        {
            get => _internalPropertyName;
            set
            {
                this.RaiseAndSetIfChanged(ref _internalPropertyName, value);
            }
        }

        public double Increment
        {
            get => _increment;
            set
            {
                this.RaiseAndSetIfChanged(ref _increment, value);
            }
        }

        private double _increment = 0.1;

        public string FormatString
        {
            get => _formatString;
            set
            {
                this.RaiseAndSetIfChanged(ref _formatString, value);
            }
        }

        private string _formatString = "0.0";
    }
}
