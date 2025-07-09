using Avalonia.Controls;
using DesktopSearch2.Models.Search;
using DesktopSearch2.ViewModels;
using eSearch.Models;
using eSearch.ViewModels;
using eSearch.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static eSearch.Views.TextInputDialog;

namespace eSearch.ViewModels
{
    public class SearchFilterWindowViewModel : ViewModelBase
    {


        public ObservableCollection<QueryFilterViewModel> QueryFilters
        {
            get
            {
                return _queryFilters;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _queryFilters, value);
            }
        }

        private ObservableCollection<QueryFilterViewModel> _queryFilters = new ObservableCollection<QueryFilterViewModel>();


        public List<string> AvailableFields = new List<string>();

        public async void AddFilter()
        {
            var queryFilterViewModel = new QueryFilterViewModel();
            queryFilterViewModel.AvailableFields = AvailableFields;
            QueryFilters.Add(queryFilterViewModel);
        }


        

    }
}
