using Avalonia.Controls;
using eSearch.Models;
using eSearch.ViewModels;
using ReactiveUI.Validation.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Views
{
    public partial class ExportSearchResultsWindow : Window
    {

        TaskDialogResult? _dialogResult = null;

        public ExportSearchResultsWindow()
        {
            InitializeComponent();
            BtnOK.Click += BtnOK_Click;
            BtnCancel.Click += BtnCancel_Click;

            LabelValidationErrors.Content = "";

            KeyUp += ExportSearchResultsWindow_KeyUp;
        }

        private void ExportSearchResultsWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
            }
        }

        private void BtnCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _dialogResult = TaskDialogResult.Cancel;
            Close();
        }

        private void BtnOK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            #region Validation
            if (this.DataContext is ExportSearchResultsViewModel exportSettings)
            {
                if (exportSettings.ExportAllColumns == false)
                {
                    
                    // Ensure at least one column is checked.
                    if (exportSettings.SelectedColumnsModel.Count == 0)
                    {
                        // This is a validation error.
                        LabelValidationErrors.Content = S.Get("Select at least one Column to export");
                        return;
                    }
                    
                }

                if (exportSettings.SelectedOutputFileTypeIndex == -1)
                {
                    // No output type selected.
                    LabelValidationErrors.Content =  S.Get("Select output format");
                    return;
                }

                if (exportSettings.SelectedRowsOnly)
                {
                    bool isRowChecked = false;
                    if (Program.GetMainWindow().DataContext is MainWindowViewModel mainWindowVM)
                    {
                        int i = mainWindowVM.Results.Count;
                        while ( i --> 0)
                        {
                            if (((ResultViewModel)mainWindowVM.Results[i]).IsResultChecked)
                            {
                                isRowChecked = true;
                                break;
                            }
                        }
                        if (!isRowChecked)
                        {
                            LabelValidationErrors.Content = S.Get("No results are checked");
                            return;
                        }
                    }
                }

                string outputDir = exportSettings.OutputDirectoryInput;
                if (string.IsNullOrWhiteSpace(outputDir))
                {
                    LabelValidationErrors.Content = S.Get("Select an output directory.");
                }
                if (!Directory.Exists(outputDir))
                {
                    LabelValidationErrors.Content = S.Get("Output Directory not found");
                    return;
                }

                string fileName = exportSettings.FileNameInput;
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    LabelValidationErrors.Content = S.Get("Must enter a file name");
                    return;
                }
                var isValid = !string.IsNullOrEmpty(fileName) && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
                if (!isValid)
                {
                    LabelValidationErrors.Content = S.Get("File name contains illegal characters");
                    return;
                }
            }
            #endregion


            _dialogResult = TaskDialogResult.OK;
            Close();
        }

        public TaskDialogResult DialogResult
        {
            get
            {
                if (_dialogResult == null)
                {
                    _dialogResult = TaskDialogResult.Cancel;
                }
                return (TaskDialogResult)_dialogResult;
            }
        }


        public static async Task<Tuple<TaskDialogResult, ExportSearchResultsViewModel>> ShowDialog(Window owner, DataColumn[] PossibleColumns)
        {
            var exportConfig = Program.ProgramConfig.ExportConfig;
            var vm = ExportSearchResultsViewModel.FromExportConfig(exportConfig, PossibleColumns);
            var dialog = new ExportSearchResultsWindow();
            dialog.DataContext = vm;
            
            /**
            vm.SelectedColumns.Clear();

            // This is a bit of a hack, to raise the events after the available columns are added.
            if (exportConfig.SelectedColumns != null)
            {
                foreach (var selectedColumn in exportConfig.SelectedColumns)
                {
                    vm.SelectedColumns.Add(selectedColumn);
                }
            }
            **/
            await dialog.ShowDialog(Program.GetMainWindow());
            Tuple<TaskDialogResult, ExportSearchResultsViewModel> result = new Tuple<TaskDialogResult, ExportSearchResultsViewModel>(dialog.DialogResult, vm);
            return result;
        }
    }
}
