using Avalonia.Controls;

namespace eSearch.Views
{
    public partial class TaskScheduleWindow : Window
    {
        public TaskScheduleWindow()
        {
            InitializeComponent();
            KeyUp += TaskScheduleWindow_KeyUp;
        }

        private void TaskScheduleWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
            }
        }
    }
}
