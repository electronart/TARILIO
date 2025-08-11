using eSearch.Interop;
using eSearch.Models.DataSources;
using eSearch.Models.Indexing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static eSearch.ViewModels.ResultsSettingsWindowViewModel;

namespace eSearch.Models.Configuration
{
    public interface IIndexConfiguration
    {
        public ObservableCollection<DataColumn> ColumnDisplaySettings
        {
            get; set;
        }

        public ColumnWidthOption ColumnSizingMode { get; set; }

        /// <summary>
        /// File Paths to files containing Stop Words, One per line.
        /// </summary>
        public List<string> SelectedStopWordFiles { get; set; }

        /// <summary>
        /// The datasource that includes everything. If an index has more than one datasource this will return a MultiDataSource
        /// </summary>
        public IDataSource GetMainDataSource();

        /// <summary>
        /// File types that should be included in the index. 
        /// Files that do not match any of the file extensions will be excluded from the index.
        /// When null (default), treat as include all files.
        /// </summary>
        public List<string> SelectedFileExtensions { get; set; }

        public int MaximumIndexedWordLength { get; set; }

        public int MaximumIndexedFileSizeMB { get; set; }


        public bool IsIndexCaseSensitive { get; set; }

        /// <summary>
        /// Null when not scheduled.
        /// </summary>
        public IndexSchedule? AutomaticUpdates
        {
            get; set;
        }
    }
}
