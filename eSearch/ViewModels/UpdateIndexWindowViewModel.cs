using Avalonia.Controls;
using eSearch.Models;
using eSearch.Models.Configuration;
using eSearch.Models.Indexing;
using eSearch.Views;
using DocumentFormat.OpenXml.Linq;
using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static eSearch.Views.TextInputDialog;
using S = eSearch.ViewModels.TranslationsViewModel;
using static thredds.featurecollection.FeatureCollectionConfig;
using eSearch.Models.Documents;

namespace eSearch.ViewModels
{
    public class UpdateIndexWindowViewModel : ViewModelBase
    {
        public IndexLibrary? IndexLibrary {
            get
            {
                return _indexLibrary;
            } set
            {
                this.RaiseAndSetIfChanged(ref _indexLibrary, value);
            }
        }

        private IndexLibrary? _indexLibrary;

        public List<IIndex> Indexes
        {
            get
            {
                if (_indexLibrary == null) return new List<IIndex>();
                return _indexLibrary.GetAllIndexes();
            }
        }

        public IIndex? SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }
            set
            {
                _selectedIndex = value;
                this.RaisePropertyChanged(nameof(SelectedIndex));
            }
        }

        private IIndex? _selectedIndex;

        public string DisplayedIndexInformation
        {
            get
            {
                return _displayedIndexInformation;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _displayedIndexInformation, value);
            }
        }

        private string _displayedIndexInformation = string.Empty;

        public UpdateIndexWindowViewModel(Window myWindow, IndexLibrary indexLibrary, IIndex selectedIndex)
        {
            this.IndexLibrary  = indexLibrary;
            this.SelectedIndex = selectedIndex;
        }

        

        public UpdateIndexWindowViewModel()
        {
            // For designer only.
        }
    }
}
