using Avalonia.Interactivity;
using eSearch.Models;
using eSearch.Models.Configuration;
using eSearch.Models.Search.Stemming;
using eSearch.Models.Search.Synonyms;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;


namespace eSearch.ViewModels
{
    public class SearchSettingsViewModel : ViewModelBase, INotifyDataErrorInfo
    {



        #region INotifyDataErrorInfo is for validation

        public List<ValidationError> ValidationErrors = new List<ValidationError>();

        public bool HasErrors
        {
            get
            {
                return ValidationErrors.Count > 0;
            }
        }

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged
        {
            add
            {
                _errorsChanged += value;
            }
            remove
            {
                _errorsChanged -= value;
            }
        }

        private event EventHandler<DataErrorsChangedEventArgs>? _errorsChanged;



        IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
        {
            Debug.WriteLine("Getting Errors for " + propertyName);
            int i = ValidationErrors.Count;
            while (i --> 0)
            {
                if (ValidationErrors[i].PropertyName == propertyName)
                {
                    return new[] { ValidationErrors[i].ErrorMsg };
                }
            }
            return null;
        }

        public void UpdateErrors()
        {
            _errorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(string.Empty));
        }

        #endregion


        /// <summary>
        /// Whether or not to use Search Term List File.
        /// </summary>
        public bool UseList
        {
            get
            {
                if (_useList == null)
                {
                    _useList = Program.ProgramConfig.SearchTermsListFile != null;
                }
                return (bool)_useList;
            }
            set
            {
                _useList = value;
                this.RaisePropertyChanged(nameof(UseList));
            }
        }

        private bool? _useList = null;


        public bool SearchAsYouType
        {
            get
            {
                if (_searchAsYouType == null)
                {
                    _searchAsYouType = Program.ProgramConfig.SearchAsYouType;
                }
                return (bool)_searchAsYouType;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _searchAsYouType, value);
            }
        }

        private bool? _searchAsYouType = null;


        public bool ListContentsOnEmptyQuery
        {
            get
            {
                if (_listContentsOnEmptyQuery == null)
                {
                    _listContentsOnEmptyQuery = Program.ProgramConfig.ListContentsOnEmptyQuery;
                }
                return (bool)_listContentsOnEmptyQuery;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _listContentsOnEmptyQuery, value);
            }
        }

        private bool? _listContentsOnEmptyQuery = null;

        /// <summary>
        /// Path of Search Term List file.
        /// </summary>
        public string ListPath
        {
            get
            {
                if (_listPath == null)
                {
                    if (!UseList) _listPath = "";
                    else
                    {
                        _listPath = Program.ProgramConfig.SearchTermsListFile;
                    }
                }
                return _listPath;
            }
            set
            {
                _listPath = value;
                this.RaisePropertyChanged(nameof(ListPath));
                if (ListPath != "")
                {
                    UseList = true;
                }
            }
        }

        private string? _listPath = null;


        public bool UseWordNet
        {
            get
            {
                if (_useWordNet == null)
                {
                    _useWordNet = Program.ProgramConfig.SynonymsConfig.UseEnglishWordNet;
                }
                return (bool)_useWordNet;
            }
            set
            {
                _useWordNet = value;
                this.RaisePropertyChanged(nameof(UseWordNet));
            }
        }

        private bool? _useWordNet = null;

        public bool UseSynonymFiles
        {
            get
            {
                if (IsUsingSynynons) return true; // TEMP - Whilst no WordNet

                if (_useSynonymFiles == null)
                {
                    _useSynonymFiles = Program.ProgramConfig.SynonymsConfig.UseSynonymFiles;
                }
                return (bool)_useSynonymFiles;
            }
            set
            {
                _useSynonymFiles = value;
                this.RaisePropertyChanged(nameof(UseSynonymFiles));
            }
        }

        private bool? _useSynonymFiles = null;

        public bool IsUsingSynynons
        {
            get
            {
                if (Program.GetMainWindow().DataContext is MainWindowViewModel vm)
                {
                    return vm.Session.Query.UseSynonyms;
                }
                return false;
            }
        }

        /*
        public string SelectedSynonymFilePath
        {
            get
            {

                return _selectedSynonymFilePath;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedSynonymFilePath, value);
            }
        }
        */

        private string _selectedSynonymFilePath = null;

        /// <summary>
        /// User Thesauri
        /// </summary>
        public List<SynonymsFileViewModel> SynonymsFiles
        {
            get
            {
                if (_synonymFiles == null)
                {
                    List<SynonymsFileViewModel> synonymsFiles = new List<SynonymsFileViewModel>();
                    // Ensure the eSearch Synonyms Directory exists.
                    Directory.CreateDirectory(Program.ESEARCH_SYNONYMS_DIR);
                    string[] files = Directory.GetFiles(Program.ESEARCH_SYNONYMS_DIR, "*.xml", SearchOption.TopDirectoryOnly);
                    foreach (string file in files)
                    {
                        bool active = Program.ProgramConfig.SynonymsConfig.ActiveSynonymFiles.Contains(System.IO.Path.GetFileName(file));

                        string displayName = Path.GetFileNameWithoutExtension(file);
                        if (displayName.StartsWith("UT"))
                        {
                            displayName = displayName.Substring(2);
                            displayName = displayName.Replace("_", " ");
                            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                            displayName = textInfo.ToTitleCase(displayName);
                        }

                        var synonymsFile = new SynonymsFileViewModel(file, displayName, active);
                        synonymsFiles.Add(synonymsFile);
                    }
                    _synonymFiles = synonymsFiles;
                }
                return _synonymFiles;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _synonymFiles, value);
            }
        }

        private List<SynonymsFileViewModel> _synonymFiles = null;

        /// <summary>
        /// Note these are file names only without extensions for presentation purposes.
        /// </summary>
        public ObservableCollection<StemmingRules> StemmingFiles
        {
            get
            {
                Debug.WriteLine("Stemming Files get");
                if (_stemmingFiles == null)
                {
                    _stemmingFiles = new ObservableCollection<StemmingRules>();
                    Directory.CreateDirectory(Program.ESEARCH_STEMMING_DIR);
                    string[] files = Directory.GetFiles(Program.ESEARCH_STEMMING_DIR, "*.dat", SearchOption.TopDirectoryOnly);
                    foreach(string file in files)
                    {
                        try
                        {
                            _stemmingFiles.Add(StemmingRules.FromFile(file));
                        } catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                    }
                }
                return _stemmingFiles;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _stemmingFiles, value);
            }
        }

        private ObservableCollection<StemmingRules> _stemmingFiles = null;

        /// <summary>
        /// Note this is the file name without extension for presentation purposes.
        /// </summary>
        public StemmingRules SelectedStemmingFile
        {
            get
            {
                if (_selectedStemmingFile == null)
                {
                    // TODO Load selected stemming file from Program Config
                    if (Program.ProgramConfig.StemmingConfig.StemmingFile != null)
                    {
                        foreach(var stemmingFile in StemmingFiles)
                        {
                            if (stemmingFile.FileName == Program.ProgramConfig.StemmingConfig.StemmingFile)
                            {
                                _selectedStemmingFile = stemmingFile;
                                break;
                            }
                        }
                    }
                }
                return _selectedStemmingFile;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedStemmingFile, value);
            }
        }

        private StemmingRules _selectedStemmingFile = null;

        public bool StemmingUseEnglishPorter
        {
            get
            {
                if (_stemmingUseEnglishPorter == null)
                {
                    _stemmingUseEnglishPorter = Program.ProgramConfig.StemmingConfig.UseEnglishPorter;
                }
                return (bool)_stemmingUseEnglishPorter;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _stemmingUseEnglishPorter, value);
            }
        }

        private bool? _stemmingUseEnglishPorter = null;

        public ObservableCollection<string> PhoneticAnalysers
        {
            get
            {
                if (_phoneticAnalysers == null)
                {
                    _phoneticAnalysers = new ObservableCollection<string>
                    {
                        S.Get("None"),
                        S.Get("Soundex"),
                        S.Get("Double Metaphone")
                    };
                }
                return _phoneticAnalysers;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _phoneticAnalysers, value);
            }
        }

        private ObservableCollection<string> _phoneticAnalysers = null;

        public string SelectedPhoneticAnalyser
        {
            get
            {
                if (_selectedPhoneticAnalyser == null)
                {
                    switch(Program.ProgramConfig.PhoneticConfig.SelectedEncoder)
                    {
                        case Models.Configuration.PhoneticConfig.Encoder.DoubleMetaphone:
                            _selectedPhoneticAnalyser = S.Get("Double Metaphone");
                            break;
                        case Models.Configuration.PhoneticConfig.Encoder.Soundex:
                            _selectedPhoneticAnalyser = S.Get("Soundex");
                            break;
                        default:
                            _selectedPhoneticAnalyser = S.Get("None");
                            break;
                    }
                }
                return _selectedPhoneticAnalyser;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedPhoneticAnalyser, value);
            }
        }

        private string _selectedPhoneticAnalyser = null;

        public PhoneticConfig.Encoder GetSelectedEncoder()
        {
            string strSelectedEncoder = SelectedPhoneticAnalyser;

            string doubleMetaphone = S.Get("Double Metaphone");
            string soundex = S.Get("Soundex");

            if (strSelectedEncoder == doubleMetaphone)
            {
                return PhoneticConfig.Encoder.DoubleMetaphone;
            }
            if (strSelectedEncoder == soundex)
            {
                return PhoneticConfig.Encoder.Soundex;
            }
            return PhoneticConfig.Encoder.None;
            // Wanted to do a switch here but doesn't work since strings aren't constant with i18n
        }



        public SynonymsFileViewModel SelectedSynonymFile
        {
            get
            {
                if (_selectedSynonymFile == null)
                {
                    string filePath = Program.ProgramConfig.SynonymsConfig.LastViewedSynonymsFile;
                    if (filePath != null)
                    {
                        foreach(var file in SynonymsFiles)
                        {
                            if (file.FilePath == filePath)
                            {
                                _selectedSynonymFile = file;
                            }
                        }
                    }
                }
                return _selectedSynonymFile;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedSynonymFile, value);
                this.RaisePropertyChanged(nameof(SynonymFileIsSelected));
                DisplayedSynonymGroups = new ObservableCollection<SynonymGroup>();
                if (value != null)
                {
                    DisplayedSynonymGroups = value.SynonymGroups;
                    
                }
                SelectedSynonymFile.IsActive = true;


            }
        }

        private SynonymsFileViewModel _selectedSynonymFile = null;

        public bool SynonymFileIsSelected
        {
            get
            {
                return (SelectedSynonymFile != null);
            }
        }

        public string[] GetSelectedSynonymFileNames()
        {
            List<string> synonymsFileNames = new List<string>();

            foreach(var synonymFile in SynonymsFiles)
            {
                if (synonymFile.IsActive)
                {
                    synonymsFileNames.Add(System.IO.Path.GetFileName(synonymFile.FilePath));
                }
            }

            return synonymsFileNames.ToArray();
        }

        

        public ObservableCollection<SynonymGroup> DisplayedSynonymGroups
        {
            get
            {
                if (_displayedSynonymGroups == null)
                {
                    if (SelectedSynonymFile != null)
                    {
                        _displayedSynonymGroups = SelectedSynonymFile.SynonymGroups;
                    }
                }
                return _displayedSynonymGroups;
            } set
            {
                _displayedSynonymGroups = value;
                SelectedSynonymGroup = null;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<SynonymGroup> _displayedSynonymGroups = null;

        public SynonymGroup? SelectedSynonymGroup
        {
            get
            {
                return _selectedSynonymGroup;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedSynonymGroup, value);
                _synonymsTextBoxText = null;
                this.RaisePropertyChanged(nameof(SynonymsTextBoxText));
                this.RaisePropertyChanged(nameof(IsSynonymGroupSelected));
            }
        }

        private SynonymGroup? _selectedSynonymGroup = null;

        public bool IsSynonymGroupSelected
        {
            get
            {
                return SelectedSynonymGroup != null;
            }
        }

        public string SynonymsTextBoxText
        {
            get
            {
                if (_synonymsTextBoxText == null && SelectedSynonymGroup != null)
                {
                    string[] synonymsArr = SelectedSynonymGroup?.Synonyms;
                    Debug.WriteLine("Did the join thing");
                    _synonymsTextBoxText = string.Join(Environment.NewLine, synonymsArr).Replace("\"", "");
                }
                return _synonymsTextBoxText;
            }
            set
            {
                _synonymsTextBoxText = value;
                string strSynonyms = value;
                if (strSynonyms != null)
                {
                    string txt = strSynonyms.Replace("\"", "");
                    string[] lines = txt.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    /*
                    int i = lines.Length;
                    while ( i --> 0)
                    {
                        if (lines[i].Trim().Contains(" "))
                        {
                            lines[i] = lines[i].Replace("\"","").Trim() ;
                        }
                    }
                    */
                    SelectedSynonymGroup?.SetSynonyms(lines);
                    Debug.WriteLine(string.Join('|', lines));
                }
                this.RaiseAndSetIfChanged(ref _synonymsTextBoxText, value);
            }
        }

        

        private string _synonymsTextBoxText = null;


    }
}
