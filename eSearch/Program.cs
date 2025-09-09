using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using System;
using System.IO;
using Avalonia.Themes.Fluent;
using Avalonia.Platform;
using System.Diagnostics;
using eSearch.ViewModels;
using eSearch.Models.Localization;
using Xilium.CefGlue.Common;
using System.Collections.Generic;
using System.Linq;
using eSearch.Models.Documents.Parse;
using System.Reflection;
using System.Text;
using eSearch.Models.Configuration;
using Newtonsoft.Json;
using Avalonia.Controls.Primitives;
using System.Timers;
using ReactiveUI;
using com.sun.istack.@internal;
using eSearch.Models.Plugins;
using S = eSearch.ViewModels.TranslationsViewModel;
using eSearch.Interop.AI;
using eSearch.Models.AI.MCP.Tools;
using Xilium.CefGlue;
using DynamicData;
using UglyToad.PdfPig.Fonts.TrueType.Names;
using eSearch.Interop;
using eSearch.Models.Logging;
using eSearch.Models.Indexing;
using eSearch.Interop.Indexing;
using System.Threading;
using Timer = System.Timers.Timer;
using ProgressCalculation;
using eSearch.Models.AI;
using System.Threading.Tasks;

namespace eSearch
{
    internal class Program
    {

        static Timer timer;

        /// <summary>
        /// When not null, this is a scheduled index update task, the application should run in the background without disturbing the user.
        /// </summary>
        public static string? WasLaunchedAsScheduledIndexUpdate        = null;

        public static bool WasLaunchedWithSearchOnlyArgument           = false;

        public static bool WasLaunchedWithAIDisabledArgument           = false;

        public static bool WasLaunchedWithCreateLLMConnectionsDisabled = false;


        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            WasLaunchedWithSearchOnlyArgument =           args.Contains("-s");
            WasLaunchedWithAIDisabledArgument =           args.Contains("-a");
            WasLaunchedWithCreateLLMConnectionsDisabled = args.Contains("-x");
            #region Check if the application has been launched as a scheduled index update
            int indexOfScheduledArg = args.IndexOf("--scheduled");
            if (indexOfScheduledArg != -1)
            {
                if (args.Length > indexOfScheduledArg + 1)
                {
                    string indexId = args[args.IndexOf("--scheduled") + 1];
                    WasLaunchedAsScheduledIndexUpdate = indexId;
                    var index = Program.IndexLibrary.GetIndexById(indexId);
                    if (index == null)
                    {
                        throw new ArgumentException("Unrecognized IndexID");
                    }
                    RunScheduledIndexUpdate(index);
                    return;
                } else
                {
                    throw new ArgumentException("`--scheduled` argument passed, but no IndexID supplied");
                }
            }
            #endregion
            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
            timer = new Timer(60000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private static System.Timers.Timer  _scheduledIndexProgressReportTimer;
        private static ProgressViewModel    _scheduledIndexProgressVM;
        private static ILogger              _scheduledIndexLogger;

        private static void RunScheduledIndexUpdate(IIndex index)
        {
            

            DateTime started = DateTime.Now;
            List<ILogger> loggers = new List<ILogger>();
#if DEBUG
            loggers.Add(new DebugLogger());
#endif
            var indexTaskLog = new IndexTaskLog();
            loggers.Add(indexTaskLog);
            if (OperatingSystem.IsWindows())
            {
                WindowsEventViewerLogger winLogger = new WindowsEventViewerLogger(index);
                loggers.Add(winLogger);
            }
            _scheduledIndexLogger = new MultiLogger(loggers);
            try
            {

                _scheduledIndexLogger.Log(ILogger.Severity.INFO, String.Format(S.Get("Scheduled Index of {0} is starting..."), index.Name));
                var indexConfig = Program.IndexLibrary.GetConfiguration(index);
                if (indexConfig == null)
                {
                    throw new Exception("Could not find index configuration");
                }
                #region Attempt the Index Task. Opening it may fail if the user is already writing to it, so there is retry logic for this.
            retryOpenIndex:
                int openAttempts = 0;
                try
                {
                    _scheduledIndexProgressReportTimer = new System.Timers.Timer(TimeSpan.FromMinutes(2));
                    _scheduledIndexProgressReportTimer.Elapsed += _scheduledIndexProgressReportTimer_Elapsed;
                    _scheduledIndexProgressReportTimer.AutoReset = true; // repeat
                    _scheduledIndexProgressReportTimer.Start();
                    _scheduledIndexProgressVM = new ProgressViewModel();
                    IndexTask task = new IndexTask(indexConfig.GetMainDataSource(), index, _scheduledIndexProgressVM, false, true, _scheduledIndexLogger);
                    task.ResumeIndexing();
                    task.Execute(true); // BLOCKING - This one call will keep this thread blocked potentially hours depending on whats to be indexed.
                                    // It may also throw FailedToOpenIndexException 
                    
                }
                catch (FailedToOpenIndexException ex)
                {
                    if (openAttempts < 3)
                    {
                        ++openAttempts;
                        _scheduledIndexLogger.Log(ILogger.Severity.WARNING, $"Failed to open index. It may be locked. Will try again in 5 minutes... (Attempt {openAttempts}/3)", ex);
                        Thread.Sleep(TimeSpan.FromMinutes(5));
                        goto retryOpenIndex;
                    }
                    else
                    {
                        _scheduledIndexLogger.Log(ILogger.Severity.ERROR, "Could not open the index after 3 attempts. The scheduled index task was cancelled.", ex);
                    }
                } finally
                {
                    _scheduledIndexProgressReportTimer.Stop();
                    _scheduledIndexProgressReportTimer.Dispose();
                }
                #endregion
            } catch (Exception ex)
            {
                _scheduledIndexLogger.Log(ILogger.Severity.ERROR, "An unhandled Exception occurred during the scheduled index task.", ex);
            } finally
            {
                string indexLog = indexTaskLog.BuildTxtLog($"Sheduled Task", "");
                File.WriteAllText(
                    Path.Combine(index.GetAbsolutePath(), "ScheduledIndexTask.txt"), indexLog);
            }
        }

        private static void _scheduledIndexProgressReportTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            string message = $"{_scheduledIndexProgressVM.Status}";
            _scheduledIndexLogger.Log(ILogger.Severity.INFO, message);
        }

        private static void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if (Program.GetMainWindow().DataContext is MainWindowViewModel vm)
                {
                    // TODO Non intuitive code - It's detecting expired serials in the case
                    // The application is left open permanently.
                    vm.RaisePropertyChanged(nameof(vm.ProductTagText));
                }
            } catch (Exception ex)
            {
                // Non fatal.
                Debug.WriteLine("Exception in UI Update Timer ");
                Debug.WriteLine(ex);
            }
        }

        static Dictionary<string, string> cefFlags = new Dictionary<string, string>
        {
            {"allow-file-access-from-files",  "true"}
        };

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            var cachePath = Path.Combine(Path.GetTempPath(), "CefGlue_" + Guid.NewGuid().ToString().Replace("-", null));

            AppDomain.CurrentDomain.ProcessExit += delegate { Cef_Cleanup(cachePath); };
            return AppBuilder.Configure<App>()
                            .UsePlatformDetect()
                            .LogToTrace()
                            .UseReactiveUI()
            .AfterSetup(_ =>
            {
                CefRuntimeLoader.Initialize(new Xilium.CefGlue.CefSettings()
                {
                    RootCachePath = cachePath,
                    Locale = "en-US",
                    LogSeverity = Xilium.CefGlue.CefLogSeverity.Verbose,
                    LogFile = "CEF.log",
                    LocalesDirPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "cef_locale_files"),
                    WindowlessRenderingEnabled = true
                }, cefFlags.ToArray());
            });
        }

        private static void Cef_Cleanup(string cachePath)
        {
            CefRuntime.Shutdown(); // must shutdown cef to free cache files (so that cleanup is able to delete files)

            try
            {
                var dirInfo = new DirectoryInfo(cachePath);
                if (dirInfo.Exists)
                {
                    dirInfo.Delete(true);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // ignore
            }
            catch (IOException)
            {
                // ignore
            }
        }

        public static event EventHandler OnLanguageChanged;

        public static Avalonia.Controls.Window? GetMainWindow()
        {
            if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            return null;
        }

        public static void SetTheme(bool light)
        {
            Debug.WriteLine("Set Theme light: " + light);
            ProgramConfig.IsThemeDark = !light;
            {
                if (GetIsThemeDark()) Program.ProgramConfig.AppDataContext.FluentTheme = "Dark";
                else Program.ProgramConfig.AppDataContext.FluentTheme = "Light";
                DarkModeIconsViewModel.ThemeChangeNotice();
            }
            
            Program.SaveProgramConfig();
            
        }

        public static MCPSearchServer MCPSearchServer
        {
            get
            {
                if (_mcpSearchServer == null)
                {
                    _mcpSearchServer = new MCPSearchServer();
                }
                return _mcpSearchServer;
            }
        }

        private static MCPSearchServer _mcpSearchServer = null;


        public static PluginLoader PluginLoader
        {
            get
            {
                if (_pluginLoader == null)
                {
                    _pluginLoader = new PluginLoader();
                }
                return _pluginLoader;
            }
        }

        private static PluginLoader? _pluginLoader = null;

        public static int MAX_RESULTS_LITE                  = 10;
        public static int DEFAULT_MAX_RESULTS_REGISTERED    = 100;

        public static bool GetIsThemeDark()
        {
            if (ProgramConfig.IsThemeDark == null)
            {
                return false;
            }
            return (bool)ProgramConfig.IsThemeDark;
        }

        public static string ESEARCH_PROGRAM_DATA_DIR
        {
            get
            {
#if STANDALONE
                return Path.Combine( Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ProgramData" );
#else
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eSearch");
#endif
            }
        }

        public static string ESEARCH_INDEX_DIR
        {
            get
            {
#if STANDALONE
                return Path.Combine(ESEARCH_PROGRAM_DATA_DIR, "Indexes");    
#else
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eSearch", "Indexes");
#endif
            }
        }

        public static string ESEARCH_INDEX_LIB_FILE
        {
            get
            {
#if STANDALONE
                return Path.Combine(ESEARCH_PROGRAM_DATA_DIR, "IndexLibrary.json");
#else
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eSearch", "IndexLibrary.json");
#endif
            }
        }


       

        public static string ESEARCH_PLUGINS_DIR
        {
            get
            {
#if STANDALONE
                return Path.Combine(ESEARCH_PROGRAM_DATA_DIR, "Plugins");
#else
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eSearch", "Plugins");
#endif
            }
        }

        public static string ESEARCH_LLM_MODELS_DIR
        {
            get
            {
#if STANDALONE
            return Path.Combine(ESEARCH_PROGRAM_DATA_DIR, "AI", "Language Models");
#else
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eSearch", "AI", "Language Models");
#endif
            }
        }

        public static string ESEARCH_SYNONYMS_DIR
        {
            get
            {
#if STANDALONE
                return Path.Combine(ESEARCH_PROGRAM_DATA_DIR, "Synonyms");
#else
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eSearch", "Synonyms");
#endif
            }
        }


        public static string ESEARCH_LLM_SYSTEM_PROMPTS_DIR
        {
            get
            {
#if STANDALONE
                return Path.Combine(ESEARCH_PROGRAM_DATA_DIR, "AI", "System Prompts");
#else
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eSearch", "AI", "System Prompts");
#endif
            }
        }

        public static string ESEARCH_i18N_DIR
        {
            get
            {
#if STANDALONE
                return Path.Combine(ESEARCH_PROGRAM_DATA_DIR, "i18n");
#else
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eSearch", "i18n");
#endif
            }
        }

        public static string ESEARCH_STOP_FILE_DIR
        {
            get
            {
#if STANDALONE
                return Path.Combine(ESEARCH_PROGRAM_DATA_DIR, "Stop");
#else
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eSearch", "Stop");
#endif
            }
        }

        public static string ESEARCH_STEMMING_DIR
        {
            get
            {
#if STANDALONE
                return Path.Combine(ESEARCH_PROGRAM_DATA_DIR, "Stemming");
#else
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eSearch", "Stemming");
#endif
            }
        }

        public static string ESEARCH_SESSION_FILE
        {
            get
            {
#if STANDALONE
                return Path.Combine(ESEARCH_PROGRAM_DATA_DIR, "Session.json");
#else
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eSearch", "Session.json");
#endif
            }
        }

        public static string ESEARCH_CONFIG_FILE
        {
            get
            {
#if STANDALONE
                return Path.Combine(ESEARCH_PROGRAM_DATA_DIR, "Config.json");
#else
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eSearch", "Config.json");
#endif
            }
        }

        public static string DEBUG_TEST_LANG_PATH
        {
            get
            {
                return @"C:\Users\Tommer\source\repos\eSearch\Lang Testing\psuedolocale.lang";
            }
        }

        public static string ESEARCH_TEMP_FILES_PATH
        {
            get
            {
                return Path.Combine(System.IO.Path.GetTempPath(), "eSearch");
            }
        }

        public static string ESEARCH_EXPORT_DIR
        {
            get
            {
#if STANDALONE
                string dir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Exports");
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                return dir;
#else
                return Path.Combine(Program.GetMainWindow().StorageProvider.TryGetWellKnownFolderAsync(Avalonia.Platform.Storage.WellKnownFolder.Documents).Result.Path.LocalPath, "eSearch");
#endif
            }
        }


        public  static IProgress<float>?            ModelLoadProgress;
        private static CancellationTokenSource?     ModelLoadCancelTokenSrc;


        /// <summary>
        /// Note, if an LLM is already loaded, and the LLM is different to the config, then this will UNLOAD the existing LLM.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static async Task<LoadedLocalLLM> GetOrLoadLocalLLM(LocalLLMConfiguration config, CancellationToken cancellationToken)
        {
            if (_loadedLocalLLM != null)
            {
                if (_loadedLocalLLM.llm == config) return _loadedLocalLLM; // The LLM was already in memory
                else
                {
                    // Another LLM is in memory...
                    _loadedLocalLLM.Dispose();
                    _loadedLocalLLM = null;
                }   
            }
            // LLM is not in memory. Need to load it.

            // Firstly, check if we're already loading a model, and if so, cancel.
            if (ModelLoadCancelTokenSrc != null)
            {
                await ModelLoadCancelTokenSrc.CancelAsync();
            }

            if (Program.GetMainWindow()?.DataContext is MainWindowViewModel mwvm)
            {

                var modelLoadProgress = new Progress<float>();
                modelLoadProgress.ProgressChanged += (sender, progress) =>
                {
#if DEBUG
                    Debug.WriteLine($"Model Load Progress {progress}%");
#endif
                    mwvm.LocalLLMModelLoadProgress = progress;
                };

                mwvm.LocalLLMIsModelLoading = true;
                try
                {
                    _loadedLocalLLM = await LoadedLocalLLM.LoadLLM(config, cancellationToken, modelLoadProgress);
                    return _loadedLocalLLM;
                } catch (Exception ex)
                {
                    // TODO - Display error on the UI.

                    Debug.WriteLine($"Error loading LLM Model: {ex.ToString()}");
                    throw;
                } finally
                {
                    mwvm.LocalLLMIsModelLoading = false;
                }
            } else
            {
                throw new Exception("Must be run from Windowed eSearch");
            }
        }

        /// <summary>
        /// This could potentially be using a very large amount of RAM/VRAM when populated..
        /// </summary>
        private static LoadedLocalLLM? _loadedLocalLLM;

        public static TranslationsViewModel TranslationsViewModel { 
            get
            {
                if (_translationsViewModel == null)
                {
                    _translationsViewModel = new TranslationsViewModel(GetPreferredLanguage());
                }
                return _translationsViewModel;
            }
        }

        public static DarkModeIconsViewModel DarkModeIconsViewModel
        {
            get
            {
                if (_darkModeIconsViewModel == null)
                {
                    _darkModeIconsViewModel = new DarkModeIconsViewModel();
                }
                return _darkModeIconsViewModel;
            }
        }

        private static DarkModeIconsViewModel _darkModeIconsViewModel = null;

        public static void PsuedoLang()
        {
            Language language = Language.DynamicPsuedolocalisationLanguage();
            TranslationsViewModel.SetLanguage(language);
        }

        public static void LoadLang(string langFile)
        {
            if (langFile != null)
            {
                if (File.Exists(langFile))
                {
                    Debug.WriteLine("Loading " + langFile);
                    Language language = Language.LoadLanguage(langFile);
                    var tvm = TranslationsViewModel;
                    tvm.SetLanguage(language);
                    
                }
            } else
            {
                var tvm = TranslationsViewModel;
                tvm.SetLanguage(null);
            }
            OnLanguageChanged?.Invoke(null, null); // This event is hooked by view models to get the language to update in realtime.
        }

        public static Language GetPreferredLanguage()
        {
            try
            {
                string file = ProgramConfig.LangFile; // may be null. null is supported, means english.
                if (file == null) return null;
                return Language.LoadLanguage(file);
            } catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        public static void SetPreferredLanguage(string langFile)
        {
            ProgramConfig.LangFile = langFile;
            SaveProgramConfig();
        }

        public static ProgramConfig ProgramConfig
        {
            get
            {
                if (_programConfig == null)
                {
                    if (_loadingProgramConfig == true)
                    {
                        throw new Exception("Already loading program config...");
                    }
                    _loadingProgramConfig = true;
                    if (File.Exists(ESEARCH_CONFIG_FILE))
                    {
                        var config = JsonConvert.DeserializeObject<ProgramConfig>(File.ReadAllText(ESEARCH_CONFIG_FILE));
                        if (config != null)
                        {
                            _programConfig = config;
                        }
                    }
                }
                if (_programConfig == null)
                {
                    _programConfig = new ProgramConfig();
                }
                _loadingProgramConfig = false;
                return _programConfig;
            }
        }

        private static bool _loadingProgramConfig = false;
        private static ProgramConfig _programConfig = null;

        public static void SaveProgramConfig()
        {
            var config = ProgramConfig;
            string output = JsonConvert.SerializeObject(ProgramConfig, Formatting.Indented);
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(ESEARCH_CONFIG_FILE));
            File.WriteAllText(ESEARCH_CONFIG_FILE, output);
        }

        /// <summary>
        /// Returns metadata key _eSearchVersion value ProgramVersion
        /// Useful to know which version of eSearch indexed a document.
        /// </summary>
        /// <returns></returns>
        public static Metadata GetProgramVersionMetadata()
        {

            if (_programVersionMetadata == null)
            {
                _programVersionMetadata = new Metadata { Key = "_eSearchVersion", Value = GetProgramVersion() };
            }
            return _programVersionMetadata;
        }

        /// <summary>
        /// Returns the version number/build of eSearch.
        /// </summary>
        /// <returns></returns>
        public static string GetProgramVersion()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            StringBuilder sb = new StringBuilder();

            sb.Append(asm.GetName().Version.Major).Append(".");
            sb.Append(asm.GetName().Version.Minor).Append(".");
            sb.Append(asm.GetName().Version.Build.ToString("0"));
            sb.Append(" (");
            sb.Append(asm.GetName().Version.Revision);
            sb.Append(")");
            return sb.ToString();
        }

        private static Metadata _programVersionMetadata = null;

        private static TranslationsViewModel _translationsViewModel;

        private static string[] metadata_hidden_fields =
        {
            "Content", // The actual text content of the document shouldn't show up in metadata.
            "_IDocumentType",
            "_DateCreated",
            "_"
        };


        public static IndexLibrary IndexLibrary
        {
            get
            {
                if (_indexLibrary == null)
                {
                    _indexLibrary = IndexLibrary.LoadLibrary(Program.ESEARCH_INDEX_LIB_FILE);
                }
                return _indexLibrary;
            }
        }

        private static IndexLibrary _indexLibrary = null;


        public static string OnlineHelpLinkLocation
        {
            get
            {
                if (!Program.ProgramConfig.IsProgramRegistered())
                {
                    // Unregistered.
                    return "https://searchcloudone.com/esearch-lite-user-guide/";
                }
                else
                {
                    // Registered.
                    return "https://searchcloudone.com/esearch-pro-user-guide/";

                }
            }
        }

    }
}