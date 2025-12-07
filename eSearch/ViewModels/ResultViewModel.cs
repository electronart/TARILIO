using Avalonia.Controls.Documents;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using eSearch.Models.Documents.Parse;
using eSearch.Models.Search;
using eSearch.Utils;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class ResultViewModel : ViewModelBase
    {

        private static SemaphoreSlim fieldLoadingSemaphore = new SemaphoreSlim(8, 8);
        private readonly IResult _result;

        public ResultViewModel(IResult result)
        {
            _result = result;
        }

        public int ResultIndex => _result.ResultIndex;

        public string Title => _result.Title;

        public string DisplayedTitle
        {
            get
            {
                string temp = Title;
                if (Title.Length > 80)
                {
                    temp = temp.Substring(0, 79);
                    temp = temp + "…";
                }
                temp = temp.Replace("\n", String.Empty);
                temp = temp.Replace("\r", String.Empty);
                temp = temp.Replace("\t", " ");
                return temp;
            }
        }

        public string Identifier => _result.GetFieldValue("_Identifier", "---");
        public string Text => _result.ExtractHtmlRender(false, false);

        public string FilePath  => _result.GetFieldValue("_DocFSPath", "---");

        public string FileName {  
            get
            {
                return Path.GetFileName(FilePath);
            } 
        }

        public string FileExtension
        {
            get
            {
                return Path.GetExtension(FilePath)?.ToLower().Replace(".","") ?? "Unknown";
            }
        }

        public long FileSize
        {
            get
            {
                var size = _result.GetFieldValue("_FileSize", "0");
                return long.Parse(size);
            }
        }

        

        public string FileSizeHumanFriendly
        {
            get
            {
                // SO 281640
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = FileSize;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }

                // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
                // show a single decimal place, and no space.
                return String.Format("{0:0} {1}", len, sizes[order]);
            }
        }

        public string Parser => _result.Parser;
        public int Score => _result.Score;
        public string Context => _result.Context;


        public InlineCollection? FormattedContextForContentView
        {
            get
            {
                if (_loadContextTask == null)
                {
                    _loadContextTask = new Task(() =>
                    {
                        try
                        {
                            ObservableCollection<Inline> formattedContext = new ObservableCollection<Inline>();
                            var context = _result.GetHitsInContextAsRuns(2);
                            Dispatcher.UIThread.Invoke(() =>
                            {
                                foreach (var chunk in context)
                                {
                                    var run = new Avalonia.Controls.Documents.Run { Text = chunk.Text };
                                    if (chunk.IsHit)
                                    {
                                        run.FontWeight = Avalonia.Media.FontWeight.Bold;
                                    }
                                    _formattedContextForContentView.Add(run);
                                }
                            });
                        } catch (Exception ex)
                        {
#if DEBUG
                            Debug.WriteLine($"Error Getting Context as Runs: {ex.Message}");
#endif  
                        }
                        
                    });
                    _loadContextTask.Start();
                }
                return _formattedContextForContentView;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _formattedContextForContentView, value);
            }
        }

        private Task? _loadContextTask = null;

        private InlineCollection? _formattedContextForContentView = new InlineCollection();

        /// <summary>
        /// Do not use SET - This is an avalonia hack
        /// Use SetResultChecked
        /// </summary>
        public bool IsResultChecked
        {
            get
            {
                return _isResultChecked;
            }
            set
            {
                // HACK - Disable setting whilst keeping the checkbox enabled in the UI by making the setter appear to be there.
            }
        }

        public void SetResultChecked(bool isChecked)
        {
            _isResultChecked = isChecked;
            this.RaisePropertyChanged(nameof(IsResultChecked));
        }

        private bool _isResultChecked = false;

        public string CreatedDsp
        {
            get
            {
                var storedDateStr = _result.GetFieldValue("_DateCreated", null);
                if (storedDateStr != null)
                {
                    var date = DateUtils.Deserialize(storedDateStr);
                    if (date != null)
                    {
                        return LocalISODate((DateTime)date);
                    }
                }

                if (_result.Document.CreatedDate != null)
                {
                    return LocalISODate((DateTime)_result.Document.CreatedDate);
                }
                return "Unknown";
            }
        }

        private string LocalISODate(DateTime date)
        {
            return date.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }

        public List<Metadata> DocumentMetaData
        {
            get
            {
                return _result.Metadata.ToList();
            }
        }
        public List<Metadata> VisibleDocumentMetaData
        {
            get
            {
                List<Metadata> temp = new List<Metadata>();
                foreach(var metaData in DocumentMetaData)
                {
                    if (!metaData.Key.StartsWith("_"))
                    {
                        temp.Add(metaData);
                    }
                }
                return temp;
            }
        }

        public IResult GetResult()
        {
            return _result;
        }

        /// <summary>
        /// Get value at field index.
        /// </summary>
        /// <param name="field_index">The index of the field in known field of the index this result belongs to.</param>
        /// <returns></returns>
        public CustomSortingCellValue this[int field_index]
        {
            get
            {
                if ((field_index + 1) > _result.Index.KnownFieldNames.Count)
                {
                    return new CustomSortingCellValue { value = "null" };
                }
                string fieldName = _result.Index.KnownFieldNames[field_index];

                if (!string.IsNullOrEmpty(fieldName))
                {
                    int m = DocumentMetaData.Count;
                    while (m --> 0)
                    {
                        if (DocumentMetaData[m].Key.ToLower() == fieldName.ToLower())
                        {
                            string value = DocumentMetaData[m].Value;
                            if (IsNumeric(value)) // Detect Integers/Longs and return them as longs for sorting purposes.
                            {
                                if (long.TryParse(value, out var result))
                                {
                                    return new CustomSortingCellValue { value = result };
                                }
                            }
                            return new CustomSortingCellValue { value = DocumentMetaData[m].Value };
                        }
                    }
                }
                return new CustomSortingCellValue { value = "-" };
            }
        }

        public static bool IsNumeric(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            int index = 0;
            int length = input.Length;

            // Handle optional negative sign
            if (index < length && input[index] == '-')
                index++;

            // Must have at least one digit
            if (index >= length)
                return false;

            bool hasDigit = false;

            while (index < length)
            {
                char c = input[index];

                if (c >= '0' && c <= '9')
                {
                    hasDigit = true;
                }
                else
                {
                    return false;
                }

                index++;
            }

            return hasDigit;
        }

        public string ModifiedDsp
        {
            get
            {
                var modifiedDateStr = _result.GetFieldValue("_DateModified", null);
                if (modifiedDateStr != null)
                {
                    var date = DateUtils.Deserialize(modifiedDateStr);
                    if (date != null)
                    {
                        return LocalISODate((DateTime)date);
                    }
                }

                if (_result.Document.ModifiedDate != null)
                {
                    return LocalISODate((DateTime)_result.Document.ModifiedDate);
                }
                return "Unknown";
            }
        }

        public string IndexedDsp
        {
            get
            {
                var dateIndexedStr = _result.GetFieldValue("_DateIndexed", null);
                if (dateIndexedStr != null)
                {
                    var date = DateUtils.Deserialize(dateIndexedStr);
                    if (date != null )
                    {
                        return LocalISODate((DateTime)date);
                    }
                }

                if (_result.Document.IndexedDate != null)
                {
                    return LocalISODate((DateTime)_result.Document.IndexedDate);
                }
                return "Unknown";
            }
        }

        public string AccessedDsp
        {
            get
            {
                var dateAccessedStr = _result.GetFieldValue("_DateAccessed", null);
                if (dateAccessedStr != null )
                {
                    var date = DateUtils.Deserialize(dateAccessedStr);
                    if (date != null)
                    {
                        return LocalISODate((DateTime)date);
                    }
                }
                if (_result.Document.AccessedDate != null)
                {
                    return LocalISODate((DateTime)_result.Document.AccessedDate);
                }
                return "Unknown";
            }
        }

        /// <summary>
        /// Attempt to extract the Result Render html from the document.
        /// </summary>
        /// <param name="escape_html">When true, escapes html (except highlight tags)</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">May be thrown when eSearch does not support html render on given document type</exception>
        public string ExtractHtmlRender(bool escape_html = false, bool hit_highlight = false)
        {
            return _result.ExtractHtmlRender(escape_html, hit_highlight);
        }

        public string HighlightTextAccordingToResultQuery(string text)
        {
            return _result.HighlightTextAccordingToResultQuery(text);
        }

        public List<(int Start, int End)> GetHighlightRanges(string textToHighlight)
        {
            return _result.GetHighlightRanges(textToHighlight);
        }

        public Bitmap? ThumbnailMedium
        {
            get {
                if (_loadThumbnailMediumTask == null)
                {
                    // Not yet attempted to load the thumbnail, start the task.
                    _loadThumbnailMediumTask = new Task(async () =>
                    {
                        int retries = 0;
                    retryPoint:
                        fieldLoadingSemaphore.Wait();
                        
                    
                        try
                        {
                        
                            var thumb = WindowsAvaloniaThumbnailProvider.GetMediumThumbnail(FilePath);
                            ThumbnailMedium = thumb;


                        }
                        catch (COMException com)
                        {
                            ++retries;
                            if (retries < 3)
                            {
                                await Task.Delay(TimeSpan.FromSeconds(3));
                                goto retryPoint;
                            }
                        }
                        catch (Exception ex)
                        {
#if DEBUG
                            Debug.WriteLine($"Error Fetching Thumbnail: {ex.Message}");
#endif
                        }
                        finally
                        {
                            fieldLoadingSemaphore.Release();
                        }
                    });
                    _loadThumbnailMediumTask.Start();
                }
                return _thumbnailMedium;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _thumbnailMedium, value);
            }
        }

        
        private Task?   _loadThumbnailMediumTask = null;
        private Bitmap? _thumbnailMedium = null;
    }
}
