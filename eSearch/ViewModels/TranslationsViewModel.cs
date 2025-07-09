using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using eSearch.Models.Localization;
using ReactiveUI;

namespace eSearch.ViewModels
{
    public class TranslationsViewModel : ReactiveObject
    {
        private Language _language;

        public TranslationsViewModel(Language language)
        {
            _language = language;
            LocalStr.SetLanguage(language);
        }

        public void SetLanguage(Language language) 
        { 
            _language = language;
            LocalStr.SetLanguage(language);
            // https://stackoverflow.com/questions/657675/propertychanged-for-indexer-property
            this.RaisePropertyChanged("Item[]");
            if (_language != null)
            {
                foreach(var item in this.Language.Translations)
                {
                    this.RaisePropertyChanged(item.Key);
                    this.RaisePropertyChanged("Item[" + item.Key + "]");
                    
                    Debug.WriteLine("Item[" + item.Key + "]");
                }
            }
            this.RaisePropertyChanged("Item[]");
            this.RaisePropertyChanged("Item");

        }

        public Language Language { get { return _language; } }

        // https://stackoverflow.com/questions/657675/propertychanged-for-indexer-property
        [IndexerName("Item")]
        public string this[string name]
        {
            get
            {
                name = name.Replace("~", " ");
                return LocalStr.Get(name);
            }
        }

        public static string Get(string name)
        {
            return Program.TranslationsViewModel[name];
        }
    }
}
