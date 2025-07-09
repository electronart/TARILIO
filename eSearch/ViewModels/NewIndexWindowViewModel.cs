using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using eSearch.Interop;
using eSearch.Models;
using eSearch.Models.Configuration;
using eSearch.Models.DataSources;
using eSearch.Models.Documents;
using eSearch.Models.Indexing;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.ViewModels
{
    public class NewIndexWindowViewModel : ViewModelBase
    {

        

        Window myWindow;
        bool _updatingExisting = false;

        public NewIndexWindowViewModel(Window myWindow, bool updatingExisting = false)
        {
            this.myWindow = myWindow;
            this._updatingExisting = updatingExisting;
        }

        /// <summary>
        /// For designer only.
        /// </summary>
        public NewIndexWindowViewModel()
        {

        }


        public IndexSettingsWindowViewModel indexSettingsWindowViewModel = null;

        public ObservableCollection<IDataSource> DataSources
        {
            get { return _dataSources; }
            set
            {
                _dataSources = value;
                this.RaisePropertyChanged(nameof(DataSources));
            }
        }

        public bool UpdatingExistingIndex
        {
            get
            {
                return _updatingExisting;
            }
        }

        public async void RemoveDataSource(object dataSource)
        {
            if (dataSource is IDataSource ds2)
            {
                DataSources.Remove(ds2);
            }
        }

        private ObservableCollection<IDataSource> _dataSources = new();

        public IndexViewModel Index
        {
            get { return _index; }
            set
            {
                _index = value;
                this.RaisePropertyChanged(nameof(Index));
            }
        }



        private IndexViewModel _index = null;

        public ProgressViewModel IndexingTask
        {
            get { return _indexingTask; }
            set
            {
                _indexingTask = value;
                this.RaisePropertyChanged(nameof(IndexingTask));
            }
        }

        public String WindowTitle
        {
            get
            {
                if (_updatingExisting) return S.Get("Update Index");
                return S.Get("Create Index");
            }
        }

        private ProgressViewModel _indexingTask = null;


        public async void ClickIndexSettings()
        {
            IndexSettingsWindowViewModel viewModel = null;
            if (indexSettingsWindowViewModel == null)
            {
                if (_updatingExisting)
                {
                    viewModel = IndexSettingsWindowViewModel.FromIIndexConfig(Program.IndexLibrary.GetConfiguration(Index.GetIIndex()));
                } else
                {
                    viewModel = new IndexSettingsWindowViewModel(); // Initialize with defaults.
                }
            } else
            {
                viewModel = indexSettingsWindowViewModel;
            }


            var res = await IndexSettingsWindow.ShowDialog(viewModel, myWindow);
            if (res.Item1 == TaskDialogResult.OK)
            {
                indexSettingsWindowViewModel = res.Item2;
                // TODO Prompt to update/update.
            }
        }


        public async void ClickAddFolder()
        {
            string? res = null;

            var dialog = new OpenFolderDialog();
            dialog.Title = "Add Folder";

            res = await dialog.ShowAsync(myWindow);
            Debug.WriteLine("Dialog was shown");
            Debug.WriteLine(res);
            Debug.WriteLine("^^ Folder");

            string dir = res ?? "";
            if (!string.IsNullOrEmpty( dir ))
            {
                List < Models.Indexing.Directory > directories = new List<Models.Indexing.Directory>();
                directories.Add(new Models.Indexing.Directory(dir, true));
                var folderDataSource = new DirectoryDataSource(directories.ToArray());
                DataSources.Add(folderDataSource);
                Debug.WriteLine("Folder added");
            }
        }

        public async void ClickAddFile()
        {
            string? res = null;

            var files = await Program.GetMainWindow().StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                AllowMultiple = true,
                Title = "Select File(s)"
            });

            if (files.Count >= 1)
            {
                foreach(var  file in files)
                {
                    var fileDS = new FileDataSource { FilePath = file.Path.LocalPath.ToString() };
                    DataSources.Add(fileDS);
                }
            }
        }

        // This is the list of extensions that were initially selected but does not update as user edits selection.
        public List<string> InitiallySelectedExtensions = new List<string>();

        public ObservableCollection<TreeNode> TreeViewFileTypes
        {
            get
            {
                if (_treeViewFileTypes == null)
                {
                    _treeViewFileTypes = DocumentType.GetDocumentTypeTreeNodeHeirachy(InitiallySelectedExtensions);
                    foreach(var node in  _treeViewFileTypes)
                    {
                        node.IsChecked = true;
                        foreach(var subnode in node.SubNodes)
                        {
                            subnode.IsChecked = true;
                        }
                    }
                }
                return _treeViewFileTypes;
            }
        }

        private ObservableCollection<TreeNode> _treeViewFileTypes = null;

        public bool IsIncludeAllChecked
        {
            get
            {
                if (!Program.ProgramConfig.IsProgramRegistered())
                {
                    return true;
                }
                return _isIncludeAllChecked;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isIncludeAllChecked, value);
                if (value == true)
                {
                    foreach(var node in TreeViewFileTypes)
                    {
                        node.IsChecked = true;
                        foreach(var subnode in node.SubNodes)
                        {
                            subnode.IsChecked = true;
                        }
                    }
                }
            }
        }


        private bool _isIncludeAllChecked = true;

        public bool IsIncludeAllEnabled
        {
            get
            {
                return Program.ProgramConfig.IsProgramRegistered();
            }
        }

        public string ValidationError
        {
            get
            {
                return _validationError;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _validationError, value);
            }
        }

        private string _validationError = "";

        #region Max Word Length / Max File Size
        public int MaximumIndexedWordLength
        {
            get
            {
                return _maxIndexedWordLength;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _maxIndexedWordLength, value);
            }
        }

        private int _maxIndexedWordLength = 0;

        public int MaximumIndexedFileSizeMB
        {
            get
            {
                return _maxIndexedFileSizeMB;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _maxIndexedFileSizeMB, value);
            }
        }

        private int _maxIndexedFileSizeMB = 0;
        #endregion
    }
}