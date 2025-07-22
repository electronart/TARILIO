using eSearch.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Configuration
{
    public class ExportConversationConfig
    {
        public string? Filename;
        public string? Directory;
        public bool AppendDate = true;
        public string? FormatExtension; // The format to use, by its extension. ie. json/econvo/csv

        public ExportConversationWindowViewModel ToViewModel()
        {
            ExportConversationWindowViewModel viewModel = new ExportConversationWindowViewModel();
            viewModel.FileName = Filename;
            viewModel.ExportDirectory = Directory;
            //viewModel.AppendDate = AppendDate;
            viewModel.SelectedExportFormat = FormatExtension != null ?
                                            viewModel.AvailableExportFormats
                                            .Where(x => x.Extension == FormatExtension).First()
                                            : viewModel.AvailableExportFormats.First();
            return viewModel;
        }

        public static ExportConversationConfig FromViewModel(ExportConversationWindowViewModel viewModel)
        {
            ExportConversationConfig config = new ExportConversationConfig();
            config.Filename = viewModel.FileName;
            config.Directory = viewModel.ExportDirectory;
            config.AppendDate = viewModel.AppendDate;
            config.FormatExtension = viewModel.SelectedExportFormat.Extension;
            return config;
        }

    }
}
