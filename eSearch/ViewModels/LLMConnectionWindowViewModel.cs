using com.sun.org.apache.xml.@internal.security.keys.keyresolver.implementations;
using DynamicData;
using eSearch.Models;
using eSearch.Models.AI;
using eSearch.Models.Configuration;
using eSearch.Utils;
using NPOI.OpenXmlFormats.Spreadsheet;
using ReactiveUI;
using sun.tools.tree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.ViewModels
{
    public class LLMConnectionWindowViewModel : ViewModelBase
    {

        public static LLMConnectionWindowViewModel FromProgramConfiguration()
        {
            LLMConnectionWindowViewModel viewModel = new LLMConnectionWindowViewModel();
            if (Program.ProgramConfig?.AISearchConfigurations?.Count > 0)
            {
                Program.ProgramConfig.AISearchConfigurations.Sort((x,y) => string.Compare(x.GetDisplayName(),y.GetDisplayName()));
                viewModel.AvailableConnections.AddRange(Program.ProgramConfig.AISearchConfigurations);
            } else
            {
                viewModel.ShowConnectionForm = false;
            }
            return viewModel;
        }



        public void PopulateValuesFromConfiguration(AISearchConfiguration aiSearchConfig)
        {
            string apiKey;
            try
            {
                apiKey = eSearch.Models.Utils.Base64Decode(aiSearchConfig.APIKey);
            }
            catch (Exception ex)
            {
                apiKey = ""; // We swapped to base64 and old strings will throw exception.
            }

            SelectedService = aiSearchConfig.LLMService; // Must be set first as changing this clears the other fields.
            APIKey = apiKey;
            
            ServerURL = aiSearchConfig.ServerURL;
            EnteredModelName = aiSearchConfig.Model;
            CustomSystemPrompt = aiSearchConfig.CustomSystemPrompt ?? string.Empty;
            SelectedSystemPromptRole = aiSearchConfig.SystemPromptRole;
            PreviousID = aiSearchConfig.Id;
            PreviousDisplayName = aiSearchConfig.CustomDisplayName ?? string.Empty;
            SelectedPerplexityModel = AvailablePerplexityModels.SingleOrDefault(m => m.Value == aiSearchConfig.PerplexityModel, AvailablePerplexityModels[0]);
        }

        public string? PreviousID = null;
        public string? PreviousDisplayName = null;


        public AISearchConfiguration ToAiSearchConfiguration()
        {
            AISearchConfiguration aiSearchConfiguration = new AISearchConfiguration()
            {
                APIKey = Models.Utils.Base64Encode(this.APIKey),
                LLMService = this.SelectedService,
                ServerURL = this.ServerURL,
                PerplexityModel = this.SelectedPerplexityModel?.Value ?? PerplexityModel.Small,
                Model = this.EnteredModelName,
                SystemPromptRole = this.SelectedSystemPromptRole
            };
            if (!string.IsNullOrWhiteSpace(CustomSystemPrompt))
            {
                aiSearchConfiguration.CustomSystemPrompt = CustomSystemPrompt;
            }
            aiSearchConfiguration.Id = PreviousID ?? Guid.NewGuid().ToString();
            aiSearchConfiguration.CustomDisplayName = PreviousDisplayName ?? string.Empty;
            return aiSearchConfiguration;
        }


        public ObservableCollection<AISearchConfiguration> AvailableConnections
        {
            get
            {
                return _availableConnections;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _availableConnections, value);
            }
        }

        private ObservableCollection<AISearchConfiguration> _availableConnections = new ObservableCollection<AISearchConfiguration>();

        public AISearchConfiguration? SelectedConnection
        {
            get
            {
                return _selectedConnection;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedConnection, value);
            }
        }

        private AISearchConfiguration? _selectedConnection = null;


        public bool ShowConnectionForm
        {
            get
            {
                return _showConnectionForm;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _showConnectionForm, value);
            }
        }

        private bool _showConnectionForm = false;

        public bool EnableConnectionForm
        {
            get
            {
                return _enableConnectionForm;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _enableConnectionForm, value);
            }
        }

        private bool _enableConnectionForm = false;

        public bool IsTesting
        {
            get
            {
                return _isTesting;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref this._isTesting, value);
            }
        }

        private bool _isTesting = false;

        public bool IsEditing
        {
            get
            {
                return _isEditing;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isEditing, value);
            }
        }

        private bool _isEditing = false;


        public ObservableCollection<LLMService> AvailableServices
        {
            get
            {
                var values = new ObservableCollection<LLMService>(Enum.GetValues(typeof(LLMService)).Cast<LLMService>());
                return values;
            }
        }

        [Required]
        public LLMService SelectedService { 
            get
            {
                return _selectedService;
            }
            set
            {
                switch(value)
                {
                    case LLMService.Custom:
                        HideServerURL = false;
                        break;
                    default:
                        HideServerURL = true;
                        break;
                }
                HidePerplexityModelSelectionDropDown = value != LLMService.Perplexity;
                this.RaiseAndSetIfChanged(ref _selectedService, value);

            }
        }

        public string CustomSystemPrompt
        {
            get
            {
                return _customSystemPrompt;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _customSystemPrompt, value);
            }
        }

        private string _customSystemPrompt = string.Empty;

        public string DefaultSystemPrompt
        {
            get
            {
                return S.Get(Completions.DEFAULT_SYSTEM_PROMPT);
            }
        }



        private LLMService _selectedService = LLMService.Perplexity;

        public bool HideServerURL { 
            get
            {
                return _hideServerURL;
            } set
            {
                this.RaiseAndSetIfChanged(ref _hideServerURL, value);
            }
        }

        private bool _hideServerURL = true;

        public string ServerURL
        {
            get
            {
                return _serverURL;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _serverURL, value);
            }
        }

        private string _serverURL = string.Empty;

        public string APIKey
        {
            get
            {
                return _apiKey;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _apiKey, value);
            }
        }

        private string _apiKey = string.Empty;

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

        private string _validationError = string.Empty;

        public bool HidePerplexityModelSelectionDropDown
        {
            get
            {
                return _hidePerplexityModelSelectionDropDown;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _hidePerplexityModelSelectionDropDown, value);
            }
        }

        private bool _hidePerplexityModelSelectionDropDown = false;


        public ObservableCollection<PerplexityModelDisplayItem> AvailablePerplexityModels
        {
            get
            {
                if (_availablePerplexityModels == null)
                {
                    _availablePerplexityModels = new ObservableCollection<PerplexityModelDisplayItem>();
                    foreach (var value in Enum.GetValues(typeof(PerplexityModel)).Cast<PerplexityModel>())
                    {
                        if (!value.IsObsolete())
                        {
                            _availablePerplexityModels.Add(new PerplexityModelDisplayItem
                            {
                                Value = value,
                                Description = value.GetDescription()
                            });
                        }
                    }

                }
                return _availablePerplexityModels;
            }
        }

        private ObservableCollection<PerplexityModelDisplayItem> _availablePerplexityModels = null;

        public class PerplexityModelDisplayItem
        {
            public PerplexityModel Value { get; set; }
            public string Description { get; set; } = string.Empty;

            public override string ToString()
            {
                return Description;
            }

        }

        public ObservableCollection<string> SystemPromptRoles
        {
            get
            {
                return new ObservableCollection<string>() { "System", "Developer" };
            }
        }

        public string SelectedSystemPromptRole
        {
            get
            {
                if (_selectedSystemPromptRole == null)
                {
                    _selectedSystemPromptRole = SystemPromptRoles[0];
                }
                return _selectedSystemPromptRole;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedSystemPromptRole, value);
            }
        }

        private string? _selectedSystemPromptRole = null;



        public PerplexityModelDisplayItem? SelectedPerplexityModel
        {
            get
            {
                return _selectedPerplexityModel;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedPerplexityModel, value);
            }
        }

        private PerplexityModelDisplayItem? _selectedPerplexityModel = null;

        public string EnteredModelName
        {
            get
            {
                return _enteredModelName;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _enteredModelName, value);
            }
        }

        private string _enteredModelName = string.Empty;




    }
}
