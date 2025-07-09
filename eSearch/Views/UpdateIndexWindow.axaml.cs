using Avalonia.Controls;
using eSearch.Models;
using eSearch.Models.Configuration;
using eSearch.Models.Indexing;
using eSearch.ViewModels;
using static eSearch.Views.TextInputDialog;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Diagnostics;
using System;
using S = eSearch.ViewModels.TranslationsViewModel;
using System.Text;
using ReactiveUI;
using System.Linq;

namespace eSearch.Views
{
    public partial class UpdateIndexWindow : Window
    {

        public UpdateIndexWindow()
        {
            InitializeComponent();
            KeyUp += UpdateIndexWindow_KeyUp;
            DataContextChanged += UpdateIndexWindow_DataContextChanged;
            BtnUpdate.Click += BtnUpdate_Click;
            BtnDelete.Click += BtnDelete_Click;
            BtnRebuild.Click += BtnRebuild_Click;
            BtnRename.Click += BtnRename_Click;
        }

        private async void BtnRename_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is UpdateIndexWindowViewModel vm)
            {
                if (vm.SelectedIndex == null) return;
                var index = vm.SelectedIndex;


                ValidateSubmission handler = ValidateIndexName;

                var res = await TextInputDialog.ShowDialog(this, S.Get("Rename Index"), "", vm.SelectedIndex.Name, vm.SelectedIndex.Name, handler);
                if (res.Item1 == TaskDialogResult.OK)
                {
                    if (res.Item2.Text.Length > 0 && res.Item2.Text.Length < 64)
                    {
                        vm.SelectedIndex.Name = res.Item2.Text;
                        vm.IndexLibrary?.SaveLibrary();
                        vm.RaisePropertyChanged(nameof(vm.Indexes));
                    }
                }
            }
        }

        private async void BtnRebuild_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (   DataContext              is UpdateIndexWindowViewModel vm 
                && Program.GetMainWindow()  is MainWindow mainWindow
                && vm.SelectedIndex != null
                && vm.IndexLibrary?.GetConfiguration(vm.SelectedIndex) is IIndexConfiguration indexConfig
                && vm.SelectedIndex.Id                                 is string SelectedIndexID)
            {
                try
                {
                    #region From MainWindow
                    if (vm.SelectedIndex == null) return; // Should not be possible to call this method when no index selected but handle this just in case.
                    vm.SelectedIndex.EnsureClosed();
                    var ixLib = vm.IndexLibrary;
                    var ivm = IndexSettingsWindowViewModel.FromIIndexConfig(indexConfig);
                    var res = await IndexSettingsWindow.ShowDialog(ivm, this);
                    if (res.Item1 == TaskDialogResult.OK)
                    {
                        mainWindow.PauseSearchUpdates = true;
                        ivm.ApplyToIndexConfig(indexConfig);
                        ixLib.SaveLibrary();
                        
                        await UpdateIndexWithProgressDialog(indexConfig);
                        vm.SelectedIndex = null;
                        vm.RaisePropertyChanged(nameof(vm.Indexes)); // Causes list to be reloaded.
                        foreach(var index in vm.Indexes)
                        {
                            if (index.Id == SelectedIndexID)
                            {
                                vm.SelectedIndex = index;
                                break;
                            }
                        }

                        mainWindow.init_searchSources();
                        mainWindow.SelectAndDisplayIndex(vm.Indexes.FirstOrDefault(index => index?.Id == SelectedIndexID, null));
                        mainWindow.PauseSearchUpdates = false;
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    await TaskDialogWindow.OKDialog(S.Get("Something went wrong"), ex.ToString(), this);
                }
            }
        }

        private async Task UpdateIndexWithProgressDialog(IIndexConfiguration indexConfig)
        {
            if (DataContext is UpdateIndexWindowViewModel mwvm)
            {
                var ds = await ((LuceneIndexConfiguration)indexConfig).GetMultiDataSource(); // TODO Hack
                var idx = ((LuceneIndexConfiguration)indexConfig).LuceneIndex; // TODO Hack

                ProgressViewModel pvm = new ProgressViewModel();
                IndexTask ixTask = new IndexTask(ds, idx, pvm, false);
                mwvm.SelectedIndex = null;
                var taskRes = await IndexProgressWindow.ShowProgressDialogAndStartIndexTask(ixTask, this);
                if (taskRes.Item1 == TaskDialogResult.OK)
                {
                    Program.SaveProgramConfig();
                    Program.IndexLibrary.SaveLibrary();
                }
            }
        }


        private async void BtnDelete_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is UpdateIndexWindowViewModel vm)
            {
                if (vm.SelectedIndex == null) return;

                var indexToDelete = vm.SelectedIndex;

                string txtDelete = S.Get("Delete");
                string txtCancel = S.Get("Cancel");


                string deleteTitle = string.Format(
                    S.Get("Delete {0}?"),
                    indexToDelete.Name
                );
                string deleteMsg = S.Get("Delete this index.");

                var _dlgOptions = new TaskDialogWindowViewModel(
                    deleteTitle,       // MainInstruction
                    deleteMsg,   // Content
                    txtDelete, txtCancel, string.Empty          // Dialog Buttons
                    );
                var confirmationDialog = new TaskDialogWindow();
                confirmationDialog.Width = 400;
                confirmationDialog.Height = 180;
                confirmationDialog.DataContext = _dlgOptions;
                await confirmationDialog.ShowDialog<object>(this);
                var res = confirmationDialog.GetDialogResult();
                if (res != txtDelete)
                {
                    Debug.WriteLine("Cancel delete index");
                    return;
                }
            retryPoint:
                try
                {
                    vm.SelectedIndex.EnsureClosed();
                    if (System.IO.Directory.Exists(indexToDelete.GetAbsolutePath()))
                    {
                        System.IO.Directory.Delete(indexToDelete.GetAbsolutePath(), true);
                    }
                    vm.IndexLibrary?.RemoveIndex(indexToDelete.Id);
                    vm.IndexLibrary?.SaveLibrary();
                    vm.RaisePropertyChanged(nameof(vm.Indexes));

                }
                catch (Exception ex)
                {
                    switch (await TaskDialogWindow.RetryCancel(ex, this))
                    {
                        case TaskDialogResult.Retry:
                            goto retryPoint;
                        default:
                            break;
                    }
                }
            }
        }

        private async void BtnUpdate_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is UpdateIndexWindowViewModel vm)
            {
                if (vm.IndexLibrary == null)  return;
                if (vm.SelectedIndex == null) return;
                var res = await NewIndexWindow.ShowDialog(vm.IndexLibrary, vm.SelectedIndex, this);
                if (res.Item1 == TaskDialogResult.OK && res.Item2 is LuceneIndexConfiguration updatedConfig)
                {
                    vm.IndexLibrary.UpdateConfiguration(updatedConfig);
                    vm.IndexLibrary.SaveLibrary();
                    vm.RaisePropertyChanged(nameof(vm.Indexes));
                    vm.SelectedIndex = updatedConfig.LuceneIndex;

                    ProgressViewModel pvm = new ProgressViewModel();
                    var ds = updatedConfig.GetMainDataSource();

                    IndexTask ixTask = new IndexTask(ds, updatedConfig.LuceneIndex, pvm, false, true);
                    var taskRes = await IndexProgressWindow.ShowProgressDialogAndStartIndexTask(ixTask, Program.GetMainWindow());
                    if (taskRes.Item1 == TaskDialogResult.OK)
                    {
                        if (Program.GetMainWindow()?.DataContext is MainWindowViewModel mwvm)
                        {
                            mwvm.RaisePropertyChanged(nameof(mwvm.AvailableIndexes));
                            foreach (var idx in mwvm.AvailableIndexes)
                            {
                                if (idx.Id == updatedConfig.LuceneIndex.Id)
                                {
                                    mwvm.SelectedIndex = idx;
                                }
                            }
                        }
                    }

                }
            }
        }

        private void UpdateIndexWindow_DataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is UpdateIndexWindowViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (DataContext is UpdateIndexWindowViewModel viewModel)
            {
                if (e.PropertyName == nameof(viewModel.SelectedIndex))
                {
                    if (viewModel.SelectedIndex == null)
                    {
                        viewModel.DisplayedIndexInformation = S.Get("No Index Selected.");
                    } else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb
                            .AppendLine(viewModel.SelectedIndex.Name)
                            .AppendLine()
                            .AppendLine(viewModel.SelectedIndex.GetAbsolutePath());
                        try
                        {
                            viewModel.SelectedIndex.OpenRead();
                            sb
                                .AppendLine()
                                .AppendLine(String.Format(S.Get("{0} Document(s)"), viewModel.SelectedIndex.GetTotalDocuments().ToString("N0")));
                            viewModel.SelectedIndex.EnsureClosed();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Non fatal - Failed to open index to retrieve the number of documents");
                        }
                        //.AppendLine()
                        //.AppendLine(SelectedIndex.Description);

                        viewModel.DisplayedIndexInformation = sb.ToString();
                    }
                }
            }
        }

        private void UpdateIndexWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
            }
        }

        private bool ValidateIndexName(string indexName, out string validationError)
        {
            if (string.IsNullOrEmpty(indexName))
            {
                validationError = "Index name may not be empty";
                return false;
            }
            if (indexName.Length > 64)
            {
                validationError = "Index name may not be longer than 64 characters";
                return false;
            }
            validationError = "";
            return true;
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }

        private void Button_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }
    }
}
