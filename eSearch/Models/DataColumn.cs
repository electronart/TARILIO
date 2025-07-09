using Avalonia;
using eSearch.Models.Search;
using eSearch.ViewModels;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Models
{

    [JsonObject(MemberSerialization.OptOut)]
    public class DataColumn : ReactiveObject
    {
        /// <summary>
        /// Column that can be displayed on the UI
        /// </summary>
        /// <param name="displayIndex">Position of column on UI. Note that 0 is reserved for the checkboxes.</param>
        /// <param name="header">Header of Column to be displayed in UI</param>
        /// <param name="visible">Whether or not the Column should be displayed now</param>
        /// <param name="width">Column width in pixels</param>
        /// <param name="bindTo">What should be bound to in value cells under this column</param>
        /// <param name="customSortField">Internal Field that shall be used for sorting</param>
        /// <param name="isLiteField">Field is a lite field that shows up in the filters UI.</param>
        public DataColumn(int displayIndex, string header, bool visible, int width = 250, 
                          string bindTo = null, string? customSortField = null, 
                          bool isLiteField = false)
        {
            this.DisplayIndex = displayIndex;
            this.Header = header;
            this.BindTo = bindTo;
            this.Visible = visible;
            this.Width  = width;
            this.CustomSortField = customSortField;
        }

        public DataColumn() { }

        public static List<DataColumn> GetStandardColumns()
        {
            List<DataColumn> StdColumns = new List<DataColumn>
            {
                // Title
                new DataColumn(
                    0, S.Get("Title"),  
                    true, 400, nameof(ResultViewModel.DisplayedTitle), "Title", true),
                // File Name
                new DataColumn(
                    1,S.Get("Name"),   
                    true, 250, nameof(ResultViewModel.FileName), "_DocFSPath", true),
                // Score
                new DataColumn(
                    2,S.Get("Score"),  
                    true, 75, nameof(ResultViewModel.Score), "_Score", false),
                // Created
                new DataColumn(
                    3,S.Get("Created"), 
                    true, 200, nameof(ResultViewModel.CreatedDsp), "_DateCreated" ),
                // Modified
                new DataColumn(
                    4,S.Get("Modified"),
                    true, 200, nameof(ResultViewModel.ModifiedDsp), "_DateModified"),
                // Indexed
                new DataColumn(
                    5,S.Get("Indexed"), 
                    true, 200, nameof(ResultViewModel.IndexedDsp), "_DateIndexed"),
                // Accessed
                new DataColumn(
                    6,S.Get("Accessed"),
                    false, 200, nameof(ResultViewModel.AccessedDsp), "_DateAccessed"),
                // Path
                new DataColumn(
                    7,S.Get("Path"),   
                    true, 600, nameof(ResultViewModel.FilePath), "_DocFSPath", true),
                // File Size
                new DataColumn(
                    8,S.Get("Size"),   
                    true, 250, nameof(ResultViewModel.FileSizeHumanFriendly), "_FileSize"),
                // Parser
                new DataColumn(
                    9,S.Get("Parser"), 
                    false, 200, nameof(ResultViewModel.Parser), "_Parser", true),
                // File Type
                new DataColumn(
                    10,S.Get("Type"),   
                    true, 75, nameof(ResultViewModel.FileExtension), "_DocFSPath", true)
            };

            return StdColumns;
        }


        public string GetInternalFieldName()
        {
            if (CustomSortField != null) return CustomSortField;
            return Header;
        }


        /// <summary>
        /// Header of the column, as shown in the UI.
        /// </summary>
        public string Header
        {
            get { 
                if (_header == null)
                {
                    _header = string.Empty;
                }
                return _header;
            }
            set { this.RaiseAndSetIfChanged(ref _header, value); }
        }

        private string _header;

        public string BindTo
        {
            get { return _bindTo; }
            set { this.RaiseAndSetIfChanged(ref _bindTo, value); }
        }

        private string _bindTo;

        /// <summary>
        /// Whether or not this column is shown in the UI
        /// </summary>
        public bool     Visible
        {
            get { return _visible; }
            set { this.RaiseAndSetIfChanged(ref _visible, value); }
        }

        private bool _visible;

        /// <summary>
        /// Column Width in pixels
        /// </summary>
        public int Width
        {
            get { return _width; }
            set { this.RaiseAndSetIfChanged(ref _width, value); }
        }

        private int _width;

        public string CustomSortField
        {
            get { return _customSortMemberPath; }
            set
            {
                this.RaiseAndSetIfChanged(ref _customSortMemberPath, value);
            }
        }

        private string _customSortMemberPath = null;

        /// <summary>
        /// Preferred display index of this column.
        /// If no preference, returns int.MAXVALUE.
        /// </summary>
        public int DisplayIndex
        {
            get
            {
                return _displayIndex;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _displayIndex, value);
            }
        }

        private int _displayIndex = int.MaxValue;

        /// <summary>
        /// Overriden
        /// </summary>
        /// <returns>Column name</returns>
        public override string ToString()
        {
            return Header;
        }
    }
}
