using com.sun.tools.corba.se.idl;
using eSearch.Models.Configuration;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class IndexSettingsWindowViewModel : ViewModelBase
    {

        public static IndexSettingsWindowViewModel FromIIndexConfig(IIndexConfiguration config)
        {
            var vm = new IndexSettingsWindowViewModel();
            if (config.SelectedStopWordFiles != null && config.SelectedStopWordFiles.Count > 0)
            {
                vm.SelectedStopWordFileName = config.SelectedStopWordFiles[0];
            }

            vm.MaximumIndexedFileSizeMB = config.MaximumIndexedFileSizeMB;
            vm.MaximumIndexedWordLength = config.MaximumIndexedWordLength;
            vm.IsIndexCaseSensitive     = config.IsIndexCaseSensitive;
            return vm;
        }

        public void ApplyToIndexConfig(IIndexConfiguration indexConfig)
        {
            indexConfig.SelectedStopWordFiles = new List<string>();
            if (SelectedStopWordFileName != null)
            {
                indexConfig.SelectedStopWordFiles.Add(SelectedStopWordFileName);
            }
            indexConfig.MaximumIndexedWordLength = MaximumIndexedWordLength;
            indexConfig.MaximumIndexedFileSizeMB = MaximumIndexedFileSizeMB;
            indexConfig.IsIndexCaseSensitive = IsIndexCaseSensitive;
        }

        #region Stop Words
        public List<string> AvailableStopWordFileNames
        {
            get
            {
                string dir = Program.ESEARCH_STOP_FILE_DIR;
                string[] files = System.IO.Directory.GetFiles(dir, "*.dat");
                List<string> fileNames = new List<string>();
                foreach (string file in files)
                {
                    fileNames.Add(Path.GetFileNameWithoutExtension(file));
                }
                fileNames.Sort();
                int noneIdx = fileNames.IndexOf("None");
                if (noneIdx != -1)
                {
                    fileNames.RemoveAt(noneIdx);
                    fileNames.Insert(0, "None");
                }

                return fileNames;
            }
        }

        public string? SelectedStopWordFileName
        {
            get
            {
                if (_selectedStopWordFileName == null)
                {
                    _selectedStopWordFileName = "None";
                }
                return _selectedStopWordFileName;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedStopWordFileName, value);
            }
        }

        private string? _selectedStopWordFileName = "English";
        #endregion

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

        #region Case Sensitivity

        public bool IsIndexCaseSensitive
        {
            get
            {
                return _isIndexCaseSensitive;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isIndexCaseSensitive, value);
            }
        }

        private bool _isIndexCaseSensitive = false;

        #endregion

    }
}