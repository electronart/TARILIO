using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.ViewModels
{
    public class CopyConversationWindowViewModel : ViewModelBase
    {

        private string documentTextToCopy = string.Empty;

        public string DocumentTextToCopy
        {
            get => documentTextToCopy;
            set => this.RaiseAndSetIfChanged(ref documentTextToCopy, value);
        }

        private bool appendNoteChecked = false;

        public bool AppendNoteChecked
        {
            get => appendNoteChecked;
            set => this.RaiseAndSetIfChanged(ref appendNoteChecked, value);
        }

        private string appendNoteText = string.Empty;

        public string DialogOKButtonText
        {
            get
            {
                return _dialogOKButtonText;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _dialogOKButtonText, value);
            }
        }

        private string _dialogOKButtonText = S.Get("Copy");

        public string AppendNoteText
        {
            get => appendNoteText;
            set => this.RaiseAndSetIfChanged(ref appendNoteText, value);
        }


        public bool AppendAIQueryChecked
        {
            get => _appendAIQueryChecked;
            set => this.RaiseAndSetIfChanged(ref _appendAIQueryChecked, value);
        }

        private bool _appendAIQueryChecked = true;

        public string AppendAIQueryText 
        { 
            get => _appendAIQueryText;
            set => this.RaiseAndSetIfChanged(ref _appendAIQueryText, value);
        }

        private string _appendAIQueryText = string.Empty;


        public enum CopySetting
        {
            Clipboard = 0,
            File = 1
        }

        private CopySetting copySetting;

        public CopySetting GetCopySetting()
        {
            return copySetting;
        }

        public bool IsRadioClipBoardChecked
        {
            get => copySetting == CopySetting.Clipboard;
            set
            {
                copySetting = CopySetting.Clipboard;
                this.RaisePropertyChanged(nameof(IsRadioClipBoardChecked));
                this.RaisePropertyChanged(nameof(IsRadioFileChecked));

            }
        }

        


        public bool IsRadioFileChecked
        {
            get => copySetting == CopySetting.File;
            set
            {
                copySetting = CopySetting.File;
                this.RaisePropertyChanged(nameof(IsRadioClipBoardChecked));
                this.RaisePropertyChanged(nameof(IsRadioFileChecked));
            }
        }

        private string copyToFileName = "eSearch";

        public string CopyToFileName
        {
            get => copyToFileName;
            set => this.RaiseAndSetIfChanged(ref copyToFileName, value);
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

        private bool _appendDateIsChecked = true;

        public string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "eSearch");

        public string SavePath
        {
            get => savePath;
            set => this.RaiseAndSetIfChanged(ref savePath, value);
        }
    }
}
