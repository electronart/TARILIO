using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static eSearch.Models.Logging.InMemoryLog;

namespace eSearch.ViewModels
{
    public class LocalServerWindowViewModel : ViewModelBase
    {
        public bool IsServerRunning
        {
            get
            {
                return _isServerRunning;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isServerRunning, value);
            }
        }

        private bool _isServerRunning = false;

        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _port, value);
            }
        }

        private int _port = 5000;

        public ObservableCollection<LogItem>? LogItems
        {
            get
            {
                return _logItems;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _logItems, value);
            }
        }

        private ObservableCollection<LogItem>? _logItems = null;
    }
}
