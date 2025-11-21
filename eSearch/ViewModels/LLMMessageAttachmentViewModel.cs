using eSearch.Models.Documents;
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
        public async Task<string> ParseOrGetCachedParsedText()
        {
            if (_parsedAttachmentText == null)
            {
                // Not yet parsed.
                FileSystemDocument fsd = new FileSystemDocument();
                fsd.SetDocument(FilePath);
                await fsd.PreloadDocument();
                var parseResult = fsd.GetParseResult();
                StringBuilder sb = new StringBuilder();
                string textContent = fsd.Text ?? "TARILIO Could not extract text contents from this file.";
                sb.AppendLine(textContent);
                _parsedAttachmentText = sb.ToString();
            }
            return _parsedAttachmentText;
        }

        private string? _parsedAttachmentText = null;
    }
}
