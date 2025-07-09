using Avalonia.Media;
using eSearch;
using eSearch.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopSearch2.ViewModels
{
    public class ColorPickerWindowViewModel : ViewModelBase
    {
        public HsvColor SelectedColor
        {
            get
            {
                if (_selectedColor == null)
                {
                    var color = Program.ProgramConfig.ViewerConfig.HitHighlightColor;
                    _selectedColor = HsvColor.FromHsv(color.GetHue(), color.GetSaturation(), color.GetBrightness());
                }
                return (HsvColor)_selectedColor;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref  _selectedColor, value);
            }
        }

        private HsvColor? _selectedColor = null;
    }
}
