using eSearch.Models.Documents;
using eSearch.Models.Documents.Parse;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class LLMMessageAttachmentViewModel : ViewModelBase
    {

        public LLMMessageAttachmentViewModel(string filePath)
        {
            this.FilePath = filePath;
            this.Filename = Path.GetFileName(filePath);
        }

        public string FilePath
        {
            get => _filePath;
            set => this.RaiseAndSetIfChanged(ref _filePath, value);
        }

        private string _filePath = string.Empty;

        // Filename without the path.
        public string Filename
        {
            get => _fileName;
            set => this.RaiseAndSetIfChanged(ref _fileName, value);
        }

        private string _fileName = string.Empty;

        /// <summary>
        /// Blocking method. Attempts to parse the document and retrieve its parsed contents.
        /// </summary>
        /// <returns></returns>
        public async Task<ParseResult> ParseOrGetCachedParseResult()
        {
            if (_parsedAttachment == null)
            {
                // Not yet parsed.
                FileSystemDocument fsd = new FileSystemDocument();
                fsd.SetDocument(FilePath);
                _parsedAttachment = fsd.GetParseResult();
            }
            return _parsedAttachment;
        }

        private ParseResult? _parsedAttachment = null;
    }
}
