using com.sun.org.glassfish.external.statistics;
using eSearch.Models.Search.Synonyms;
using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class SynonymsFileViewModel : ViewModelBase
    {

        public SynonymsFileViewModel(string filePath, string name, bool active)
        {
            FilePath = filePath;
            Name = name;
            _isActive = active;
        }

        public string FilePath;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _name, value);
            }
        }

        private string _name;
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isActive, value);

            }
        }

        private bool _isActive = false;

        public ObservableCollection<SynonymGroup> SynonymGroups
        {
            get
            {
                if (_synonymGroups == null)
                {
                    // Not yet loaded the groups.
                    var synonymGroups = Thesaurus.GetSynonymGroups();
                    _synonymGroups = new ObservableCollection<SynonymGroup>();
                    _synonymGroups.AddRange(synonymGroups);
                }   
                return _synonymGroups;
            } set
            {
                this.RaiseAndSetIfChanged(ref _synonymGroups, value);
            }
        }

        private ObservableCollection<SynonymGroup> _synonymGroups;

        public void SaveChanges()
        {
            if (Thesaurus is UTP_Thesaurus utpThesaur)
            {
                utpThesaur.SaveThesaurus(SynonymGroups.ToArray());
            }
        }


        private IThesaurus Thesaurus
        {
            get
            {
                // TODO We may support alternatives to UTP format in future. For now, always
                // assume that thesaurus is a UTP Thesaurus format file.
                if (_thesaurus == null)
                {
                    _thesaurus = UTP_Thesaurus.LoadThesaurus(FilePath);
                }
                return _thesaurus;
            }
        }
        private IThesaurus _thesaurus = null;



    }
}
