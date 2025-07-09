using Avalonia.Controls;
using DesktopSearch2.Models.Search;
using DesktopSearch2.ViewModels;
using eSearch.Models;
using eSearch.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace eSearch.Views
{
    public partial class SearchFilterWindow2 : Window
    {

        TaskDialogResult DialogResult = TaskDialogResult.Cancel;

        public SearchFilterWindow2()
        {
            InitializeComponent();
            BtnOK.Click += BtnOK_Click;
            BtnCancel.Click += BtnCancel_Click;

            KeyUp += SearchFilterWindow2_KeyUp;
        }

        private void SearchFilterWindow2_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
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

        public static async Task<Tuple<TaskDialogResult, SearchFilterWindowViewModel2>> ShowDialog(Window owner, List<DataColumn> AvailableFields)
        {
            var viewModel = new SearchFilterWindowViewModel2();
            viewModel.SelectableFields.Clear();
            viewModel.SelectableFields.AddRange(AvailableFields);
            var window = new SearchFilterWindow2();
            window.DataContext = viewModel;
            await window.ShowDialog(owner);
            return new Tuple<TaskDialogResult, SearchFilterWindowViewModel2>(window.DialogResult, viewModel);
        }
    }
}
