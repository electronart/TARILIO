using Avalonia.Platform.Storage;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using S = eSearch.ViewModels.TranslationsViewModel;
using System.IO;
using System.Globalization;
using CsvHelper;
using System.Diagnostics;
using CsvHelper.Configuration.Attributes;
using CsvHelper.Configuration;
using DocumentFormat.OpenXml.Wordprocessing;
using DynamicData;
using eSearch.Models;
using System.ComponentModel;
using System.Xml;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using System.IO;
using System.Text.Json.Serialization;
using eSearch.Models.Configuration;
using eSearch.Utils;
using Avalonia.Controls.Selection;

namespace eSearch.ViewModels
{
    public class ExportSearchResultsViewModel : ViewModelBase, IValidatableViewModel
    {

        public ExportSearchResultsViewModel()
        {
            this.ValidationRule(
                viewModel => viewModel.SelectedOutputFileTypeIndex,
                selectedIndex => selectedIndex == -1, S.Get("Select output file type")
            );
        }

        public static ExportSearchResultsViewModel FromExportConfig(ExportConfig config, DataColumn[] possible_columns)
        {
            var vm = new ExportSearchResultsViewModel();
            if (config.OutputFileName != null)
            {
                vm.FileNameInput = config.OutputFileName;
            }
            if (config.OutputDirectory != null)
            {
                vm.OutputDirectoryInput = config.OutputDirectory;
            }
            if (config.OutputTypeIndex != -1)
            {
                vm.SelectedOutputFileTypeIndex = config.OutputTypeIndex;
            }
            vm.AvailableColumns = new ObservableCollection<DataColumn>();
            foreach(var column in possible_columns)
            {
                vm.AvailableColumns.Add(column);
            }
            vm.AppendDateChecked = config.AppendDate;
            vm.ExportAllColumns = config.AllColumns;
            if (config.SelectedColumns != null)
            {
                foreach(var columnHeader in config.SelectedColumns)
                {
                    foreach(var possible_column in vm.AvailableColumns)
                    {
                        if (possible_column.Header == columnHeader)
                        {
                            vm.SelectedColumnsModel.Select(vm.AvailableColumns.IndexOf(possible_column));
                        }
                    }
                }
            }
            return vm;
        }

        public ValidationContext ValidationContext { get; } = new ValidationContext();



        public class OutputType
        {

            public OutputType(string fileExtension, Func<string> typeNameGetter)
            {
                FileExtension = fileExtension;
                _typeNameGetter = typeNameGetter;
            }

            private Func<string> _typeNameGetter = null;

            public string FileExtension;
            public string TypeName { 
                get 
                { 
                    if (_typeNameGetter != null) return _typeNameGetter(); 
                    else return null; 
                }  
            }

            public override string ToString()
            {
                return TypeName;
            }
        }

        [JsonIgnore]
        OutputType XML = new OutputType("XML", () => S.Get("XML (.xml)"));

        [JsonIgnore]
        OutputType CSV_Comma = new OutputType("CSV", () => S.Get("Comma Separated Values (.csv)"));

        [JsonIgnore]
        OutputType CSV_Tab = new OutputType("TXT", () => S.Get("Tab Separated (.txt)"));

        public bool SelectedRowsOnly
        {
            get
            {
                return _selectedRowsOnly;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedRowsOnly, value);
            }
        }

        private bool _selectedRowsOnly = false;

        public int SelectedOutputFileTypeIndex
        {
            get
            {
                /*
                if (_selectedOutputFileTypeIndex == -1)
                {
                    var lastOutputType = Program.ProgramConfig.LastOutputType;
                    if (lastOutputType != null)
                    {
                        _selectedOutputFileTypeIndex = OutputFileTypes.IndexOf(lastOutputType);
                    }
                }
                */
                return _selectedOutputFileTypeIndex;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedOutputFileTypeIndex, value);
            }
        }

        private int _selectedOutputFileTypeIndex = 1;

        public OutputType[] OutputFileTypes
        {
            get
            {
                return new OutputType[] {
                    XML,
                    CSV_Comma,
                    CSV_Tab
                };
            }
        }

        public bool ExportAllColumns
        {
            get
            {
                return _exportAllColumns;
            }
            set
            {
                SelectedColumnsModel.Clear();
                this.RaiseAndSetIfChanged(ref _exportAllColumns, value);
                if (value)
                {
                    SelectedColumnsModel.SelectAll();
                }
            }
        }

        private bool _exportAllColumns = true;

        public ObservableCollection<DataColumn> AvailableColumns
        {
            get
            {
                return _availableColumns;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _availableColumns, value);
            }
        }

        private ObservableCollection<DataColumn> _availableColumns = new ObservableCollection<DataColumn>();

        public SelectionModel<DataColumn> SelectedColumnsModel
        {
            get
            {
                if (_selectedColumnsModel == null)
                {
                    _selectedColumnsModel = new SelectionModel<DataColumn>
                    {
                        SingleSelect = false
                    };
                    _selectedColumnsModel.Source = _availableColumns;
                }
                return _selectedColumnsModel;
            }
        }

        private SelectionModel<DataColumn> _selectedColumnsModel = null;

        public Uri OutputDirectory
        {
            get
            {
                if (_outputDirectory == null)
                {
                    _outputDirectory = new Uri(Program.ESEARCH_EXPORT_DIR);
                }
                return _outputDirectory;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _outputDirectory, value);
            }
        }

        private Uri _outputDirectory = null;

        public string OutputFileNameExcludingPathAndExtension
        {
            get
            {
                if (_outputFileName == null)
                {
                    _outputFileName = S.Get("Results");
                }
                return _outputFileName;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _outputFileName, value);
            }
        }

        

        private string _outputFileName = null;


        public string FileNameInput
        {
            get
            {
                if (_fileNameInput == null)
                {
                    _fileNameInput = OutputFileNameExcludingPathAndExtension;
                }
                return _fileNameInput;
            } set
            {
                this.RaiseAndSetIfChanged(ref _fileNameInput, value);
            }
        }

        private string _fileNameInput = null;


        public bool AppendDateChecked
        {
            get
            {
                return _appendDateChecked;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _appendDateChecked, value);
            }
        }

        private bool _appendDateChecked = true;

        public string OutputDirectoryInput
        {
            get
            {
                if (_outputDirectoryInput == null)
                {
                    _outputDirectoryInput = Program.GetMainWindow().StorageProvider.TryGetWellKnownFolderAsync(Avalonia.Platform.Storage.WellKnownFolder.Documents).Result.Path.LocalPath;
                }
                return _outputDirectoryInput;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _outputDirectoryInput, value);
            }
        }

        private string _outputDirectoryInput = null;

        public async void BrowseForOutputDirectory()
        {
            var openFolderDialog = new Avalonia.Controls.OpenFolderDialog();
            var initialDiretory = OutputDirectoryInput;
            openFolderDialog.Directory = initialDiretory;

            var res = await openFolderDialog.ShowAsync(Program.GetMainWindow());
            if (res != null)
            {
                // res is a directory.
                OutputDirectoryInput = res;
            }
        }


        public void ExportResultsBasedOnSettings(IEnumerable<ResultViewModel> results, string saveTo)
        {
            try
            {
                string strWrite = "";
                var selectedOutputFileType = OutputFileTypes[SelectedOutputFileTypeIndex];


            


                if (selectedOutputFileType == CSV_Comma)
                {
                     strWrite = BuildCSVData(results, false);
                }
                if (selectedOutputFileType == CSV_Tab)
                {
                    strWrite = BuildCSVData(results, true);
                }
                if (selectedOutputFileType == XML)
                {
                    strWrite = BuildXMLData(results);
                }



                Debug.WriteLine("Would Write:");
                Debug.WriteLine(strWrite);
                Debug.WriteLine(":)");


            
                File.WriteAllText(saveTo + "." +  selectedOutputFileType.FileExtension.ToLower(), strWrite);
            } catch (Exception ex)
            {
                Debug.WriteLine("An exception occurred!");
                Debug.WriteLine(ex.ToString());
            }
        }


        private string BuildXMLData(IEnumerable<ResultViewModel> results)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode root = xmlDoc.CreateElement("Results");
            foreach(var result in results)
            {
                if (SelectedRowsOnly && !result.IsResultChecked) continue;
                var resultNode = xmlDoc.CreateElement("Result");
                foreach(var selectedColumn in SelectedColumnsModel.SelectedItems)
                {
                    var columnValue = GetResultValueAsString(result, selectedColumn);
                    var valueNode = xmlDoc.CreateElement(selectedColumn.Header);
                    if (columnValue != null)
                    {
                        valueNode.InnerText = columnValue.ToString();
                    } else
                    {
                        valueNode.InnerText = string.Empty;
                    }
                    resultNode.AppendChild(valueNode);
                }
                root.AppendChild(resultNode);
            }
            xmlDoc.AppendChild(root);
            return xmlDoc.OuterXml;
        }

        private object GetResultValueAsString(ResultViewModel result, DataColumn column)
        {
            try
            {
                var value = eSearch.Models.Utils.GetValueByPath(result, column.BindTo);
                if (value == null)
                {
                    return "null";
                } else
                {
                    return value;
                }
            } catch (ArgumentException ex)
            {
                return "";
            }
        }

        private string BuildCSVData(IEnumerable<ResultViewModel> results, bool tabs)
        {

            var records = new List<dynamic>();
            foreach (var result in results)
            {
                if (SelectedRowsOnly && !result.IsResultChecked) continue;
                ExpandoObject record = new ExpandoObject();
                foreach(var selectedColumn in SelectedColumnsModel.SelectedItems)
                {
                    // TODO this is temporary until we get a proper column solution.
                    record.AddProperty(selectedColumn.Header, GetResultValueAsString(result, selectedColumn));
                }
                records.Add(record);
            }
            using (var writer = new StringWriter())
            {
                using (var csv = GetWriter(writer, tabs))
                {
                    csv.WriteRecords(records);
                }
                return writer.ToString();
            }
        }

        private CsvWriter GetWriter(StringWriter stringWriter, bool tabs)
        {
            if (tabs)
            {
                var config = CsvConfiguration.FromAttributes<CSVTabs>();
                return new CsvWriter(stringWriter, config);
            } else
            {
                return new CsvWriter(stringWriter, CultureInfo.InvariantCulture);
            }
        }

    }



    [Delimiter("\t")]
    [CultureInfo("InvariantCulture")]
    public class CSVTabs
    {
        
    }




}
