using Avalonia.Controls;
using DesktopSearch2.Views;
using eSearch.Models;
using eSearch.ViewModels;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace eSearch.Views
{
    public partial class ViewerSettingsWindow : Window
    {
        public ViewerSettingsWindow()
        {
            InitializeComponent();
            BtnOK.Click += BtnOK_Click;
            BtnCancel.Click += BtnCancel_Click;
            BtnSelectHighlightColor.Click += BtnSelectHighlightColor_Click;
            BtnSelectViewerFont.Click += BtnSelectViewerFont_Click;
            KeyUp += ViewerSettingsWindow_KeyUp;
        }

        private void ViewerSettingsWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
            }
        }

        private async void BtnSelectViewerFont_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var res = await FontPickerWindow.ShowDlg(this);
            if (res.Item1 == TaskDialogResult.OK)
            {
                var fontfamily  = res.Item2.SelectedFontFamily;
                var fontsize    = res.Item2.FontSizePt;
                var font = new Font(fontfamily.Name, fontsize);
                Program.ProgramConfig.ViewerConfig.FontFamilyName = fontfamily.Name;
                Program.ProgramConfig.ViewerConfig.FontSizePt = fontsize;
                Program.SaveProgramConfig();

                if (this.DataContext is ViewerSettingsWindowViewModel vm)
                {
                    vm.FontFamilyName = fontfamily.Name + " " + fontsize + "pt";
                }
            }
        }

        private async void BtnSelectHighlightColor_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var res = await ColorPickerWindow.ShowDlg(this);
            if (res.Item1 == TaskDialogResult.OK)
            {
                var selectedColor = res.Item2.SelectedColor;
                if (DataContext != null && DataContext is ViewerSettingsWindowViewModel vm) {
                    var rgb = selectedColor.ToRgb();
                    Program.ProgramConfig.ViewerConfig.HitHighlightColor = Color.FromArgb(0, rgb.R, rgb.G, rgb.B);
                    Program.SaveProgramConfig();
                    vm.UpdateHighlightColor();
                }
            }
        }

        private void BtnCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            DialogResult = TaskDialogResult.Cancel;
            Close();
        }

        private void BtnOK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            DialogResult = TaskDialogResult.OK;
            Close();
        }

        public TaskDialogResult DialogResult = TaskDialogResult.Cancel;


        public static async Task<Tuple<TaskDialogResult, ViewerSettingsWindowViewModel>> ShowDialog()
        {
            var viewerSettings = new ViewerSettingsWindowViewModel();
            // I don't like the way this is implemented but avalonia radio button to enum binding is something awful.
            viewerSettings.OptionPDFViewer = Program.ProgramConfig.ViewerConfig.PDFViewerOption;            // HACK
            viewerSettings.OptionViewLargeFiles = Program.ProgramConfig.ViewerConfig.ViewLargeFileOption;   // HACK

            var dialog = new ViewerSettingsWindow();
            dialog.DataContext = viewerSettings;
            await dialog.ShowDialog(Program.GetMainWindow());
            return new Tuple<TaskDialogResult, ViewerSettingsWindowViewModel>(dialog.DialogResult, viewerSettings);
        }
    }
}
