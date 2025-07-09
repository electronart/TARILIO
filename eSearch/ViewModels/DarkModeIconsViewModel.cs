using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class DarkModeIconsViewModel : ReactiveObject
    {

        private bool _dark = false;

        public DarkModeIconsViewModel()
        {

        }

        public HashSet<string> icons = new HashSet<string>();

        public void ThemeChangeNotice()
        {
            this.RaisePropertyChanged("Item[]");
            foreach(var icon in icons)
            {
                this.RaisePropertyChanged("Item[" + icon + "]");
            }
            this.RaisePropertyChanged("Item[]");
            this.RaisePropertyChanged("Item");
        }

        [IndexerName("Item")]
        public Bitmap this[string name]
        {
            get
            {
                icons.Add(name);
                var assemblyName = typeof(Program).Assembly.GetName().Name;
                string uri = "";
                if (Program.GetIsThemeDark())
                {
                    uri = "avares://" + assemblyName + "/Assets/dark-" + name;
                    
                }
                else
                {
                    uri = "avares://" + assemblyName + "/Assets/" + name;
                }
                return new Bitmap(AssetLoader.Open(new Uri(uri)));

            }
        }


    }
}
