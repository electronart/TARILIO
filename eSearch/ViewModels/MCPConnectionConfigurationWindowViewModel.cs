using eSearch.Interop.AI;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class MCPConnectionConfigurationWindowViewModel : ViewModelBase
    {

        public ObservableCollection<IESearchMCPServer> AvailableMCPServers
        {
            get
            {
                return _availableMCPServers;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _availableMCPServers, value);
            }
        }

        private ObservableCollection<IESearchMCPServer> _availableMCPServers = new ObservableCollection<IESearchMCPServer>();

        public IESearchMCPServer? SelectedMCPServer
        {
            get
            {
                return _selectedMCPServer;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedMCPServer, value);
            }
        }

        private IESearchMCPServer? _selectedMCPServer = null;


        public string ConfigurationWatermark
        {
            get
            {
                return 
@"{
   ""mcpServers"": 
   {
      ""my-server"": {
         ""command"": ""npx"",
         ""args"": [
         ""-y"",
         ""@modelcontextprotocol/my-server""
         ],
         ""env"": {
         ""MY_TOKEN"": ""<YOUR_TOKEN>""
         }
      }
   }
}";
            }
        }

        public bool IsFormEditMode
        {
            get
            {
                return _isFormEditMode;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isFormEditMode, value);
            }
        }

        private bool _isFormEditMode = false;

        public bool ShowServerConfigurationPanel
        {
            get
            {
                return _showServerConfigurationPanel;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _showServerConfigurationPanel, value);
            }
        }

        private bool _showServerConfigurationPanel = false;

        public string CurrentConfigurationJson
        {
            get
            {
                return _currentConfigurationJson;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _currentConfigurationJson, value);
            }
        }

        private string _currentConfigurationJson = string.Empty;

    }
}
