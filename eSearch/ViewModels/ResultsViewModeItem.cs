using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public enum ResultsViewMode
    {
        Grid = 0,
        Content = 1,
        // TODO Icon view mode maybe...
    }

    public class ResultsViewModeItem : ViewModelBase
    {

        public ResultsViewMode Mode { get; }
        public string DisplayNameKey { get; }
        public string IconKey { get; }

        public IImage Icon => I[IconKey];
        public string DisplayName => S[DisplayNameKey];

        public ResultsViewModeItem(
            ResultsViewMode mode,
            string displayNameKey,
            string iconKey)
        {
            Mode = mode;
            DisplayNameKey = displayNameKey;
            IconKey = iconKey;

            S.PropertyChanged += (_, __) => this.RaisePropertyChanged(nameof(DisplayName));
            I.PropertyChanged += (_, __) => this.RaisePropertyChanged(nameof(Icon));
        }
    }
}
