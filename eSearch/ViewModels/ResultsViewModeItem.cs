using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public enum ResultsViewMode
    {
        Grid,
        Content,
        // TODO Icon view mode maybe...
    }

    public class ResultsViewModeItem
    {
        public ResultsViewMode Mode { get; }
        public string DisplayName { get; }

        public Bitmap Icon { get; }

        public ResultsViewModeItem(ResultsViewMode mode, string displayName, Bitmap icon)
        {
            this.Mode = mode;
            this.DisplayName = displayName;
            this.Icon = icon;
        }
    }
}
