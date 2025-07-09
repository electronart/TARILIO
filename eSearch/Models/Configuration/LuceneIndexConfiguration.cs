using eSearch.Models.DataSources;
using eSearch.ViewModels;
using DynamicData;
using Newtonsoft.Json;
using NPOI.OpenXmlFormats.Dml;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eSearch.Interop;
using eSearch.Models.Logging;
using eSearch.Interop.IDataSourceExtensions;
using eSearch.Models.Documents;
using eSearch.Models.Indexing;

namespace eSearch.Models.Configuration
{
    [JsonObject(ItemTypeNameHandling = TypeNameHandling.Arrays)] // Ensure  this object is understood as a LuceneIndexConfiguration, not just an IIndexConfiguration when serialized/deserialized from an array of IIndexConfiguration.
    public class LuceneIndexConfiguration : ViewModelBase, IIndexConfiguration
    {

        
        public LuceneIndex                  LuceneIndex          = null;
        /// <summary>
        /// Any Directories that should be indexed along with configuration.
        /// </summary>
        public List<DirectoryDataSource>    DirectoryDataSources = new List<DirectoryDataSource>();
        /// <summary>
        /// List of FilePaths that should be indexed.
        /// </summary>
        public List<FileDataSource>         FileDataSources                  = new List<FileDataSource>();

        /// <summary>
        /// File Paths to files containing Stop Words, One per line.
        /// </summary>
        [JsonIgnore]
        public List<string> _selectedStopWordFiles = new List<string>();

        public List<string> SelectedStopWordFiles
        {
            get
            {
                return _selectedStopWordFiles;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedStopWordFiles, value);
            }
        }

        public int MaximumIndexedFileSizeMB { get; set; }

        public int MaximumIndexedWordLength { get; set; }

        public bool IsIndexCaseSensitive
        {
            get
            {
                return _isIndexCaseSensitive;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isIndexCaseSensitive, value);
            }
        }

        private bool _isIndexCaseSensitive = false;


        public async Task<MultipleSourceDataSource> GetMultiDataSource()
        {
#if DEBUG
            DebugLogger debugLogger = new DebugLogger();
#endif

            FileParserForPlugins fileParserForPlugins = new FileParserForPlugins();
            List<IDataSource> dataSources = new List<IDataSource>();
            dataSources.AddRange(DirectoryDataSources);
            dataSources.AddRange(FileDataSources);
            #region Handle Plugin DataSources...
            foreach(var plugin in await Program.PluginLoader.GetInstalledPlugins())
            {
                foreach(var dsManager in plugin.GetPluginDataSourceManagers())
                {
                    foreach(var ds in dsManager.GetConfiguredDataSources(LuceneIndex.Id))
                    {
#if DEBUG
                        ds.UseIndexTaskLog(debugLogger);
#endif
                        if (ds is IRequiresESearchFileParser requiresParser)
                        {
                            requiresParser.SetESearchFileParser(fileParserForPlugins);
                        }
                        dataSources.Add(ds);
                    }
                }
            }
            #endregion
            MultipleSourceDataSource multiSource = new MultipleSourceDataSource(dataSources);
            return multiSource;
        }

        public IDataSource GetMainDataSource()
        {
            return GetMultiDataSource().Result;
        }

        [JsonIgnore]
        public ResultsSettingsWindowViewModel.ColumnWidthOption ColumnSizingMode {
            get
            {
                return _columnSizingMode;
            }
            set
            {
                _columnSizingMode = value;
            }
        }

        [JsonProperty("ColumnSizingMode")]
        private ResultsSettingsWindowViewModel.ColumnWidthOption _columnSizingMode = ResultsSettingsWindowViewModel.ColumnWidthOption.SetManually;

        [JsonIgnore]
        public ObservableCollection<DataColumn> ColumnDisplaySettings
        {
            get
            {
                if (_columnDisplaySettings == null) // Condition is only met after an index is created. Otherwise ColumnDisplaySettings are persisted when IndexLibrary is saved.
                {
                    var columns = LuceneIndex.GetAvailableColumns();
                    _columnDisplaySettings = new ObservableCollection<DataColumn>();
                    _columnDisplaySettings.AddRange(columns);
                }

                var availableColumns = LuceneIndex.GetAvailableColumns();
                #region 1. Check if any Columns have since become available.
                foreach (var availableColumn in availableColumns)
                {
                    var match = _columnDisplaySettings.FirstOrDefault(c => c.Header.ToLower() == availableColumn.Header.ToLower());
                    if (match == null)
                    {
                        availableColumn.Visible = true; // Always make new columns visible.
                        _columnDisplaySettings.Add(availableColumn);
                    }
                }
                #endregion
                #region 2. Check if any Columns have ceased to exist in the index.
                int i = _columnDisplaySettings.Count;
                while (i --> 0)
                {
                    var existingColumn = _columnDisplaySettings[i];
                    var match = availableColumns.FirstOrDefault(c => c.Header.ToLower() ==  existingColumn.Header.ToLower());
                    if (match == null)
                    {
                        // This column is no longer available.
                        _columnDisplaySettings.RemoveAt(i);
                    }
                }
                #endregion
                #region 3. Prune out dupes?
                i = _columnDisplaySettings.Count;
                while (i --> 0)
                {
                    var column = _columnDisplaySettings[i];
                    int x = 0;
                    while (x < i)
                    {
                        if (_columnDisplaySettings[x].Header.ToLower() == column.Header.ToLower())
                        {
                            _columnDisplaySettings.RemoveAt(i);
                            break;
                        }
                        ++x;
                    }
                }
                #endregion
                
                return _columnDisplaySettings;
            }
            set
            {
                #region Ensure all new columns to display have the correct sorting (Necessary after upgrade to TreeDataGrid)
                var availableColumns = LuceneIndex.GetAvailableColumns();
                foreach(var newDataColumnSetting in value)
                {
                    var equivColumn = availableColumns.FirstOrDefault(c => c?.Header == newDataColumnSetting?.Header, null);
                    if (equivColumn != null)
                    {
                        newDataColumnSetting.CustomSortField = equivColumn.CustomSortField;
                        newDataColumnSetting.BindTo = equivColumn.BindTo;
                    }
                }
                #endregion
                this.RaiseAndSetIfChanged(ref _columnDisplaySettings, value);
            }
        }

        

        [JsonProperty("ColumnDisplaySettings")]
        private ObservableCollection<DataColumn> _columnDisplaySettings = null;


        public List<string> SelectedFileExtensions
        {
            get
            {
                return _selectedFileExtensions;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedFileExtensions, value);
            }
        }

        [JsonIgnore]
        private List<string>? _selectedFileExtensions = null;
    }
}
