using Avalonia.Data.Converters;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Converters
{
    public class SearchControlsVerticalAlignmentConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool AIModeEnabled)
            {
                if (AIModeEnabled)
                {
                    return VerticalAlignment.Center;
                } else
                {
                    return VerticalAlignment.Bottom;
                }
            }
            throw new NotSupportedException("A bool must be passed to convert");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Should not attempt to convert back");
        }
    }
}
