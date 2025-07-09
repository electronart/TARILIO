using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopSearch2.Models.Configuration
{
    public class ViewerConfig
    {

        #region View Large Files
        public int MaxFileSizeMB = 10;

        public enum OptionViewLargeFile
        {
            /// <summary>
            /// Same as any other file
            /// </summary>
            Fully = 0,
            /// <summary>
            /// Display just the first page
            /// </summary>
            FirstPageOnly = 2,
            /// <summary>
            /// Display a report
            /// </summary>
            InReportView = 4
        }

        public OptionViewLargeFile ViewLargeFileOption = OptionViewLargeFile.Fully;



        public int ReportViewContextAmount = 3;

        /// <summary>
        /// Controls what ReportViewContextAmount refers to.
        /// </summary>
        public enum OptionContextAmountType
        {
            Words = 0,
            Paragraphs = 1
        }

        public OptionContextAmountType ReportViewContextTypeOption = OptionContextAmountType.Paragraphs;
        #endregion
        #region PDF

        public OptionPDFViewer PDFViewerOption = OptionPDFViewer.PdfJS;

        public enum OptionPDFViewer
        {
            PdfJS = 0,
            Acrobat = 1,
            PlainText = 2
        }
        #endregion
        #region View File Types
        // ????
        #endregion

        public Color HitHighlightColor   = Color.FromArgb(Int32.Parse("00FF00", System.Globalization.NumberStyles.HexNumber));

        public string FontFamilyName = "Courier";

        public int FontSizePt       = 12;

        public bool NoWordWrap = false;


    }
}
