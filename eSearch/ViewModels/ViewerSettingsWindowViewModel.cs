using DocumentFormat.OpenXml.Linq;
using eSearch;
using eSearch.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DesktopSearch2.Models.Configuration.ViewerConfig;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.ViewModels
{
    public class ViewerSettingsWindowViewModel : ViewModelBase
    {
        #region View Large Files settings
        public int ViewerMaxFileSizeMB
        {
            get
            {
                if (_viewerMaxFileSizeMB == null)
                {
                    _viewerMaxFileSizeMB = Program.ProgramConfig.ViewerConfig.MaxFileSizeMB;
                }
                return (int)_viewerMaxFileSizeMB;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _viewerMaxFileSizeMB, value);
            }
        }

        private int? _viewerMaxFileSizeMB = null;



        public OptionViewLargeFile OptionViewLargeFiles
        {
            get
            {
                if (IsRadioViewLargeFileFullyChecked) return OptionViewLargeFile.Fully;
                if (IsRadioViewLargeFileFirstPageChecked) return OptionViewLargeFile.FirstPageOnly;
                if (IsRadioViewLargeFileReportViewChecked) return OptionViewLargeFile.InReportView;
                return OptionViewLargeFile.Fully;
            }
            set
            {
                switch( value )
                {
                    case OptionViewLargeFile.Fully:
                        IsRadioViewLargeFileFullyChecked = true; break;
                    case OptionViewLargeFile.FirstPageOnly:
                        IsRadioViewLargeFileFirstPageChecked = true; break;
                    case OptionViewLargeFile.InReportView:
                        IsRadioViewLargeFileReportViewChecked = true; break;
                }
            }
        }

        public bool IsRadioViewLargeFileFullyChecked { get; set; }

        public bool IsRadioViewLargeFileFirstPageChecked { get; set; }

        public bool IsRadioViewLargeFileReportViewChecked { get; set; }

        public int   ReportViewContextAmount
        {
            get
            {
                if (_reportViewContextAmount == null)
                {
                    _reportViewContextAmount = Program.ProgramConfig.ViewerConfig.ReportViewContextAmount;
                }
                return (int)_reportViewContextAmount;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _reportViewContextAmount, value);
            }
        }

        private int? _reportViewContextAmount = null;

        public OptionContextAmountType OptionReportViewContextType
        {
            get
            {
                if (ComboBoxContextTypeSelectedIndex == 0) return OptionContextAmountType.Words;
                return OptionContextAmountType.Paragraphs;
            }
            set
            {
                switch(value)
                {
                    case OptionContextAmountType.Words:
                        ComboBoxContextTypeSelectedIndex = 0; break;
                    default:
                        ComboBoxContextTypeSelectedIndex = 1; break;
                }
            }
        }

        public string FontFamilyName
        {
            get
            {
                if (_fontFamilyName == null)
                {
                    _fontFamilyName = Program.ProgramConfig.ViewerConfig.FontFamilyName + " " + Program.ProgramConfig.ViewerConfig.FontSizePt + "pt";
                }
                return _fontFamilyName;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _fontFamilyName, value);
            }
        }

        private string? _fontFamilyName = null;


        public string HighlightColorHex
        {
            get
            {
                var color = Program.ProgramConfig.ViewerConfig.HitHighlightColor;
                var hex = string.Format("{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
                return hex;
            }
        }

        public string HighlightColorBackground
        {
            get
            {
                return "#" + HighlightColorHex;
            }
        }

        public void UpdateHighlightColor()
        {
            this.RaisePropertyChanged(nameof(HighlightColorHex));
            this.RaisePropertyChanged(nameof(HighlightColorBackground));
        }

        public List<string> ComboBoxContextTypeItems
        {
            get
            {
                //OptionReportViewContextType.Words;
                //OptionReportViewContextType.Paragraphs

                return new List<string>() { S.Get("Words"), S.Get("Paragraphs") };
            }
        }

        public int ComboBoxContextTypeSelectedIndex { get; set; }


        #endregion
        #region PDF Viewer Settings

        public OptionPDFViewer OptionPDFViewer
        {
            get
            {
                if (IsRadioPDFViewerChecked) return OptionPDFViewer.PdfJS;
                if (IsRadioPDFAcrobatChecked) return OptionPDFViewer.Acrobat;
                if (IsRadioPDFPlainTextChecked) return OptionPDFViewer.PlainText;
                return OptionPDFViewer.PlainText;
            }
            set
            {
                switch(value)
                {
                    case OptionPDFViewer.PdfJS:
                        IsRadioPDFViewerChecked = true; break;
                    case OptionPDFViewer.Acrobat:
                        IsRadioPDFAcrobatChecked = true; break;
                    case OptionPDFViewer.PlainText:
                        IsRadioPDFPlainTextChecked = true; break;
                }
            }
        }

        public bool IsRadioPDFViewerChecked
        {
            get; set;
        }

        public bool IsRadioPDFAcrobatChecked {
            get; set;
        }

        public bool IsRadioPDFPlainTextChecked {
            get; set;
        }

        #endregion
        #region View file type

        #endregion
        #region Hit Highlight

        #endregion
    }
}
