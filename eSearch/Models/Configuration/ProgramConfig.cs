using DesktopSearch2.Models.Configuration;
using DesktopSearch2.Views;
using eSearch.Interop.AI;
using eSearch.Models.AI.MCP;
using eSearch.Models.AI.MCP.Tools;
using eSearch.Utils;
using eSearch.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Models.Configuration
{
    public class ProgramConfig
    {
        public string LangFile = null;

        /// <summary>
        /// When non-null, the application searches based on terms in the file instead of query text box.
        /// </summary>
        public string         SearchTermsListFile = null;

        /// <summary>
        /// Whether or not the index contents should be listed when the query is empty.
        /// </summary>
        public bool ListContentsOnEmptyQuery = true;

        public SynonymsConfig SynonymsConfig = new SynonymsConfig();

        public StemmingConfig StemmingConfig = new StemmingConfig();

        public PhoneticConfig PhoneticConfig = new PhoneticConfig();

        public AppDataContext AppDataContext = new AppDataContext();

        // This is for exporting search results..
        public ExportConfig   ExportConfig = new ExportConfig();

        public string? CopyResultsFolderPath = null;

        public CopyDocumentConfig CopyDocumentConfig = new CopyDocumentConfig();

        public CopyConversationConfig CopyConvoConfig = new CopyConversationConfig();

        public string? SelectedAISearchConfigurationID = null;

        public ExportConversationConfig ExportConversationConfig = new ExportConversationConfig();

        /// <summary>
        /// May return null if not configured.
        /// </summary>
        /// <returns></returns>
        public AISearchConfiguration? GetSelectedConfiguration()
        {
            return AISearchConfigurations.FirstOrDefault(x => x.Id == SelectedAISearchConfigurationID, null);
        }

        public List<AISearchConfiguration> AISearchConfigurations = new List<AISearchConfiguration>();

        public List<string> EnabledMCPServerNames = new List<string>();

        public List<UserConfiguredMCPServer> UserConfiguredMCPServers = new List<UserConfiguredMCPServer>();

        public bool SearchAsYouType = true;

        public SearchReportConfig SearchReportConfig = new SearchReportConfig();

        public ViewerConfig ViewerConfig = new ViewerConfig();

        public LocalLLMConfiguration LocalLLMConfiguration = new LocalLLMConfiguration();

        public LocalLLMServerConfiguration LocalLLMServerConfig = new LocalLLMServerConfiguration();

        /// <summary>
        /// List of Plugin Guids that are currently active.
        /// The guid is generated when the plugin is installed. It is the folder name inside the plugins folder.
        /// </summary>
        public List<string> InstalledPlugins = new List<string>();

        public bool? IsThemeDark = true;

        public bool IsThemeHighContrast = false;

        public string FirstRun = null;


        public DateTime GetFirstRunDate()
        {
            string iso_8601 = "yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz";
            if (FirstRun == null)
            {
                // This is the first run.
                var dateTimeUTC_Now = DateTime.UtcNow;
                string strDateTime = dateTimeUTC_Now.ToString(iso_8601, CultureInfo.InvariantCulture);
                FirstRun = Utils.Base64Encode(strDateTime);
                Program.SaveProgramConfig();
            }

            string strDateTimeFirstRun = Utils.Base64Decode(FirstRun);
            var dateTimeUTCFirstRun = DateTime.ParseExact(strDateTimeFirstRun, iso_8601, CultureInfo.InvariantCulture);
            return dateTimeUTCFirstRun;
        }

        public IEnumerable<IESearchMCPServer> GetAllAvailableMCPServers()
        {
            // yield return Program.MCPSearchServer;
            foreach (var userConfiguredServer in UserConfiguredMCPServers.OrderBy(o => o.DisplayName))
            {
                yield return userConfiguredServer;
            }
        }

        public bool LaunchAtStartup
        {
            get
            {
                if (OperatingSystem.IsWindows())
                {
                    // We used to register eSearch in the startup registry (ie. to show on task manager startup applications
                    // However, ran into issues with it related to llamasharp, so we switched to using shortcut in the startup folder
                    // (shell:startup)
                    Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    key?.DeleteValue("eSearch", false);
                }

                return System.IO.File.Exists(GetStartupShortcutFilename());
            }
            set 
            {
                try
                {

                    if (value)
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            string fileName = GetStartupShortcutFilename();
                            string? target = Environment.ProcessPath;
                            if (target != null)
                            {
                                WindowsShortcutHelper.CreateShortcut(fileName, target);
                            }
                        }
                    }
                    else
                    {
                        if (File.Exists(GetStartupShortcutFilename()))
                        {
                            File.Delete(GetStartupShortcutFilename());
                        }
                    }
                } catch (NullReferenceException nre)
                {
                    if (nre.Message == "Already loading program config...")
                    {
                        // Non fatal. Json deserializer is attempting to assign this, it shouldn't. TODO figure out how to exclude this from serialization
                    }
                }
            }
        }

        private string GetStartupShortcutFilename()
        {
            string startDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string fileName = GetProductTagText() + ".lnk";
            return Path.Combine(startDir, fileName);
        }


        public string Serial = "";

        /// <summary>
        /// Note this method currently ALSO returns true if the application is currently in evaluation mode.
        /// </summary>
        /// <returns></returns>
        public bool IsProgramRegistered()
        {
#if STANDALONE
            bool standalone = true;
#else
            bool standalone = false;
#endif
#if LITE
            return false;
#endif
#if TARILIO
            if (string.IsNullOrEmpty(Serial))
            {
                if (IsProgramInEvaluationPeriod(out int daysRemaining))
                {
                    return true;
                }
                return false;
            }
            var serialValidity = TARILIO.ProductSerials.isValidSerial(Serial, out string year, standalone);
            if (serialValidity == TARILIO.ProductSerials.SerialValidationResult.Valid
             || serialValidity == TARILIO.ProductSerials.SerialValidationResult.SearchOnly   
                )
            {
                return true;
            } else
            {
                if (IsProgramInEvaluationPeriod(out int daysRemaining))
                {
                    return true;
                }
                return false;
            }
#else
            // eSearch Pro
            return true;
#endif
        }

        public string GetProductTagText()
        {
#if STANDALONE
            bool standalone = true;
#else
            bool standalone = false;
#endif
#if TARILIO
            string productVersion = "TARILIO";
                if (Program.ProgramConfig.IsProgramRegistered())
                {
                    bool search_only = (TARILIO.ProductSerials.isValidSerial(Program.ProgramConfig.Serial, out var ignored, standalone) == TARILIO.ProductSerials.SerialValidationResult.SearchOnly);
                    #if STANDALONE
                        if (search_only) 
                        { 
                            productVersion += " Publish";
                        } else
                        {
                            productVersion += " Portable";
                        }
                    #else
                        if (search_only) 
                        { 
                            productVersion += " Pro (Search Only)";
                        } else {
                            productVersion += " Pro";
                        }
                    #endif
                }
                else
                {
                    // Not registered
                    #if STANDALONE
                        productVersion += " Lite Portable";
                    #else
                        productVersion += " Lite";
                    #endif
                }
            #else       // eSearch Build
                string productVersion = "TARILIO"; // Previously eSearch Pro
                #if STANDALONE
                    productVersion += " Portable";
                #endif
            #endif

#if DEBUG
            productVersion += " (Debug)";
#endif
            return productVersion;
        }

        public bool IsProgramInEvaluationPeriod(out int daysRemaining)
        {
            DateTime periodEnd  = GetFirstRunDate().AddDays(30);
            DateTime utcNow     = DateTime.UtcNow;
            if (utcNow < periodEnd)
            {
                daysRemaining = (int)Math.Ceiling((periodEnd - utcNow).TotalDays);
                return true;
            } else
            {
                daysRemaining = 0;
                return false;
            }
        }



    }
}
