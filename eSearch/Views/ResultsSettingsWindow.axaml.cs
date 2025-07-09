using Avalonia.Controls;
using eSearch.Models;
using eSearch.ViewModels;
using System.Threading.Tasks;
using System;
using static eSearch.ViewModels.ResultsSettingsWindowViewModel;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Views
{
    public partial class ResultsSettingsWindow : Window
    {

        TaskDialogResult DialogResult = TaskDialogResult.Cancel;

        public ResultsSettingsWindow()
        {
            InitializeComponent();

            BtnOK.Click += BtnOK_Click;
            BtnCancel.Click += BtnCancel_Click;

            KeyUp += ResultsSettingsWindow_KeyUp;

            LabelValidationErrors.Content = string.Empty;
        }

        private void ResultsSettingsWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
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
            #region Validate Column Selection
            if (DataContext is ResultsSettingsWindowViewModel vm)
            {
                bool atleast_one_column_checked = false;
                if (vm.AvailableColumns != null)
                {
                    foreach (var column in vm.AvailableColumns)
                    {
                        if (column.IsChecked)
                        {
                            atleast_one_column_checked = true;
                            break;
                        }
                    }
                }

                if (!atleast_one_column_checked)
                {
                    LabelValidationErrors.Content = S.Get("Select at least one column");
                    return;
                }
            }

            #endregion
            DialogResult = TaskDialogResult.OK;
            Close();
        }

        public static async Task<Tuple<TaskDialogResult, ResultsSettingsWindowViewModel>> ShowDialog(DataColumn[] ColumnSelection, ColumnWidthOption WidthOption, QueryViewModel query)
        {
            var viewModel = new ResultsSettingsWindowViewModel(ColumnSelection);
            viewModel.SelectedColumnSizingMode = WidthOption;
            viewModel.IsLimitResultsChecked = query.LimitResults;
            viewModel.LimitResultsStartAt   = 1;
            viewModel.LimitResultsEndAt     = query.LimitResultsEndAt;


            var dialog = new ResultsSettingsWindow();
            dialog.DataContext = viewModel;
            await dialog.ShowDialog(Program.GetMainWindow());
            return new Tuple<TaskDialogResult, ResultsSettingsWindowViewModel>(dialog.DialogResult, viewModel);
        }
    }
}
