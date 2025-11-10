using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class LLMMessageAttachmentViewModel : ViewModelBase
    {
        // Filename without the path.
        public string Filename
        {
            get => _fileName;
            set => this.RaiseAndSetIfChanged(ref _fileName, value);
        }

        private string _fileName = string.Empty;

        /// <summary>
        /// Text returned when parsed as a FileSystemDocument.
        /// </summary>
        public string ParsedText
        {
            get => _parsedText;
            set => this.RaiseAndSetIfChanged(ref _parsedText, value);
        }

        private string _parsedText = string.Empty;
    }
}
