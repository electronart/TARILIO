using eSearch.Interop;
using eSearch.Models.Configuration;
using eSearch.Models.DataSources;
using eSearch.Models.Documents;
using eSearch.Models.TaskManagement;
using eSearch.ViewModels;
using ProgressCalculation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static eSearch.Interop.ILogger;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Models.Indexing
{
    public class IndexTask : IProgressQueryableTask
    {
        ILogger Logger = new IndexTaskLog();

        IDataSource Source;
        IIndex Index;
        ProgressViewModel progressView;
        
        bool append;
        bool removeNotFound;

        DateTime startTime;

        ManualResetEvent mrse = new ManualResetEvent(false);
        bool Cancelling = false;
        bool Cancelled  = false;

        public HashSet<string> FoundFileNames = new HashSet<string>();

        List<string> TempFilesToDelete = new List<string>();

        int RetrievedDocuments = 0;
        int IndexedDocuments = 0;

        private int _progress    = 0;
        private int _maxProgress = 1;
        private string _progressStatus  = "";

        public int GetProgress()
        {
            return _progress;
        }

        public int GetMaxProgress()
        {
            return _maxProgress;
        }

        public string GetStatusString()
        {
            return _progressStatus;
        }


        /// <summary>
        /// Construct Index Task
        /// </summary>
        /// <param name="source"></param>
        /// <param name="index"></param>
        /// <param name="append">Whether to append to the index, or recreate. True for append.</param>
        /// <param name="removeNotFound">During index update, remove any items that were no longer found during the update</param>
        public IndexTask(IDataSource source, IIndex index, ProgressViewModel progressView, bool append = true, bool removeNotFound = false, ILogger? customLogger = null)
        {
            Source = source;
            Index = index;
            this.progressView = progressView;
            this.append = append;
            if (customLogger != null)
            {
                this.Logger = customLogger;
            }
        }


        

        public ProgressViewModel GetProgressViewModel()
        {
            return progressView;
        }

        public IIndex GetIndex()
        {
            return Index;
        }

        public void PauseIndexing()
        {
            mrse.Reset();
        }

        public void ResumeIndexing()
        {
            mrse.Set();
        }

        public void RequestCancel()
        {
            Cancelling = true;
        }

        private Stopwatch _stopWatch = new Stopwatch();

        private Stopwatch _batchWatch = new Stopwatch();

        /// <summary>
        /// Warning - Thread blocking!! Should use a non-ui-thread to call this method.
        /// </summary>
        public void Execute()
        {
            // In case the Index Directory previously failed to create...
            System.IO.Directory.CreateDirectory(Index.GetAbsolutePath());
            
            startTime = DateTime.Now;
            Logger.Log(Severity.INFO, "Index Task Started", null);
            var indexConfig = Program.IndexLibrary.GetConfiguration(Index);
            _batchWatch.Start();
            try
            {
                progressView.BeginWatching(this);
                Source.Rewind();
                Source.UseIndexTaskLog(Logger);
                _progress = 3;
                _maxProgress = 100;
                _progressStatus = S.Get("Opening Index");

                // Null when index all file types.
                List<string> indexedExtensions = null;

                
                if (indexConfig != null)
                {
                    indexedExtensions = indexConfig.SelectedFileExtensions;
                    if (Source is ISupportsIndexConfigurationDataSource configurable)
                    {
                        configurable.UseIndexConfig(indexConfig);
                    }
                }

                bool opened = Index.OpenWrite(!append);
                if (!opened)
                {
                    Logger.Log(Severity.ERROR, "Failed to open index for writing - Is it already open?");
                    return;
                }

                try
                {
                    List<IDocument> documentBatch = new List<IDocument>(); // We add Documents in Batches of up to 512 or however many documents we retrieve in 3 seconds whichever comes first.
                    IDocument document = null;
                    bool isDiscoveryComplete;
                    do
                    {
                        if (_batchWatch.ElapsedMilliseconds > 3000 || documentBatch.Count > 1024)
                        {
                            try
                            {
                                Index.AddDocuments(documentBatch);
                                IndexedDocuments += documentBatch.Count;
                                documentBatch.Clear();
                                _batchWatch.Restart();
                            } catch (Exception ex)
                            {
                                Logger.Log(Severity.ERROR, "Exception whilst adding documents to index " + ex.ToString());
                            }
                        }
                        Source.GetNextDoc(out document, out isDiscoveryComplete);
#if DEBUG
                        if (document?.FileName?.Contains("007") ?? false)
                        {
                            Debug.WriteLine("007 Found..");
                        }
#endif
                        #region Check for Pause / Cancel
                        mrse.WaitOne(); // This is the point where the thread will pause if Pause() has been called.
                        
                        if (Cancelling)
                        {
                            Logger.Log(Severity.INFO, "Index task was cancelled");
                            Cancelled = true;
                            return;
                        }
                        #endregion
                        #region Add document to the index if we got a valid one.
                        if (document != null)
                        {
                            ++RetrievedDocuments;
                            #region Check if this is an indexed file type.
                            if (indexedExtensions != null)
                            {
                                string fileType = document.FileType;
                                if (!indexedExtensions.Contains(fileType) 
                                    && fileType != "Database Record"        // Eg. Contents of a CSV File.
                                    )
                                {
                                    // Skip - Not an indexed extension.
                                    continue;
                                }
                            }
                            #endregion
                            string displayName = document.DisplayName;
                            if (!string.IsNullOrWhiteSpace(document.FileName))
                            {
                                displayName = Path.GetFileNameWithoutExtension(document.FileName);
                            }
                            if (removeNotFound)
                            {
                                FoundFileNames.Add(document.FileName);
                            }

                            if (document.ShouldSkipIndexing == IDocument.SkipReason.DontSkip) // Some documents are skipped due to parse errors or being ignored file types etc.
                            {
#if DEBUG
                                int totalDiscovered = Source.GetTotalDiscoveredDocuments();
                                if (IndexedDocuments > totalDiscovered)
                                {
                                    Debug.WriteLine("???");
                                }
#endif

                                documentBatch.Add(document);
                                

                                if (document.ExtractedFiles != null)
                                {
                                    TempFilesToDelete.AddRange(document.ExtractedFiles);
                                }
                            }
                            else
                            {
                                switch(document.ShouldSkipIndexing)
                                {
                                    case IDocument.SkipReason.TooLarge:
                                        Logger.Log(Severity.INFO, "Skip " + document.FileName + " - Too large");
                                        break;
                                    case IDocument.SkipReason.ParseError:
                                        Logger.Log(Severity.WARNING, "Skip - " + document.FileName + " - Parse Error \n " + document.Text);
                                        break;
                                }
                            }

                            int totalDiscoveredDocs = Source.GetTotalDiscoveredDocuments();

                            _maxProgress = totalDiscoveredDocs;
                            _progress    = RetrievedDocuments;
                            string strTimeRemaining  = ProgressCalculator.GetHumanFriendlyTimeRemainingLocalizablePrecise(startTime, Source.GetProgress());



                            if (document.FileName != null)
                            {

                                string currentDoc = RetrievedDocuments.ToString("N0");
                                string totalDocs = totalDiscoveredDocs.ToString("N0");

                                _progressStatus = String.Format(
                                    S.Get("Indexing {0}"),
                                    Path.GetFileName(document.FileName)
                                )
                                    + "\n" + currentDoc + " / " + totalDocs
                                    + "\n" + strTimeRemaining;

                                
                            }
                        }
                        #endregion
                        if (document == null && !isDiscoveryComplete)
                        {
                            Thread.Sleep(50);
                        }
                    } while (document != null || !isDiscoveryComplete || documentBatch.Count > 0);
                    #region If we should remove documents not found, do that now.
                    if (removeNotFound)
                    {
                        _progressStatus = S.Get("Cleaning up...");              
                        int i = Index.GetTotalDocuments();
                        IDocument doc;
                        while (i-- > 0) // Loop through backwards since we're doing deletions
                        {
                            doc = Index.GetDocument(i);
                            if (!FoundFileNames.Contains(doc.FileName))
                            {
                                // Document no longer part of this index.
                                Index.RemoveDocument(doc);
                            }
                        }
                    }
                    #endregion
                }
                finally
                {
                    Index.CloseWrite();
                    Program.IndexLibrary.SaveLibrary(); // Necessary to ensure known field names etc are persisted in indexes.
                    if (TempFilesToDelete.Count > 0)
                    {
                        _progressStatus = S.Get("Cleaning up...");
                        foreach (var tempFile in TempFilesToDelete)
                        {
                            try
                            {
                                System.IO.File.Delete(tempFile);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(Severity.ERROR, "Failed to delete temp file " + tempFile, ex);
                            }
                        }
                    }

                    var finishStatus = new StringBuilder();
                    finishStatus.AppendLine(S.Get("Finished"));
                    try
                    {
                        Index.OpenRead();
                        finishStatus.AppendLine(String.Format(S.Get("{0} Document(s) Indexed"), IndexedDocuments.ToString("N0")));
                        Index.EnsureClosed();
                    } catch (Exception ex)
                    {
                        // Treat this as non-fatal.
                        Logger.Log(Severity.WARNING, "Error getting total number of documents in index", ex);
                    }

                    if (Logger.NumErrors > 0)
                    {
                        finishStatus.AppendLine(S.Get("Error(s) during indexing. Check log for details."));
                    }

                    _progressStatus = finishStatus.ToString();
                    _progress = 100;
                    _maxProgress = 100;
                    progressView.IsFinished = true;
                    Logger.Log(Severity.INFO, "Index Task Finished");
                }

            } catch (Exception ex)
            {
                Logger.Log(Severity.ERROR, "Fatal error during indexing", ex);
                Index.CloseWrite();

            } finally
            {
                progressView.EndWatching();
                string indexLog = Logger.BuildTxtLog("", "");


                File.WriteAllText(
                    Path.Combine(Index.GetAbsolutePath(), "IndexTask.txt"), indexLog );
            }
        }
    }
}
