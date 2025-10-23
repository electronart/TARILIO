
using ReactiveUI;
using System.Diagnostics;

namespace eSearch.ViewModels
{
    public class TaskDialogWindowViewModel : ViewModelBase
    {
        /* Buttons */
        public bool Button1Visible { get; set; }
        public bool Button2Visible { get; set; }
        public bool Button3Visible { get; set; }

        public string Button1Text { get; set; }
        public string Button2Text { get; set; }
        public string Button3Text { get; set; }
        /* Main Instruction / Content */
        public bool     MainInstructionVisible { get; set; }
        public string MainInstructionText { get; set; }
        public bool     ContentVisible { get; set; }
        public string   ContentText { get; set; }

        public bool ShowDetails { 
            get
            {
                return _showDetails;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _showDetails, value);
            }
        }

        private bool _showDetails = false;

        public string? DetailsText
        {
            get
            {
                return _detailsText;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _detailsText, value);
            }
        }

        private string? _detailsText = null;

        /// <summary>
        /// Pass String.empty on any element to hide it.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <param name="button1"></param>
        /// <param name="button2"></param>
        /// <param name="button3"></param>
        public TaskDialogWindowViewModel(string title, string text, string button1, string button2, string button3, string? details = null)
        {
            MainInstructionText = title;
            MainInstructionVisible = title != string.Empty;
            ContentText = text;
            ContentVisible = text != string.Empty;
            Button1Text = button1;
            Button1Visible = button1 != string.Empty;
            Button2Text = button2;
            Button2Visible = button2 != string.Empty;
            Button3Text = button3;
            Button3Visible = button3 != string.Empty;
            DetailsText = details;
            Debug.WriteLine("Button3Text " + button3 + " isVisible: " + Button3Visible);
        }

        public TaskDialogWindowViewModel() : this("Example Main Instruction", "Example Content", "Button 1 Txt", "Button 2 Txt", "Button 3 Txt")
        {

        }
    }
}
