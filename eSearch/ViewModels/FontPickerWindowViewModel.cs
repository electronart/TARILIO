using Avalonia.Media;
using eSearch;
using eSearch.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class FontPickerWindowViewModel : ViewModelBase
    {

        public List<Avalonia.Media.FontFamily> Fonts { 
            get 
            {
                List<Avalonia.Media.FontFamily> fonts = new List<Avalonia.Media.FontFamily>();
                foreach (var font in FontManager.Current.SystemFonts)
                {
                    fonts.Add(font);
                }
                return fonts;
            } 
        }

        public Avalonia.Media.FontFamily SelectedFontFamily
        {
            get
            {
                if (_selectedFontFamily == null)
                {
                    foreach(var family in Fonts)
                    {
                        if (family.Name == Program.ProgramConfig.ViewerConfig.FontFamilyName)
                        {
                            _selectedFontFamily = family;
                        }
                    }
                }
                return _selectedFontFamily;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedFontFamily, value);
            }
        }

        private Avalonia.Media.FontFamily _selectedFontFamily = null;

        public int FontSizePt {
            get
            {
                if (_fontSizePt == null)
                {
                    _fontSizePt = Program.ProgramConfig.ViewerConfig.FontSizePt;
                }
                return (int)_fontSizePt;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _fontSizePt, value);
            }
        }

        private int? _fontSizePt;
    }
}
