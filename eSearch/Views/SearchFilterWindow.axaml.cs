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
    public partial class SearchFilterWindow : Window
    {

        TaskDialogResult DialogResult = TaskDialogResult.Cancel;

        public SearchFilterWindow()
        {
            InitializeComponent();
            BtnOK.Click += BtnOK_Click;
            BtnCancel.Click += BtnCancel_Click;

            KeyUp += SearchFilterWindow_KeyUp;
        }

        private void SearchFilterWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
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

        public static async Task<Tuple<TaskDialogResult, SearchFilterWindowViewModel>> ShowDialog(Window owner, IEnumerable<QueryFilter> existingFilters, List<string> AvailableFields)
        {
            var viewModel = new SearchFilterWindowViewModel();
            viewModel.QueryFilters.Clear();
            foreach (var existingFilter in existingFilters)
            {
                viewModel.QueryFilters.Add(new QueryFilterViewModel
                {
                    SearchText    = existingFilter.SearchText,
                    SelectedField = existingFilter.FieldName,
                    AvailableFields = AvailableFields
                });
            }
            viewModel.AvailableFields = AvailableFields;
            var window = new SearchFilterWindow();
            window.DataContext = viewModel;
            await window.ShowDialog(owner);
            return new Tuple<TaskDialogResult, SearchFilterWindowViewModel>(window.DialogResult, viewModel);
        }
    }
}
