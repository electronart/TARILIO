using Avalonia.Controls;
using Avalonia.Platform.Storage;
using eSearch.Models;
using eSearch.Models.Search.Synonyms;
using eSearch.ViewModels;
using DynamicData;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Views
{
    public partial class SearchSettingsWindow : Window
    {

        public TaskDialogResult DialogResult = TaskDialogResult.Cancel;

        public SearchSettingsWindow()
        {
            InitializeComponent();
            BtnOK.Click += BtnOK_Click;
            BtnCancel.Click += BtnCancel_Click;

            BtnSynonymGroupsNew.Click += BtnSynonymGroupsNew_Click;
            BtnSynonymGroupsRename.Click += BtnSynonymGroupsRename_Click;
            BtnSynonymGroupsDelete.Click += BtnSynonymGroupsDelete_Click;
            BtnSynonymGroupsSort.Click += BtnSynonymGroupsSort_Click;

            BtnBrowseForList.Click += BtnBrowseForList_Click;

            KeyUp += SearchSettingsWindow_KeyUp;

            listBoxSynonymGroups.SelectionChanged += ListBoxSynonymGroups_SelectionChanged;
        }

        private void ListBoxSynonymGroups_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DataContext is SearchSettingsViewModel searchSettingsViewModel)
            {
                textBoxExpansion.Focus();
            }
        }

        private void SearchSettingsWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
            }
        }

        private async void BtnBrowseForList_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var storageProvider = this.StorageProvider;

            var filePickerOptions = new FilePickerOpenOptions {
                Title = "Select List File",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.TextPlain },
                SuggestedStartLocation = await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
            };

            var files = await storageProvider.OpenFilePickerAsync(filePickerOptions);

            if (files.Count == 1)
            {
                var file = files[0];
                var path = file.Path;
                if (this.DataContext is SearchSettingsViewModel searchSettingsViewModel)
                {
                    searchSettingsViewModel.ListPath = path.LocalPath;
                }
            }
        }

        private void BtnSynonymGroupsSort_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is SearchSettingsViewModel searchSettings)
            {
                if (searchSettings.SynonymFileIsSelected && searchSettings.SelectedSynonymFile != null && searchSettings.SelectedSynonymFile.SynonymGroups != null)
                {
                    var list = searchSettings.SelectedSynonymFile.SynonymGroups.OrderBy(s => s.Name).ToList();
                    searchSettings.SelectedSynonymFile.SynonymGroups.Clear();
                    searchSettings.SelectedSynonymFile.SynonymGroups.AddRange(list);
                }
            }
        }

        private async void BtnSynonymGroupsDelete_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is SearchSettingsViewModel searchSettings)
            {
                if (searchSettings.SynonymFileIsSelected && searchSettings.SelectedSynonymFile != null && searchSettings.SelectedSynonymGroup != null) {

                    string groupName = searchSettings.SelectedSynonymGroup.Name;
                    string text = S.Get("%GROUP% will be deleted.").Replace("%GROUP%", groupName);
                    var res = await TaskDialogWindow.DeleteCancel(S.Get("Delete Synonyms Group?"), text, this);
                    if (res == TaskDialogResult.OK)
                    {
                        // OK to delete.
                        var index = searchSettings.SelectedSynonymFile.SynonymGroups.IndexOf(searchSettings.SelectedSynonymGroup);
                        searchSettings.SelectedSynonymFile.SynonymGroups.RemoveAt(index);
                    }

                }
            }
        }

        private async void BtnSynonymGroupsRename_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is SearchSettingsViewModel searchSettings)
            {
                if (searchSettings.SynonymFileIsSelected && searchSettings.SelectedSynonymFile != null && searchSettings.SelectedSynonymGroup != null)
                {
                    var title = S.Get("Rename Synonyms Group");
                    var label = S.Get("Name");
                    var watermark = S.Get("Synonym Group Name");
                    var text = searchSettings.SelectedSynonymGroup.Name;

                    var res = await TextInputDialog.ShowDialog(this, title, label, watermark, text);
                    if (res.Item1 == TaskDialogResult.OK)
                    {
                        if (res.Item2.TextValid)
                        {
                            string newName = res.Item2.Text;
                            var group = searchSettings.SelectedSynonymGroup;
                            var index = searchSettings.SelectedSynonymFile.SynonymGroups.IndexOf(group);
                            searchSettings.SelectedSynonymFile.SynonymGroups.RemoveAt(index);
                            group.Name = newName;
                            searchSettings.SelectedSynonymFile.SynonymGroups.Insert(index, group); // The weird remove/add is to do with databinding/updating the ui properly.
                        }
                    }
                }
            }
        }

        private async void BtnSynonymGroupsNew_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is SearchSettingsViewModel searchSettings)
            {
                if (searchSettings.SynonymFileIsSelected && searchSettings.SelectedSynonymFile != null)
                {
                    var selectedSynonymFile = searchSettings.SelectedSynonymFile;

                    var title = S.Get("New Synonyms Group");
                    var label = S.Get("Name");
                    var watermark = S.Get("Synonym Group Name");
                    var text = "";

                    var res = await TextInputDialog.ShowDialog(this, title, label, watermark, text);
                    if (res.Item1 == TaskDialogResult.OK)
                    {
                        if (res.Item2.TextValid)
                        {
                            string newName = res.Item2.Text;
                            var group = new SynonymGroup { Name = newName, Synonyms = new string[0] };
                            selectedSynonymFile.SynonymGroups.Add(group);

                            searchSettings.SelectedSynonymGroup = group;
                        }
                    }
                }
            }
        }

        private void BtnCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            DialogResult = TaskDialogResult.Cancel;
            this.Close();
        }

        private void BtnOK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            #region Validation
            if (this.DataContext is SearchSettingsViewModel searchSettings)
            {
                searchSettings.ValidationErrors.Clear();
                if (searchSettings.UseSynonymFiles)
                {
                    string[] selectedSynonymFiles = searchSettings.GetSelectedSynonymFileNames();
                    if (selectedSynonymFiles.Length == 0)
                    {
                        // This is invalid. Must have at least one Synonym Group Selected.
                        var validationError = new ValidationError(nameof(searchSettings.SelectedSynonymFile), S.Get("Check at least one Synonym file from the drop down."));
                        searchSettings.ValidationErrors.Add(validationError);
                    }
                }
                if (searchSettings.UseList)
                {
                    string selectedPath = searchSettings.ListPath.Trim();
                    if (string.IsNullOrEmpty(selectedPath))
                    {
                        var validationError = new ValidationError(nameof(searchSettings.ListPath), S.Get("No list file supplied"));
                        searchSettings.ValidationErrors.Add(validationError);
                    } else
                    {
                        if (!File.Exists(selectedPath))
                        {
                            var validationError = new ValidationError(nameof(searchSettings.ListPath), S.Get("File does not exist"));
                            searchSettings.ValidationErrors.Add(validationError);
                        }
                    }
                }


                // Finally, update Errors.
                searchSettings.UpdateErrors();
                if (searchSettings.ValidationErrors.Count > 0) return; // Don't close the dialog due to input error.
            }
            #endregion



            DialogResult = TaskDialogResult.OK;
            this.Close();
        }

        public static async Task<Tuple<TaskDialogResult,SearchSettingsViewModel>> ShowDialog()
        {
            var settings = new SearchSettingsViewModel();
            var dialog = new SearchSettingsWindow();
                dialog.DataContext = settings;
            await dialog.ShowDialog(Program.GetMainWindow());
            return new Tuple<TaskDialogResult, SearchSettingsViewModel>(dialog.DialogResult, settings);

        }
    }
}
