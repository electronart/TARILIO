using Avalonia.Controls;

namespace eSearch.Views
{
    public partial class CopyResultsWindow : Window
    {
        public CopyResultsWindow()
        {
            InitializeComponent();
            KeyUp += CopyResultsWindow_KeyUp;
        }

        private void CopyResultsWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
            }
        }
    }
}
