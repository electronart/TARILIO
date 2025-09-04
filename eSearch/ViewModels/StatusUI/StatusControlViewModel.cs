using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels.StatusUI
{
    public class StatusControlViewModel : ViewModelBase
    {
        // Tag is used to associate the status control with an object in eSearch
        public object? Tag
        {
            get
            {
                return _tag;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _tag, value);
            }
        }

        private object? _tag = null;

        public string? StatusTitle
        {
            get
            {
                return _statusTitle;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _statusTitle, value);
            }
        }

        private string? _statusTitle = null;

        public string? StatusMessage
        {
            get
            {
                return _statusMessage;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _statusMessage, value);
            }
        }

        private string? _statusMessage = null;

        public float? StatusProgress
        {
            get
            {
                return _statusProgress;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _statusProgress, value);
            }
        }

        private float? _statusProgress = null;
    }

    public class DesignStatusControlViewModel : StatusControlViewModel
    {
        public DesignStatusControlViewModel() : base() { StatusTitle = "This is a sample title"; StatusMessage = "This is a sample status message."; StatusProgress = 32.0f; }
    }
}
