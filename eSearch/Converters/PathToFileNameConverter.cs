using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Converters
{
    public class PathToFileNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                return Path.GetFileNameWithoutExtension(path);
            }
            return string.Empty; // Fallback for invalid or empty paths
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If two-way binding is needed, you might need to reconstruct the full path.
            // This is optional and depends on your use case.
            throw new NotImplementedException();
        }
    }
}
