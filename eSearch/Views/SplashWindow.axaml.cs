using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace eSearch;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();

        ProductTag.Content = Program.GetBaseProductTag(); // Use this instead of ProgramConfig to avoid I/O
        ProductVersion.Content = Program.GetProgramVersion();
        ProductImg.Source = Program.GetProductIcon();

}
}