using eSearch.Interop;
using eSearch.Models.Plugins;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class PluginsWindowViewModel : ViewModelBase
    {
        public ObservableCollection<Plugin> AvailablePlugins
        {
            get
            {
                return _availablePlugins;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _availablePlugins, value);
            }
        }

        private ObservableCollection<Plugin> _availablePlugins = new ObservableCollection<Plugin>();

        public Plugin? SelectedPlugin
        {
            get
            {
                return _selectedPlugin;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedPlugin, value);
            }
        }

        private Plugin? _selectedPlugin = null;

        public string PluginName { 
            get
            {
                return _pluginName;
            } 
            set
            {
                this.RaiseAndSetIfChanged(ref _pluginName, value);
            }
        }

        private string _pluginName = string.Empty;

        public string PluginAuthor
        {
            get
            {
                return _pluginAuthor;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _pluginAuthor, value);
            }
        }

        public string PluginVersion
        {
            get
            {
                return _pluginVersion;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _pluginVersion, value);
            }
        }

        private string _pluginVersion = string.Empty;

        private string _pluginAuthor = string.Empty;

        public string PluginDescription
        {
            get
            {
                return _pluginDescription;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _pluginDescription, value);
            }
        }

        private string _pluginDescription = string.Empty;

        public bool IsPluginInstalling
        {
            get
            {
                return _isPluginInstalling;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isPluginInstalling, value);
            }
        }

        private bool _isPluginInstalling = false;

        public bool IsPluginCompatible
        {
            get
            {
                return _isPluginCompatible;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isPluginCompatible, value);
            }
        }

        private bool _isPluginCompatible = false;

    }
}
