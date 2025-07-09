using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DynamicData;
using eSearch.Interop;
using eSearch.Models;
using eSearch.Models.Configuration;
using eSearch.Models.DataSources;
using eSearch.Models.Documents;
using eSearch.Models.Indexing;
using eSearch.ViewModels;
using ikvm.runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Views
{


    public partial class NewIndexWindow : Window
    {

        TaskDialogResult DialogResult = TaskDialogResult.Cancel;

        public NewIndexWindow()
        {
            InitializeComponent();
            KeyUp += NewIndexWindow_KeyUp;
            BtnOK.Click += BtnOK_Click;
            BtnCancel.Click += BtnCancel_Click;
            this.Loaded += NewIndexWindow_Loaded;
        }

        

        private async void DeleteDataSourceBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NewIndexWindowViewModel vm)
            {
                if (sender is Button deleteBtn && deleteBtn.DataContext is IDataSource src)
                {
                    RemoveDataSourceFromPlugins(src);
                    vm.DataSources.Remove(src);
                }
            }
        }

        private async void RemoveDataSourceFromPlugins(IDataSource src)
        {
            if (DataContext is NewIndexWindowViewModel vm)
            {
                foreach (var plugin in await Program.PluginLoader.GetInstalledPlugins())
                {
                    foreach (var manager in plugin.GetPluginDataSourceManagers())
                    {
                        var dataSources = manager.GetConfiguredDataSources(vm.Index.Id);
                        if (dataSources.Contains(src))
                        {
                            manager.RemoveDataSource(src);
                        }
                    }
                }
            }
        }

        private async void NewIndexWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            #region Insert any Plugin Datasource Buttons now
            foreach(var plugin in await Program.PluginLoader.GetInstalledPlugins())
            {
                var dsManagers = plugin.GetPluginDataSourceManagers();
                foreach (var manager in dsManagers)
                {
                    Button button = new Button();
                    button.Width  = 50;
                    button.Height = 50;
                    button.Tag    = manager;
                    button.Click += AddPluginDSButtonClick;

                    Avalonia.Controls.Image mainImage = new Avalonia.Controls.Image();
                    mainImage.Width  = 30;
                    mainImage.Height = 30;
                    if (File.Exists(manager.GetDataSourceIconPath()))
                    {
                        mainImage.Source = new Avalonia.Media.Imaging.Bitmap(manager.GetDataSourceIconPath());
                    }
                    Avalonia.Controls.Image addImage = new Avalonia.Controls.Image();
                    addImage.Width = 15;
                    addImage.Height = 15;
                    addImage.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
                    addImage.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
                    addImage.Margin = new Avalonia.Thickness(2);
                    addImage.Source = new Avalonia.Media.Imaging.Bitmap(AssetLoader.Open(new Uri(GetAddIconRes("icons8-add-48.png"))));

                    Panel combinedIconPanel = new Panel();
                    combinedIconPanel.Children.Add(mainImage);
                    combinedIconPanel.Children.Add(addImage);
                    button.Content = combinedIconPanel;
                    StackPanelDataSourceButtons.Children.Add(button);
                }
            }
            #endregion

            txtBoxIndexName.Focus();
            if (DataContext is NewIndexWindowViewModel vm)
            {
                if (!vm.UpdatingExistingIndex) { txtBoxIndexName.SelectAll(); }
            }
            
            checkBoxIncludeAll.IsCheckedChanged += CheckBoxIncludeAll_IsCheckedChanged;
        }

        private string GetAddIconRes(string name)
        {
            var assemblyName = typeof(Program).Assembly.GetName().Name;
            // new Bitmap(AssetLoader.Open(new System.Uri("avares://" + assemblyName + "/Assets/esearch-icon.ico")));
            return "avares://" + assemblyName + "/Assets/" + name;
        }

        private async void AddPluginDSButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is NewIndexWindowViewModel vm) {
                if (sender is Button button && button.Tag is IPluginDataSourceManager manager)
                {
                    await manager.InvokeDataSourceConfigurator(vm.Index.Id, null);
                    RefreshPluginDataSources();
                }
            }
        }

        private void CheckBoxIncludeAll_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (checkBoxIncludeAll.IsChecked == false)
            {
                if (DataContext is NewIndexWindowViewModel viewModel)
                {
                    foreach( var node in viewModel.TreeViewFileTypes )
                    {
                        node.IsChecked = false;
                        foreach(var subNode in node.SubNodes)
                        {
                            node.IsChecked = false;
                        }
                    }
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
            var vm = this.DataContext as NewIndexWindowViewModel;
            #region Validation
            // Check non empty and unique name
            if (string.IsNullOrEmpty(vm.Index.Name))
            {
                vm.ValidationError = S.Get("Index name required.");
                return;
            }
            // Check at least one datasource.
            if (vm.DataSources == null || vm.DataSources.Count == 0)
            {
                vm.ValidationError = S.Get("Select data to be indexed.");
                return;
            }
            // TODO Check if index file types selected, at least one type is selected.
            List<string> SelectedFileExtensions = null;
            if (!vm.IsIncludeAllChecked)
            {
                SelectedFileExtensions = new List<string>();
                foreach (var node in vm.TreeViewFileTypes)
                {
                    if (node.IsChecked == true)
                    {
                        if (node.Tag is DocumentType docType)
                        {
                            SelectedFileExtensions.Add(docType.Extension);
                        }
                    }
                    if (node.SubNodes != null)
                    {
                        foreach (var subNode in node.SubNodes)
                        {
                            if (subNode.IsChecked == true)
                            {
                                if (subNode.Tag is DocumentType docType)
                                {
                                    SelectedFileExtensions.Add(docType.Extension);
                                }
                            }
                        }
                    }
                }
                
                if (SelectedFileExtensions.Count == 0)
                {
                    vm.ValidationError = S.Get("Select at least one content type to index.");
                    return;
                }
            }
            #endregion

            
            LuceneIndex index = new LuceneIndex(
                vm.Index.Name,
                vm.Index.Description,
                vm.Index.Id,
                vm.Index.Location,
                0
            );
            List<DirectoryDataSource> directoryDataSources = new List<DirectoryDataSource>();
            List<FileDataSource> fileDataSources = new List<FileDataSource>();
            foreach (var source in vm.DataSources)
            {
                if (source is DirectoryDataSource directoryDataSource)
                {
                    directoryDataSources.Add(directoryDataSource);
                }
                if (source is FileDataSource fileDataSource)
                {
                    fileDataSources.Add(fileDataSource);
                }
            }

            LuceneIndexConfiguration config = new LuceneIndexConfiguration
            {
                DirectoryDataSources = directoryDataSources,
                FileDataSources = fileDataSources,
                LuceneIndex = index
            };

            config.SelectedFileExtensions = SelectedFileExtensions;

            config.SelectedStopWordFiles.Clear();

            #region Apply index settings
            if (vm.indexSettingsWindowViewModel == null)
            {
                vm.indexSettingsWindowViewModel = new IndexSettingsWindowViewModel();
            }
            vm.indexSettingsWindowViewModel.ApplyToIndexConfig(config);
            #endregion

            DialogResult = TaskDialogResult.OK;
            Close(config);
        }

        private void NewIndexWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
            }
        }

        private void Binding(object? sender, Avalonia.Input.TextInputEventArgs e)
        {
        }

        private async void RefreshPluginDataSources()
        {
            if (DataContext is NewIndexWindowViewModel viewModel) {
                #region Remove Any Plugin DataSources from the list
                int i = viewModel.DataSources.Count;
                while (i --> 0)
                {
                    var dataSource = viewModel.DataSources[i];
                    if (dataSource is DirectoryDataSource) continue;
                    if (dataSource is FileDataSource) continue;
                    if (dataSource is MultipleSourceDataSource) continue;
                    viewModel.DataSources.RemoveAt(i);
                }
                #endregion
                #region Build list of Plugin DataSources
                foreach (var plugin in await Program.PluginLoader.GetInstalledPlugins())
                {
                    foreach (var dataSourceManager in plugin.GetPluginDataSourceManagers())
                    {
                        var dataSources = dataSourceManager.GetConfiguredDataSources(viewModel.Index.Id);
                        foreach(var dataSource in dataSources)
                        {
                            viewModel.DataSources.Add(dataSource);
                        }
                    }
                }
                #endregion
            }
        }

        public static async Task<Tuple<TaskDialogResult, IIndexConfiguration>> ShowDialog(IndexLibrary library, IIndex editingIndex, Window parent)
        {
            NewIndexWindowViewModel viewModel;
            if (editingIndex == null)
            {
                viewModel = new NewIndexWindowViewModel(parent, false);
                string id = System.Guid.NewGuid().ToString();
                string fullPath = Path.Combine(Program.ESEARCH_INDEX_DIR, id);
                string relativePath = Path.GetRelativePath(Path.GetDirectoryName(Program.ESEARCH_INDEX_LIB_FILE), fullPath);
                viewModel.Index = new IndexViewModel(new LuceneIndex("New Index", "No Description", id, relativePath, 0));
            } else
            {

                LuceneIndex tempIndex = new LuceneIndex(
                    editingIndex.Name,
                    editingIndex.Description,
                    editingIndex.Id,
                    editingIndex.Location,
                    editingIndex.Size
                );
                
                viewModel = new NewIndexWindowViewModel(parent, true);
                #region Populate View model based on existing index parameters
                IndexViewModel editingIndexViewModel = new IndexViewModel(tempIndex);
                viewModel.Index = editingIndexViewModel;

                var config = library.GetConfiguration((LuceneIndex)editingIndex);
                viewModel.DataSources.AddRange(config.DirectoryDataSources);
                viewModel.DataSources.AddRange(config.FileDataSources);

                viewModel.IsIncludeAllChecked = config.SelectedFileExtensions == null;
                #region FileType Treeview
                foreach (var node in viewModel.TreeViewFileTypes)
                {
                    if (node.Tag is DocumentType docType)
                    {
                        var extension = docType.Extension;
                        if (config.SelectedFileExtensions == null)
                        {
                            node.IsChecked = true;
                        }
                        else
                        {
                            node.IsChecked = config.SelectedFileExtensions.Contains(extension);
                        }
                    }
                    if (node.SubNodes != null)
                    {
                        foreach (var subNode in node.SubNodes)
                        {
                            if (subNode.Tag is DocumentType docType2)
                            {
                                var extension = docType2.Extension;
                                if (config.SelectedFileExtensions == null)
                                {
                                    subNode.IsChecked = true;
                                }
                                else
                                {
                                    subNode.IsChecked = config.SelectedFileExtensions.Contains(extension);
                                }
                            }
                        }
                    }
                }
                #endregion FileType Treeview
                #endregion
            }
            var window = new NewIndexWindow();
            window.DataContext = viewModel;
            window.RefreshPluginDataSources(); // Initializes the Plugin Datasources
            var res = await window.ShowDialog<LuceneIndexConfiguration>(parent);
            if (window.DialogResult == TaskDialogResult.OK)
            {
                return new Tuple<TaskDialogResult, IIndexConfiguration>(window.DialogResult, res);
            } else
            {
                return new Tuple<TaskDialogResult, IIndexConfiguration>(window.DialogResult, null);
            }
        }
    }
}
