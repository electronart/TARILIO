using Avalonia.Controls;
using Avalonia.Xaml.Interactions.Custom;
using eSearch.Models;
using eSearch.ViewModels;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Views
{
    public partial class ModelChooserWindow : Window
    {
        private string _result = string.Empty;


        public ModelChooserWindow()
        {
            InitializeComponent();
            BindEvents();
            KeyUp += ModelChooserWindow_KeyUp;
        }

        private void ModelChooserWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close(TaskDialogResult.Cancel);
            }
        }

        public string GetDialogResult()
        {
            return _result;
        }

        private void BindEvents()
        {
            ButtonOK.Click += ButtonOK_Click;
            ButtonCancel.Click += ButtonCancel_Click;
        }

        private void ButtonCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close(TaskDialogResult.Cancel);
        }

        private void ButtonOK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close(TaskDialogResult.OK);
        }        
    }
}
