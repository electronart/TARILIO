using Avalonia.Input;
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


        public Cursor Cursor
        {
            get
            {
                return _cursor;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _cursor, value);
            }
        }

        private Cursor _cursor = new Cursor(StandardCursorType.Arrow);


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

        public string? StatusError
        {
            get
            {
                return _statusError;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _statusError, value);
            }
        }

        private string? _statusError = null;

        /// <summary>
        /// When not null, a dismiss button is shown. the action will be carried out when clicked.
        /// </summary>
        public Action? DismissAction
        {
            get
            {
                return _dismissAction;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _dismissAction, value);
            }
        }

        private Action? _dismissAction = null;

        /// <summary>
        /// Set to 0 for indeterminate progress bar.
        /// </summary>
        public float? StatusProgress
        {
            get
            {
                return _statusProgress;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _statusProgress, value);
                StatusProgressIsIndeterminate = value != null && value < 1;
            }
        }

        private float? _statusProgress = null;

        /// <summary>
        /// When not null, a cancel button is shown. The action is performed when clicked.
        /// </summary>
        public Action? CancelAction
        {
            get
            {
                return _cancelAction;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _cancelAction, value);
            }
        }

        private Action? _cancelAction = null;

        public bool StatusProgressIsIndeterminate
        {
            get
            {
                return _statusProgressIsIndeterminate;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _statusProgressIsIndeterminate, value);
            }
        }

        private bool _statusProgressIsIndeterminate = false;

        public Action? ClickAction
        {
            get
            {
                return _clickAction;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _clickAction, value);
                if (value == null)
                {
                    Cursor = new Cursor(StandardCursorType.Arrow);
                } else
                {
                    Cursor = new Cursor(StandardCursorType.Hand);
                }
            }
        }

        private Action? _clickAction = null;
    }

    public class DesignStatusControlViewModel : StatusControlViewModel
    {
        public DesignStatusControlViewModel() : base() { StatusTitle = "This is a sample title"; StatusMessage = "This is a sample status message."; StatusProgress = 32.0f; StatusError = "An error message."; }
    }
}
