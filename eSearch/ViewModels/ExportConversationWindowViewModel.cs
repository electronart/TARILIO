using Lucene.Net.Queries.Function.ValueSources;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class ExportConversationWindowViewModel : ViewModelBase
    {
        public string FileName
        {
            get
            {
                if (_fileName == null)
                {
                    _fileName = "Conversation";
                }
                return _fileName;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _fileName, value);
            }
        }

        private string? _fileName;

        public string ExportDirectory
        {
            get
            {
                if (_exportDirectory == null)
                {
                    _exportDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "eSearch");
                }
                return _exportDirectory;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _exportDirectory, value);
            }
        }

        private string? _exportDirectory;

        public bool AppendDate
        {
            get
            {
                return true;
                //return _appendDate;
            }
            //set
            //{
            //    this.RaiseAndSetIfChanged(ref _appendDate, value);
            //}
        }

        //private bool _appendDate = false;

        public ObservableCollection<ExportFormat> AvailableExportFormats
        {
            get
            {
                if (_availableExportFormats == null)
                {
                    _availableExportFormats =
                    [
                        new ExportFormat
                        {
                            Extension = "econvo",
                            Description = "eSearch Conversation"
                        },
                        new ExportFormat
                        {
                            Extension = "json",
                            Description = "JSON"
                        },
                        new ExportFormat {
                            Extension = "csv",
                            Description = "Comma Seperated Values"
                        },
                    ];
                }
                return _availableExportFormats;
            }
        }

        private ObservableCollection<ExportFormat>? _availableExportFormats;

        public ExportFormat SelectedExportFormat
        {
            get
            {
                if (_selectedExportFormat == null)
                {
                    _selectedExportFormat = AvailableExportFormats.First();
                }
                return _selectedExportFormat;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedExportFormat, value);
            }
        }

        private ExportFormat? _selectedExportFormat;


        public class ExportFormat
        {
            public required string Extension;
            public required string Description;

            public override string ToString()
            {
                return $"{Description} (.{Extension})";
            }
        }
    }
}
