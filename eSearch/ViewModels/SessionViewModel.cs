
using com.sun.org.glassfish.external.statistics;
using eSearch.Models;
using eSearch.Models.Configuration;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{

    [JsonObject(MemberSerialization.OptIn)]
    public class SessionViewModel : ViewModelBase
    {

        private string sessionFileLocation = "";
        private QueryViewModel _query;

        [JsonProperty]
        public QueryViewModel Query
        {
            get {
                if (_query == null)
                {
                
                    _query = new QueryViewModel();
                }
                return _query;
            }
            set {  
                _query = value;
                this.RaisePropertyChanged(nameof(Query));
            }
        }

        [JsonProperty]
        public string? SelectedIndexId = null;
        /*
                public IndexViewModel? Index
                {
                    get { return _index; }
                    set
                    {
                        this.RaiseAndSetIfChanged(ref _index, value, nameof(Index));
                    }
                }

                public ObservableCollection<IndexViewModel> Indexes => _indexes;

                private ObservableCollection<IndexViewModel> _indexes = new ObservableCollection<IndexViewModel>();
                private IndexViewModel? _index = null;
        */



        public static SessionViewModel LoadSession(string sessionFile)
        {
            Debug.WriteLine("Load Session " + sessionFile);
            if (File.Exists(sessionFile))
            {
                SessionViewModel session = JsonConvert.DeserializeObject<SessionViewModel>(File.ReadAllText(sessionFile)) ?? new SessionViewModel();
                session.sessionFileLocation = sessionFile;
                return session;
            }
            else
            {
                SessionViewModel session = new SessionViewModel();
                session.sessionFileLocation= sessionFile;
                return session;
            }
        }

        
        public void SaveSession()
        {
            Debug.WriteLine("Save Session " + sessionFileLocation);
            if (string.IsNullOrEmpty(sessionFileLocation))
            {
                return;
            }
            string dirName = new FileInfo(sessionFileLocation).Directory.FullName;
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            string json = JsonConvert.SerializeObject(this);
            File.WriteAllText(sessionFileLocation, json);
        }
    }
}
