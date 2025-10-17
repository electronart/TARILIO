
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class SliderPropertyViewModel : ViewModelBase
    {

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Value { get; set; } = 0;

        public decimal MinValue { get; set; } = 0;

        public decimal MaxValue { get; set; } = 0;

        public decimal SoftMinValue { get; set; } = 0;

        public decimal SoftMaxValue { get; set; } = 0;

        public string InternalPropertyName { get; set; } = string.Empty;
    }
}
