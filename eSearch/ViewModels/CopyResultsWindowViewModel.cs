using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class CopyResultsWindowViewModel : ViewModelBase
    {

        public bool IncludeSearchQuery
        {
            get
            {
                return _includeSearchQuery;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _includeSearchQuery, value);
            }
        }

        private bool _includeSearchQuery;

        public bool IncludeDataAndTime
        {
            get
            {
                return _includeDateTime;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _includeDateTime, value);
            }
        }

        private bool _includeDateTime;

        public bool CopyToFile
        {
            get
            {
                return _copyToFile;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _copyToFile, value);
            }
        }

        public bool AppendDateIsChecked
        {
            get
            {
                return _appendDateIsChecked;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _appendDateIsChecked, value);
            }
        }

        private bool _appendDateIsChecked = false;

        private bool _copyToFile;

        public string FileName
        {
            get
            {
                return _fileName;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _fileName, value);
            }
        }

        private string _fileName;

        public string FileDirectory
        {
            get
            {
                return _fileDirectory;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _fileDirectory, value);
            }
        }

        private string _fileDirectory;

    }
}
