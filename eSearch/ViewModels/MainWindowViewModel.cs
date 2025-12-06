
using Avalonia.Controls;
using eSearch.Models;
using eSearch.Models.Configuration;
using eSearch.Models.Indexing;
using eSearch.Models.Search;
using eSearch.Utils;
using eSearch.Views;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;
using DesktopSearch2.Views;
using eSearch.Converters;
using eSearch.Models.AI;
using System.Reflection;
using eSearch.ViewModels.StatusUI;


namespace eSearch.ViewModels
{

    [JsonObject(MemberSerialization.OptIn)]
    public class MainWindowViewModel : ViewModelBase
    {

        public event EventHandler ColumnSettingsChanged;


        [JsonProperty]
        public SessionViewModel Session { 
            get {
                if (_session == null)
                {
                    _session = SessionViewModel.LoadSession(Program.ESEARCH_SESSION_FILE);
                    _session.Query.PropertyChanged += Query_PropertyChanged;
                }
                return _session; 
            }
            set { _session = value; 
                this.RaisePropertyChanged(nameof(Session));
                Debug.WriteLine("RaisePropertyChanged: " + nameof(Session));
            }
        }

        private SessionViewModel _session = null;

        // Don't persist this.
        // It stops repeated searches during initialisation reading from json file
        public bool Initializing = true;

        public WheelViewModel? Wheel
        {
            get
            {
                return _wheel;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _wheel, value);
            }
        }

        private WheelViewModel? _wheel = null;



        public string ProductTagText
        {
            get
            {
                return Program.ProgramConfig.GetProductTagText();
            }
        }

        public bool AreRAABRadiosEnabled
        {
            get
            {
                return _areRAABRadiosEnabled;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _areRAABRadiosEnabled, value);
            }
        }

        private bool _areRAABRadiosEnabled = false;

        public TrulyObservableCollection<ProgressViewModel> OngoingTasks
        {
            get { return _ongoingTasks; }
            set { _ongoingTasks = value; 
                this.RaisePropertyChanged(nameof(OngoingTasks));
            }
        }

        private TrulyObservableCollection<ProgressViewModel> _ongoingTasks = new();

        public IVirtualReadOnlyObservableCollectionProvider<ResultViewModel> Results
        {
            get {;
                return _results;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _results, value);
            }
        }

        public bool IsThemeHighContrast
        {
            get
            {
                return Program.ProgramConfig.IsThemeHighContrast;
            }
        }


        private IVirtualReadOnlyObservableCollectionProvider<ResultViewModel> _results = new EmptySearchResultsProvider(); // Dummy.

        public ObservableCollection<ResultViewModel> SelectedResults
        {
            get
            {
                return _selectedResults;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedResults, value);
            }
        }

        private ObservableCollection<ResultViewModel> _selectedResults = new();


        

        private IndexProgressWindow? _indexProgressWindow = null;

        public IndexLibrary IndexLibrary
        {
            get
            {
                return Program.IndexLibrary;
            }
        }

        private ResultViewModel? _selectedResult = null;

        public ResultViewModel? SelectedResult
        {
            get => _selectedResult;
            set => this.RaiseAndSetIfChanged(ref _selectedResult, value);
        }

        public LLMConversationViewModel? CurrentLLMConversation
        {
            get
            {
                return _currentLLMConversation;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _currentLLMConversation, value);
            }
        }


        public void ToggleLaunchAtStartup()
        {
            Program.ProgramConfig.LaunchAtStartup = !Program.ProgramConfig.LaunchAtStartup;
            Program.SaveProgramConfig();
            IsLaunchAtStartupEnabled = Program.ProgramConfig.LaunchAtStartup;
        }

        public bool IsLaunchAtStartupEnabled
        {
            get
            {
                if (_isLaunchAtStartupEnabled == null)
                {
                    _isLaunchAtStartupEnabled = Program.ProgramConfig.LaunchAtStartup;
                }
                return (bool)_isLaunchAtStartupEnabled;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isLaunchAtStartupEnabled, value);
            }
        }

        private bool? _isLaunchAtStartupEnabled = null; // Null until loaded.

        

        private LLMConversationViewModel? _currentLLMConversation;

        public bool ShowHitNavigation
        {
            get
            {
                return _showHitNavigation;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _showHitNavigation, value);
            }
        }

        private bool _showHitNavigation = false;

        public int CurrentDocHitCount
        {
            get
            {
                return _currentDocHitCount;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _currentDocHitCount, value);
            }
        }

        private int _currentDocHitCount = 0;

        public int CurrentDocSelectedHit
        {
            get
            {
                return _currentDocSelectedHit;
            }
            set
            {
                _currentDocSelectedHit = value;
                this.RaisePropertyChanged(nameof(CurrentDocSelectedHit)); // Always raise changed, even if not because this will scroll hit into view.
            }
        }

        private int _currentDocSelectedHit = 1;

        public async void ShowBrowserDebug()
        {
            if (Program.GetMainWindow() is MainWindow window)
            {
                window.ShowBrowserDebug();
            }
        }


        public bool IsGridSplitterHidden
        {
            get
            {
                return _isGridSplitterHidden;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isGridSplitterHidden, value);
            }
        }

        private bool _isGridSplitterHidden = false;

        public async void NextHit()
        {
            if (CurrentDocSelectedHit < CurrentDocHitCount)
            {
                ++CurrentDocSelectedHit;
            } else
            {
                CurrentDocSelectedHit = 1; // Loop around.
            }
        }

        public async void PrevHit()
        {
            if (CurrentDocSelectedHit > 1)
            {
                CurrentDocSelectedHit--;
            } else
            {
                CurrentDocSelectedHit = CurrentDocHitCount; // Loop around.
            }
        }

        public bool ShowRegistrationPromptPanel
        {
            get
            {
                return Program.ProgramConfig.IsProgramRegistered() == false;
            }
        }

        public async void MenuDebugExpandPaths()
        {
            string Location = "Indexes\\askd-flghsdaklf-h";
            string libFileLoc = Program.ESEARCH_INDEX_LIB_FILE ?? "";
            string libDir = Path.GetDirectoryName(libFileLoc);
            string absPath = Path.GetFullPath(Location, libDir);

            string debugMsg = $"Location: {Location}\nlibFileLoc: {libFileLoc}\nlibDir: {libDir}\nabsPath: {absPath}";
            TaskDialogWindow.OKDialog("DEBUG", debugMsg, Program.GetMainWindow());
            
        }

        public async void ClickRegister()
        {
            var dialogRes = await RegistrationWindow.ShowDialog();
            if (dialogRes == TaskDialogResult.OK)
            {
                this.RaisePropertyChanged(nameof(ShowRegistrationPromptPanel));
            }
        }


        public void UpdateLayout()
        {
            this.RaisePropertyChanged(nameof(SelectedLayout));
            this.RaisePropertyChanged(nameof(IsLayoutHorizontal));
            this.RaisePropertyChanged(nameof(IsLayoutVertical));
        }

        [JsonProperty]
        public LayoutPreference SelectedLayout
        {
            get
            {
                if (_layoutPreference == null)
                {
                    _layoutPreference = LayoutPreference.Vertical;
                }
                return (LayoutPreference)_layoutPreference;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _layoutPreference, value);
                this.RaisePropertyChanged(nameof(IsLayoutHorizontal));
                this.RaisePropertyChanged(nameof(IsLayoutVertical));

            }
        }


        public bool IsLayoutVertical
        {
            get
            {
                return SelectedLayout == LayoutPreference.Vertical;
            }
        }

        public bool IsLayoutHorizontal
        {
            get
            {
                return SelectedLayout == LayoutPreference.Horizontal;
            }
        }

        private LayoutPreference? _layoutPreference = null;

        public enum LayoutPreference
        {
            Horizontal,
            Vertical
        }

        #region Searchtype radio handling
        /*
         *  TODO Must be some better way to handle the radios...
         */
        public bool IsRadioRAABAllWordsChecked
        {
            get
            {
                return Session.Query.SelectedSearchType == QueryViewModel.SearchType.AllWords;
            }
            set
            {
                Debug.WriteLine("AllWordsChecked " + value);
                if (value) Session.Query.SelectedSearchType = QueryViewModel.SearchType.AllWords;
            }
        }

        public bool IsRadioRAABAnyWordsChecked
        {
            get
            {
                return Session.Query.SelectedSearchType == QueryViewModel.SearchType.AnyWords;
            }
            set
            {
                Debug.WriteLine("AnyWordsChecked " + value);
                if (value) Session.Query.SelectedSearchType = QueryViewModel.SearchType.AnyWords;
            }
        }

        public bool IsRadioRAABBoolChecked
        {
            get
            {
                return Session.Query.SelectedSearchType == QueryViewModel.SearchType.Boolean;
            }
            set
            {
                Debug.WriteLine("Bool Checked " + value);
                if (value) Session.Query.SelectedSearchType = QueryViewModel.SearchType.Boolean;
            }
        }
        #endregion

        public List<IIndex> AvailableIndexes
        {
            get
            {
                return IndexLibrary.GetAllIndexes();
            }
        }

        public IIndex? SelectedIndex
        {
            get
            {
                string? selected_id = Session.SelectedIndexId;
                List<IIndex> indexes = IndexLibrary.GetAllIndexes();
                if (selected_id != null)
                {
                    foreach(var index in indexes)
                    {
                        if (index != null)
                        {
                            if (index.Id == selected_id)
                            {
                                return index;
                            }
                        }
                    }
                }
                if (indexes.Count > 0) return indexes[0];
                return null;
            }
            set
            {
                bool changed = false;
                if (value?.Id != Session.SelectedIndexId) changed = true;
                Session.SelectedIndexId = value?.Id ?? null;
                if (changed) this.RaisePropertyChanged(nameof(SelectedIndex));
            }
        }

        public SearchSource? SelectedSearchSource
        {
            get
            {
                return _searchSource;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _searchSource, value);
            }
        }

        private SearchSource? _searchSource = null;

        public ObservableCollection<SearchSource> AvailableSearchSources
        {
            get
            {
                return _availableSearchSources;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _availableSearchSources, value);
            }
        }

        private ObservableCollection<SearchSource> _availableSearchSources = new ObservableCollection<SearchSource>();


        public bool IsIndexLoading
        {
            get
            {
                return _isIndexLoading;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isIndexLoading, value);
            }
        }

        private bool _isIndexLoading = false;

        public StringPair DocCopyToolTipTextPair
        {
            get
            {
                return new StringPair
                {
                    TrueString = S.Get("Copy Response..."),
                    FalseString = S.Get("Copy Document...")
                };
            }
        }

        [JsonProperty]
        public bool ShowMetadataPanel
        {
            get
            {
                return _showMetadataPanel;
            }
            set
            {
                _showMetadataPanel = value;
                this.RaisePropertyChanged(nameof(ShowMetadataPanel));
                if (value)
                {
                    ShowDocumentLocation = false;
                }
            }
        }

        private bool _showMetadataPanel;

        [JsonProperty]
        public bool ShowWordWheel
        {
            get
            {
                return _showWordWheel;
            }
            set
            {
                _showWordWheel = value;
                this.RaisePropertyChanged(nameof(ShowWordWheel));
            }
        }

        private bool _showWordWheel = true;

        public async Task<bool> ToggleWheel()
        {
            ShowWordWheel = !ShowWordWheel;
            Debug.WriteLine("WordWheel Shown: " +  ShowWordWheel);
            return true;
        }

        public bool IsVoiceInputActive
        {
            get
            {
                return _isVoiceInputActive;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isVoiceInputActive, value);
            }
        }

        private bool _isVoiceInputActive = false;

        public async void MenuFileExit()
        {
            Program.GetMainWindow().Close();
        }

        public async void MenuFilePrint()
        {
            var window = Program.GetMainWindow();
            if (window is MainWindow mainWindow)
            {
                mainWindow.PrintViewedDocument();
            }
        }


        private IndexLibrary _indexLibrary;


        public void ClickTest2()
        {
            ProgressViewModel pvm1 = new ProgressViewModel() { Progress = 20, Status = "Hello!" };
            ProgressViewModel pvm2 = new ProgressViewModel() { Progress = 50, Status = "Yo!" };
            OngoingTasks.Add(pvm1);
            OngoingTasks.Add(pvm2);

        }

        public void ClickRandomizeProgress()
        {
            foreach(var task in OngoingTasks)
            {
                Random r = new Random();
                int rInt = r.Next(0, 100);
                task.Progress = rInt;
                task.Status = "Randomized";
            }
        }

        public async Task<bool> ClickWheelListOptions()
        {
            var wheelListOptionsDialog = new ListContentsWindow();
            wheelListOptionsDialog.DataContext = new TaskDialogWindowViewModel();
            await wheelListOptionsDialog.ShowDialog(Program.GetMainWindow());
            return true;
        }

        public async Task<bool> ClickDocumentSettings()
        {
            var res = await ViewerSettingsWindow.ShowDialog();
            if (res.Item1 == TaskDialogResult.OK)
            {
                var newSettings = res.Item2;


                Program.ProgramConfig.ViewerConfig.PDFViewerOption          = newSettings.OptionPDFViewer;
                Program.ProgramConfig.ViewerConfig.MaxFileSizeMB            = newSettings.ViewerMaxFileSizeMB;
                Program.ProgramConfig.ViewerConfig.ViewLargeFileOption      = newSettings.OptionViewLargeFiles;
                Program.ProgramConfig.ViewerConfig.ReportViewContextAmount  = newSettings.ReportViewContextAmount;
                Program.ProgramConfig.ViewerConfig.ReportViewContextTypeOption = newSettings.OptionReportViewContextType;
                Program.SaveProgramConfig();
                // Cause the result to be refreshed with the new viewer settings.
                var priorResult = SelectedResult;
                SelectedResult = null;
                SelectedResult = priorResult;
            }
            return true;
        }

        public async void MenuDebugDialogTest()
        {
            await TaskDialogWindow.OKDialog("This is a very long instruction that is likely to end up getting pushed off the side of the dialog",
                                                       "This is a very long content string which is equally likely to end up getting pushed off the side of the dialog. The text needs to be wrapped instead", Program.GetMainWindow());

        }

        #region Document Location Logic

        public string DocumentLocationButtonToolTip
        {
            get
            {
                return _documentLocationButtonToolTip;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _documentLocationButtonToolTip, value);
            }
        }

        private string _documentLocationButtonToolTip = "";


        public bool ShowDocumentLocation
        {
            get
            {
                return _showDocumentLocation;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _showDocumentLocation, value);
                if (value)
                {
                    ShowMetadataPanel = false;
                }
            }
        }

        private bool _showDocumentLocation = false;

        public bool IsDocumentLocationAvailable
        {
            get
            {
                return _isDocumentLocationAvailable;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isDocumentLocationAvailable, value);
            }
        }

        private bool _isDocumentLocationAvailable = false;

        #endregion




        public async Task<bool> ClickDocumentViewInFolder()
        {
            if (SelectedResult == null) return true; // illegal.
            string fileName = SelectedResult.FilePath;
            if (File.Exists(fileName))
            {
                Models.Utils.RevealInFolderCrossPlatform(fileName);
            } else
            {
                TaskDialogWindow.OKDialog(
                    S.Get("File not found"),
                    S.Get("File \"%s\" not found").Replace("%s", fileName),
                    Program.GetMainWindow()
                );
            }
            return true;
        }


        public ObservableCollection<DataColumn> Columns
        {
            get
            {
                if (_columns == null)
                {
                    return new ObservableCollection<DataColumn>();
                    if (SelectedIndex == null)
                    {
                        _columns = new ObservableCollection<DataColumn>();
                    }
                    else
                    {
                        var config = IndexLibrary.GetConfiguration(SelectedIndex);
                        _columns = config.ColumnDisplaySettings;
                    }
                }
                return _columns;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _columns, value);
            }
        }

        private ObservableCollection<DataColumn> _columns;


        public async Task<bool> ClickCopyResults()
        {
            return true; // TODO this method is no longer used need to remove the bindings.
            //var copyResultsDialog = new CopyResultsWindow();
            //await copyResultsDialog.ShowDialog(Program.GetMainWindow());
            //return true;
        }

        public async void ClickSearchFilters()
        {

            //var res = await SearchFilterWindow2.ShowDialog(Program.GetMainWindow(), )

            /*
            List<string> fieldNames = new List<string>();
            foreach(var column in Columns)
            {
                fieldNames.Add(column.Header);
            }
            var res = await SearchFilterWindow.ShowDialog(Program.GetMainWindow(), Session.Query.QueryFilters, fieldNames);
            if (res.Item1 == TaskDialogResult.OK)
            {
                var vm = res.Item2;
                Session.Query.QueryFilters.Clear();
                foreach(var filter in vm.QueryFilters)
                {
                    Session.Query.QueryFilters.Add(new DesktopSearch2.Models.Search.QueryFilter
                    {
                        FieldName = filter.SelectedField,
                        Type = DesktopSearch2.Models.Search.QueryFilter.FilterType.Text,
                        SearchText = filter.SearchText
                    });
                }
                UpdateSearchResults();
            }
            */
            

        }

        public void DebugSetupSearchSession()
        {
            /*
            LuceneIndex index = new LuceneIndex("Test", "Ignored", "Ignored", @"C:\Users\Tommer\source\repos\eSearch\Test Indexes\Test Data Index", 100, null);
            IndexViewModel indexVM = new IndexViewModel(index);
            Session = new();
            Session.Query = new QueryViewModel();
            Session.Query.Query = "Hello World!";
            Session.Query.PropertyChanged += Query_PropertyChanged;
            Session.Indexes.Add(indexVM);
            Session.Index = indexVM;
            Debug.WriteLine("Index added.");
            */

        }

        private async void Query_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            
        }

        public bool IsAISearchButtonEnabled
        {
            get
            {
                return (!Program.WasLaunchedWithAIDisabledArgument);
            }
        }

        public bool IsAIConnectionsMenuEnabled
        {
            get
            {
                return (!Program.WasLaunchedWithCreateLLMConnectionsDisabled);
            }
        }

        public void ToggleSystemPrompt()
        {
            Session.Query.ShowSystemPrompt = !Session.Query.ShowSystemPrompt;
        }

        public bool LocalLLMIsModelLoading
        {
            get
            {
                return _localLLMIsModelLoading;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _localLLMIsModelLoading, value);
            }
        }

        private bool _localLLMIsModelLoading = false;

        public float LocalLLMModelLoadProgress
        {
            get
            {
                return _localLLMModelLoadProgress;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _localLLMModelLoadProgress, value);
            }
        }

        private float _localLLMModelLoadProgress = 0.0f;

        public void ClickClear()
        {
            OngoingTasks.Clear();
        }

        public async void ClickSearchSubmit()
        {
            if (Program.GetMainWindow() is MainWindow window)
            {
                window.UpdateSearchResults(); // TODO HACK Breaks MWVM Pattern
            }
        }

        public async Task<bool> OnClickSearchSettings()
        {
            var taskRes = await SearchSettingsWindow.ShowDialog();
            var dialogResult = taskRes.Item1;
            if (dialogResult == Models.TaskDialogResult.OK)
            {
                var newSettings = taskRes.Item2;
                // Clicked OK. Save new preferences.

                #region Save Synonyms Config
                Program.ProgramConfig.SynonymsConfig.ActiveSynonymFiles     = newSettings.GetSelectedSynonymFileNames().ToList();
                Program.ProgramConfig.SynonymsConfig.UseSynonymFiles        = newSettings.UseSynonymFiles;
                Program.ProgramConfig.SynonymsConfig.UseEnglishWordNet      = newSettings.UseWordNet;
                Program.ProgramConfig.SynonymsConfig.LastViewedSynonymsFile = newSettings.SelectedSynonymFile?.FilePath;
                Program.ProgramConfig.SearchAsYouType = newSettings.SearchAsYouType;
                // Save Changes to Synonyms Files
                foreach (var synonymsFile in newSettings.SynonymsFiles)
                {
                    synonymsFile.SaveChanges();
                }
                #endregion
                #region Save Stemming Config
                string selectedStemmingFile = null;
                if (newSettings.SelectedStemmingFile != null)
                {
                    selectedStemmingFile = Path.Combine(Program.ESEARCH_STEMMING_DIR, newSettings.SelectedStemmingFile.FileName );
                }

                Program.ProgramConfig.StemmingConfig.UseEnglishPorter   = newSettings.StemmingUseEnglishPorter;
                Program.ProgramConfig.StemmingConfig.StemmingFile       = selectedStemmingFile;

                #endregion
                #region Save Phonetics Config
                Program.ProgramConfig.PhoneticConfig.SelectedEncoder = newSettings.GetSelectedEncoder();
                #endregion
                #region Save Search Term List Config
                if (newSettings.UseList)
                {
                    Program.ProgramConfig.SearchTermsListFile = newSettings.ListPath;
                    Session.Query.QueryListFilePath = newSettings.ListPath;

                } else
                {
                    Program.ProgramConfig.SearchTermsListFile = null;
                    Session.Query.QueryListFilePath = null;
                }
                #endregion
                Program.ProgramConfig.ListContentsOnEmptyQuery = newSettings.ListContentsOnEmptyQuery;
                Program.SaveProgramConfig();
                Session.Query.InvalidateCachedThesauri();
                Session.Query.StemmingRules = Program.ProgramConfig.StemmingConfig.LoadActiveStemmingRules(); // Reload stemming rules.
                // Finally, perform a new query by indicating the query has changed.
                if (Program.GetMainWindow() is MainWindow window)
                {
                    window.UpdateSearchResults(); // TODO quick HACK breaks MWVM pattern.
                }
            }
            return true;
        }

        public async Task<bool> MenuFileExportSearchResults()
        {
            
            var res = await ExportSearchResultsWindow.ShowDialog(Program.GetMainWindow(), Columns.ToArray());
            var dialogResult = res.Item1;
            if (dialogResult == TaskDialogResult.OK)
            {
                // User wants to export search results.
                var exportSettings = res.Item2;

                var outputType = exportSettings.OutputFileTypes[exportSettings.SelectedOutputFileTypeIndex];
                Program.ProgramConfig.ExportConfig = ExportConfig.FromExportResultsVM(exportSettings);
                Program.SaveProgramConfig();

                string fileName = exportSettings.FileNameInput;
                if (exportSettings.AppendDateChecked) {
                    fileName += " " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
                }
                string outputDir = exportSettings.OutputDirectoryInput;
                string savePath = Path.Combine(outputDir, fileName);
                exportSettings.ExportResultsBasedOnSettings(Results, savePath);
            }
            return true;
        }

        public async Task<bool> HelpAboutApplication()
        {
            var aboutDialog = new AboutWindow();
            aboutDialog.DataContext = new AboutWindowViewModel();
            var res = await aboutDialog.ShowDialog<object>(Program.GetMainWindow());
            return true;
        }

        public async void PressF1()
        {
            string? exeDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? string.Empty);
            string helpFilePath = Path.Combine(exeDir ?? "", "help", "Tarilio-User-Guide.pdf");
            if (File.Exists(helpFilePath))
            {
                var uri = new System.Uri(helpFilePath);
                var url = uri.AbsoluteUri;
                Models.Utils.CrossPlatformOpenBrowser(url);
            }
            else
            {
                await TaskDialogWindow.OKDialog(S.Get("File not found"), "Expected Location: " + helpFilePath, Program.GetMainWindow());
            }
        }

        public async Task<bool> MenuFileSearchReport()
        {
            var searchReportWindow = new SearchResultsReportWindow();
            searchReportWindow.DataContext = Session;
            await searchReportWindow.ShowDialog(Program.GetMainWindow());
            return true;
        }

        public void MenuAppearanceDark()
        {
            Program.SetTheme(false);
            if (Program.GetMainWindow() is MainWindow w)
            {
                w.UpdateTheme(); 
            }
        }

        public void MenuAppearanceLight()
        {
            Program.SetTheme(true);
            if (Program.GetMainWindow() is MainWindow w)
            {
                w.UpdateTheme();
            }
        }

        public void MenuLayoutHorizontal()
        {
            SelectedLayout = LayoutPreference.Horizontal;
        }

        public void MenuLayoutVertical()
        {
            SelectedLayout = LayoutPreference.Vertical;
        }

        public async void DebugExportSearchReport()
        {
            // SearchReportGenerator.GenerateSearchReport(Session.Query, Results);
        }

        public ObservableCollection<StatusControlViewModel> StatusMessages { 
            get
            {
                return _statusMessages;
            } 
            set
            {
                this.RaiseAndSetIfChanged(ref _statusMessages, value);
            }
        }

        private ObservableCollection<StatusControlViewModel> _statusMessages = new ObservableCollection<StatusControlViewModel>();

        /*
        public void MenuAppearanceSystem()
        {
            // TODO.
        }
        */

        public void MenuLanguageEnglish()
        {
            Program.LoadLang(null);
            Program.SetPreferredLanguage(null);
        }

        public void MenuLanguagePsuedo()
        {
            Program.PsuedoLang();
            //Program.LoadLang(Program.DEBUG_TEST_LANG_PATH);
        }

        public async void MenuLanguageFromFile()
        {
            System.IO.Directory.CreateDirectory(Program.ESEARCH_i18N_DIR); // Ensure directory exists.
            var dialog = new OpenFileDialog();
            dialog.Title = "Open Language File";
            FileDialogFilter filter = new FileDialogFilter();
            filter.Extensions.Add("lang");
            filter.Name = "Language files (*.lang)";
            dialog.Filters = new List<FileDialogFilter> { filter };
            dialog.Directory = Program.ESEARCH_i18N_DIR;
            
            var res = await dialog.ShowAsync(Program.GetMainWindow());
            if (res != null && res.Length > 0)
            {
                Program.LoadLang(res[0]);
                Program.SetPreferredLanguage(res[0]);
            }
        }

        private void IndexWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            IndexLibrary.SaveLibrary(); // This is to save any newly known fields.
            if (_indexProgressWindow != null)
            {
                _indexProgressWindow.Close();
                _indexProgressWindow = null;
            }
            if (e.Result is IIndex createdIndex)
            {
                Session.SelectedIndexId = createdIndex.Id;
                this.RaisePropertyChanged(nameof(IndexLibrary));
                this.RaisePropertyChanged(nameof(SelectedIndex));
                this.RaisePropertyChanged(nameof(AvailableIndexes));
            }
            Debug.WriteLine("Indexing Task Complete!");
        }


        private void IndexWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("Executing Index Task");
            
            try
            {
                IndexTask indexTask = (IndexTask)e.Argument;
                Debug.WriteLine("Got Index Task");
                if (indexTask != null)
                {
                    Debug.WriteLine("Not Null");
                    indexTask.Execute(); // Thread blocking task... could be seconds, could be minutes, could be hours.
                    e.Result = true;
                    e.Result = indexTask.GetIndex();
                }
                else throw new Exception("Index Task null?");
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                e.Result = ex;
            }
        }
    }
}