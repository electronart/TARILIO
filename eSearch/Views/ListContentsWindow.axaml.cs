using Avalonia.Controls;

namespace eSearch.Views
{
    public partial class ListContentsWindow : Window
    {
        public ListContentsWindow()
        {
            InitializeComponent();

            KeyUp += ListContentsWindow_KeyUp;
        }

        private void ListContentsWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
            }
        }
    }
}
