using eSearch.Models.Indexing;
using eSearch.Models.Search;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class IndexViewModel : ViewModelBase
    {
        private IIndex _index;

        public IndexViewModel(IIndex index)
        {
            _index = index;
        }

        public string Name
        {
            get { return _index.Name; }
            set
            {
                _index.Name = value;
                this.RaisePropertyChanged(nameof(Name));
            }
        }

        public IIndex GetIIndex()
        {
            return _index;
        }
          
        public string Description
        {
            get { return _index.Description; }
            set
            {
                _index.Description = value;
                this.RaisePropertyChanged(nameof(Description));
            }
        }

        public string Id => _index.Id;
        public string Location
        {
            get { return _index.Location; }
            set
            {
                _index.Location = value;
                this.RaisePropertyChanged(nameof(Location));
            }
        }

        public int Size
        {
            get { return _index.Size; }
            set
            {
                _index.Size = value;
                this.RaisePropertyChanged(nameof(Size));
            }
        }

        public IWordWheel? WordWheel => _index.WordWheel;

    }
}
