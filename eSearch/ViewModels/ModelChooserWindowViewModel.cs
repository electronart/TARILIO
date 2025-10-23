using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class ModelChooserWindowViewModel : ViewModelBase
    {
        public List<string> AvailableModels 
        { 
            get
            {
                return _availableModels;
            } 
            set
            {
                this.RaiseAndSetIfChanged(ref _availableModels, value);
            }
        }

        private List<string> _availableModels = new List<string>();

        public string? SelectedModel
        {
            get
            {
                return _selectedModel;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedModel, value);
            }
        }

        private string? _selectedModel = null;
    }
}
