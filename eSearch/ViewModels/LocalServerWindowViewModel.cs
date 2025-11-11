using eSearch.Utils;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using static eSearch.Models.Logging.InMemoryLog;

namespace eSearch.ViewModels
{
    public class LocalServerWindowViewModel : ViewModelBase, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }

        public LocalServerWindowViewModel()
        {
            Activator = new ViewModelActivator();

            this.WhenActivated(disposables =>
            {
                UpdateIPAddress(); 

                var refreshInterval = TimeSpan.FromSeconds(10);
                Observable
                    .Timer(TimeSpan.FromSeconds(1), refreshInterval)
                    .Subscribe(x =>
                    {
                        UpdateIPAddress();
                    })
                    .DisposeWith(disposables);
            });
        }

        public bool JustCopiedAddress
        {
            get => _justCopiedAddress;
            set => this.RaiseAndSetIfChanged(ref _justCopiedAddress, value);
        }

        private bool _justCopiedAddress = false;
        private void UpdateIPAddress()
        {
            #region Update IP Address Info
            string? ipAddress = IPAddressHelper.GetLocalIPv4Address();
            if (ipAddress != null)
            {
                DetectedIPAddress = $"http://{ipAddress}:{Port}/v1";
            } else
            {
                DetectedIPAddress = "No local IPv4 address found.";
            }
            #endregion
        }

        public string DetectedIPAddress
        {
            get => _detectedIpAddress;
            set => this.RaiseAndSetIfChanged(ref _detectedIpAddress, value);
        }

        private string _detectedIpAddress = "";

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
                UpdateIPAddress();
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
