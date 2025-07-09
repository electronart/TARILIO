using eSearch.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class SearchFilterWindowViewModel2 : ViewModelBase
    {

        public List<DataColumn> SelectableFields
        {
            get
            {
                return _selectableFields;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectableFields, value);
            }
        }

        private List<DataColumn> _selectableFields = new List<DataColumn>();

        public DataColumn SelectedField
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

        private DataColumn _selectedField = null;
    }
}
