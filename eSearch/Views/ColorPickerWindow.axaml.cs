using Avalonia.Controls;
using eSearch;
using eSearch.Models;
using eSearch.ViewModels;
using eSearch.Views;
using System.Threading.Tasks;
using System;
using DesktopSearch2.ViewModels;

namespace DesktopSearch2.Views
{
    public partial class ColorPickerWindow : Window
    {
        
        TaskDialogResult DialogResult = TaskDialogResult.Cancel;

        public ColorPickerWindow()
        {
            InitializeComponent();

            BtnOK.Click += BtnOK_Click;
            BtnCancel.Click += BtnCancel_Click;
            KeyUp += ColorPickerWindow_KeyUp;
        }

        private void ColorPickerWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
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

        public static async Task<Tuple<TaskDialogResult, ColorPickerWindowViewModel>> ShowDlg(Window owner)
        {
            var colorSettings = new ColorPickerWindowViewModel();

            var dialog = new ColorPickerWindow();
            dialog.DataContext = colorSettings;
            await dialog.ShowDialog(owner);
            return new Tuple<TaskDialogResult, ColorPickerWindowViewModel>(dialog.DialogResult, colorSettings);
        }


    }
}
