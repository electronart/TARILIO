using Avalonia.Controls;
using Avalonia.Media;
using eSearch.Models;
using eSearch.ViewModels;
using eSearch.Views;
using eSearch;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Threading.Tasks;
using DesktopSearch2.ViewModels;

namespace DesktopSearch2.Views
{
    public partial class FontPickerWindow : Window
    {
        TaskDialogResult DialogResult = TaskDialogResult.Cancel;


        public FontPickerWindow()
        {
            InitializeComponent();

            /*
            InstalledFontCollection installedFontCollection = new InstalledFontCollection();
            List<FontFamily> families = new List<FontFamily>();
            FontManager.
            families.AddRange(installedFontCollection.Families);
            */

            BtnOK.Click += BtnOK_Click;
            BtnCancel.Click += BtnCancel_Click;
            KeyUp += FontPickerWindow_KeyUp;

        }

        private void FontPickerWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
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

        public static async Task<Tuple<TaskDialogResult, FontPickerWindowViewModel>> ShowDlg(Window owner)
        {
            var fontSettings = new FontPickerWindowViewModel();
            var dialog = new FontPickerWindow();
            dialog.DataContext = fontSettings;
            await dialog.ShowDialog(owner);
            return new Tuple<TaskDialogResult, FontPickerWindowViewModel>(dialog.DialogResult, fontSettings);
        }

    }
}
