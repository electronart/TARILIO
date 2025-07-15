using eSearch.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Configuration
{
    public class CopyConversationConfig
    {
        public string? SavePath = null;

        public enum CopyToOption
        {
            Clipboard,
            File
        }

        public CopyToOption? CopyTo = null;

        public string? CopyToFileName = null;

        public bool? AppendDate = null;

        public bool? AppendNote = null;

        public CopyConversationWindowViewModel ToViewModel()
        {
            CopyConversationWindowViewModel viewModel = new CopyConversationWindowViewModel();
            if (SavePath != null) viewModel.SavePath = SavePath;
            if (CopyTo != null)
            {
                viewModel.IsRadioClipBoardChecked = CopyTo == CopyToOption.Clipboard;
                viewModel.IsRadioFileChecked = CopyTo == CopyToOption.File;
            }
            if (AppendDate != null) viewModel.AppendDateIsChecked = (bool)AppendDate;
            if (AppendNote != null) viewModel.AppendNoteChecked = (bool)AppendNote;
            if (CopyToFileName != null) viewModel.CopyToFileName = CopyToFileName;
            return viewModel;
        }

        public static CopyConversationConfig FromViewModel(CopyConversationWindowViewModel viewModel)
        {
            CopyConversationConfig copyDocumentConfig = new CopyConversationConfig
            {
                SavePath = viewModel.SavePath,
                CopyTo = viewModel.IsRadioClipBoardChecked ? CopyToOption.Clipboard : CopyToOption.File,
                AppendDate = viewModel.AppendDateIsChecked,
                AppendNote = viewModel.AppendNoteChecked,
                CopyToFileName = viewModel.CopyToFileName
            };
            return copyDocumentConfig;
        }


    }
}
