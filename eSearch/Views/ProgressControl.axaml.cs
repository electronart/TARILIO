using Avalonia.Controls;
using eSearch.ViewModels;

namespace eSearch.Views
{
    public partial class ProgressControl : UserControl
    {
        public ProgressControl()
        {
            InitializeComponent();
        }

        public ProgressControl(ProgressViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
    }
}
