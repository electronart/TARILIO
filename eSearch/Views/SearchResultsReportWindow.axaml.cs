using Avalonia.Controls;

namespace eSearch.Views
{
    public partial class SearchResultsReportWindow : Window
    {
        public SearchResultsReportWindow()
        {
            InitializeComponent();

            KeyUp += SearchResultsReportWindow_KeyUp;
        }

        private void SearchResultsReportWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            Close();
        }
    }
}
