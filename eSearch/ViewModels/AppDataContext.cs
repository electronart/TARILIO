using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{

    [JsonObject(MemberSerialization.OptIn)]
    public class AppDataContext : ViewModelBase
    {

        [JsonProperty]
        public string FluentTheme
        {
            get
            {
                if (_theme == null)
                {
                    if (Program.GetIsThemeDark())
                    {
                        _theme = "Dark";
                    }
                    else
                    {
                        _theme = "Light";
                    }
                }
                return _theme;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _theme, value);
            }
        }
        
        private string _theme = null;


        [JsonProperty]
        public MainWindowViewModel MainWindowViewModel
        {
            get
            {
                if (_mainWindowViewModel == null)
                {
                    _mainWindowViewModel = new MainWindowViewModel();
                }
                return _mainWindowViewModel;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _mainWindowViewModel, value);
            }
        }

        private MainWindowViewModel _mainWindowViewModel = null;
        

    }
}
