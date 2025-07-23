using Avalonia.Controls;
using OpenXmlPowerTools;
using System.IO;
using System.Reflection;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            BtnClose.Click += BtnClose_Click;
            ButtonHelp.Click += ButtonHelp_Click;
            this.KeyUp += AboutWindow_KeyUp;
        }

        private async void ButtonHelp_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            string? exeDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? string.Empty);
            string helpFilePath = Path.Combine(exeDir ?? "", "help", "eSearch-Pro-User-Guide.pdf");
            if (File.Exists(helpFilePath))
            {
                var uri = new System.Uri(helpFilePath);
                var url = uri.AbsoluteUri;
                Models.Utils.CrossPlatformOpenBrowser(url);
            }
            else
            {
                await TaskDialogWindow.OKDialog(S.Get("File not found"), "Expected Location: " + helpFilePath, Program.GetMainWindow());
            }
        }

        private void AboutWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
            }
        }

        private void BtnClose_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
    }
}
