using NPOI.OpenXmlFormats.Dml.Chart;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class CheckBoxItemViewModel : ReactiveObject
    {
        public bool IsChecked
        {
            get
            {
                return _isChecked;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isChecked, value);
            }
        }

        private bool _isChecked;

        public string Header
        {
            get
            {
                return _header;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _header, value);
            }
        }

        private string _header;
    }
}
