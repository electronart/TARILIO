using eSearch.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;


namespace DesktopSearch2.ViewModels
{
    public class QueryFilterViewModel : ViewModelBase
    {
        public string SelectedField
        {
            get
            {
                return _selectedField;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedField, value);
            }
        }

        private string _selectedField = "";


        public List<string> AvailableFields
        {
            get
            {
                return _availableFields;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _availableFields, value);
            }
        }

        private List<string> _availableFields = new List<string>
        {
            "Sample field 1", "Sample field 2"
        };

        public string SearchText
        {
            get
            {
                return _searchText;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
            }
        }

        private string _searchText = "";

    }
}
