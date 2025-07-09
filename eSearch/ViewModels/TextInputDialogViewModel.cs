using NPOI.OpenXmlFormats.Dml.Chart;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class TextInputDialogViewModel : ViewModelBase
    {
        public string Label
        {
            get
            {
                return _label;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _label, value);
            }
        }

        private string _label = "";

        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                string raw = value;
                if (raw.Length > 50) raw = raw.Substring(0, 50);
                raw = raw.Replace("<", "").Replace(">", "").Replace("\"", "");
                this.RaiseAndSetIfChanged(ref _text, raw);
                this.RaisePropertyChanged(nameof(TextValid));
            }
        }

        private string _text;

        public bool TextValid
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_text);
            }
        }

        public int MaxLength
        {
            get
            {
                return _maxLength;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _maxLength, value);
            }
        }

        private int _maxLength = 50;

        public string Watermark
        {
            get
            {
                return _watermark;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _watermark, value);
            }
        }

        private string _watermark = "Example Watermark";

        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _title, value);
            }
        }

        private string _title = "Example Title";

        public string ValidationError
        {
            get
            {
                return _validationError;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _validationError, value);
            }
        }

        private string _validationError = "";
    }
}
