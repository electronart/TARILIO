using DocumentFormat.OpenXml.Bibliography;
using eSearch.Interop;
using eSearch.Interop.Indexing;
using eSearch.Models.Configuration;
using eSearch.Models.DataSources;
using eSearch.Models.Documents;
using eSearch.Models.TaskManagement;
using eSearch.ViewModels;
using Microsoft.VisualBasic;
using ProgressCalculation;
using System;
using System.Collections.Concurrent;
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

        private ConcurrentQueue<string> TempFilesToDelete = new ConcurrentQueue<string>();

        int RetrievedDocuments = 0;
        int IndexedDocuments = 0;

        private int _progress    = 0;
        private int _maxProgress = 1;
        private string _progressStatus  = "";

        private long _currentBatchSize = 0L;

        // For debugging queue contention
        private long _documentsProduced      = 0;
        private long _documentsPreloaded     = 0;
        private long _documentsFlushed       = 0;

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
            Execute(false);
        }

        public async Task Execute(bool throwOnFailedToOpen = false)
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
                HashSet<string>? indexedExtensions = null;


                if (indexConfig != null)
                {
                    if (indexConfig.SelectedFileExtensions != null)
                    {
                        indexedExtensions = new HashSet<string>(indexConfig.SelectedFileExtensions);
                    } else
                    {
                        indexedExtensions = null;
                    }
                    if (Source is ISupportsIndexConfigurationDataSource configurable)
                    {
                        configurable.UseIndexConfig(indexConfig);
                    }
                }
                /*
                 * Note - This may throw 'FailedToOpenIndexException'.
                 * It is rethrown intentionally (view catch logic)
                 */
                Index.OpenWrite(!append);

                try
                {
                    var docProcessingQueue  = new BlockingCollection<IDocument>(1000); // Documents that need preloading/processing
                    var readyDocQueue       = new ConcurrentQueue<IDocument>();        // Documents that are fully loaded/ready to be inserted into the index.
                    var workers = new List<Task>();

                    #region Set up workers for preloading documents
                    int numWorkers = 50;
                    for (int i = 0; i < numWorkers; i++)
                    {
                        #region Check for Pause / Cancel
                        mrse.WaitOne(); // This is the point where the thread will pause if Pause() has been called.

                        if (Cancelling)
                        {
                            Logger.Log(Severity.INFO, "Index task was cancelled");
                            Cancelled = true;
                            return;
                        }
                        #endregion

                        workers.Add(Task.Run(async () =>
                        {
                            while (!docProcessingQueue.IsCompleted)
                            {
                                mrse.WaitOne();  // Pause here too
                                if (docProcessingQueue.TryTake(out var docToParse, TimeSpan.FromMilliseconds(500)))
                                {
                                    try
                                    {
                                        #region Check if this is an indexed file type.
                                        // Do this before preloading to avoid preloading documents unecessarily.
                                        if (indexedExtensions != null)
                                        {
                                            string? fileTypeFast = docToParse.FileName != null ? Path.GetExtension(docToParse.FileName).Replace(".","").ToLower() : null;
                                            if (fileTypeFast != null)
                                            {
                                                if (!indexedExtensions.Contains(fileTypeFast)) {
                                                    continue;
                                                }
                                            }
                                            else
                                            {
                                                string fileType = docToParse.FileType; // Performance - Calling this results in IO due to magic number check.
                                                if (!indexedExtensions.Contains(fileType)
                                                    && fileType != "Database Record"        // Eg. Contents of a CSV File.
                                                    )
                                                {
                                                    // Skip - Not an indexed extension.
                                                    continue;
                                                }
                                            }
                                        }
                                        #endregion
                                        if (docToParse is IPreloadableDocument preloadableDoc)
                                        {
                                            await preloadableDoc.PreloadDocument();
                                        }
                                        
                                        if (docToParse.ExtractedFiles != null)
                                        {
                                            foreach ( var file in docToParse.ExtractedFiles )
                                            {
                                                TempFilesToDelete.Enqueue(file);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Log(Severity.ERROR, "Error Preloading Document", ex);
                                    }
                                    finally
                                    {
                                        if (docToParse.ShouldSkipIndexing == IDocument.SkipReason.DontSkip)
                                        {
                                            readyDocQueue.Enqueue(docToParse); // It's ready to be be parsed now.
                                            Interlocked.Add(ref _currentBatchSize, docToParse.FileSize); // Thread safe addition
                                            Interlocked.Increment(ref _documentsPreloaded);
                                        }
                                        else
                                        {
                                            switch (docToParse.ShouldSkipIndexing)
                                            {
                                                case IDocument.SkipReason.TooLarge:
                                                    Logger.Log(Severity.INFO, "Skip " + docToParse.FileName + " - Too large");
                                                    break;
                                                case IDocument.SkipReason.ParseError:
                                                    Logger.Log(Severity.WARNING, "Skip - " + docToParse.FileName + " - Parse Error \n " + docToParse.Text);
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }));
                    }
                    #endregion
                    #region An additional worker in charge of updating progress display
                    workers.Add(Task.Run( async () =>
                    {
                        TimeSpan updateFrequency = TimeSpan.FromMilliseconds(250);
                        while (!docProcessingQueue.IsCompleted)
                        {
                            int totalDiscoveredDocs = Source.GetTotalDiscoveredDocuments();

                            _maxProgress = totalDiscoveredDocs;
                            _progress = RetrievedDocuments;
                            string strTimeRemaining = "";
                            try
                            {
                                strTimeRemaining = ProgressCalculator.GetHumanFriendlyTimeRemainingLocalizablePrecise(startTime, Source.GetProgress());
                            } catch (Exception ex)
                            {
#if DEBUG
                                Debug.WriteLine($"Exception calculating time remaining: {ex.Message}");
#endif
                                // Non fatal.
                            }

                            readyDocQueue.TryPeek(out var document);

                            if (document?.FileName != null)
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
                            await Task.Delay(updateFrequency);
                        }
                    }));
                    // An additional one to do debug logging on queue contention
#if DEBUG
                    workers.Add(Task.Run(async () =>
                    {
                        var sw = Stopwatch.StartNew();
                        var last = new
                        {
                            Time = DateTime.UtcNow,
                            Produced = _documentsProduced,
                            Preloaded = _documentsPreloaded,
                            Flushed = _documentsFlushed
                        };

                        while (!docProcessingQueue.IsCompleted)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(8));
                            var now = DateTime.UtcNow;
                            double elapsedSec = (now - last.Time).TotalSeconds;

                            long prodDelta = _documentsProduced - last.Produced;
                            long preloadDelta = _documentsPreloaded - last.Preloaded;
                            long flushDelta = _documentsFlushed - last.Flushed;

                            double prodRate = prodDelta / elapsedSec;
                            double preloadRate = preloadDelta / elapsedSec;
                            double flushRate = flushDelta / elapsedSec;

                            Debug.WriteLine(
                                $"[Pipeline Metrics] " +
                                $"\nInQ={docProcessingQueue.Count,4}/1000  " +
                                $"\nReadyQ={readyDocQueue.Count,6}  " +
                                $"\nBatchMB={_currentBatchSize / 1048576,4}  " +
                                $"\nRates → Produce:{prodRate,6:F1}/s  Preload:{preloadRate,6:F1}/s  Flush:{flushRate,6:F1}/s");

                            last = new { Time = now, Produced = _documentsProduced, Preloaded = _documentsPreloaded, Flushed = _documentsFlushed };
                        }
                    }));
#endif
                    #endregion

                    IDocument? document = null;
                    bool isDiscoveryComplete;
                    long max_batch_size_bytes = ( (long)(MemoryUtils.GetRecommendedRAMBufferSizeMB() * 1048576) / 2 ); // * 1048576 converts mb to bytes

                    

                    do
                    {
                        mrse.WaitOne();
                        if (Cancelling)
                        {
                            Logger.Log(Severity.INFO, "Index task was cancelled");
                            Cancelled = true;
                            docProcessingQueue.CompleteAdding();  // Signal workers to stop
                            return;
                        }


                        if (_batchWatch.ElapsedMilliseconds > 3000 || readyDocQueue.Count > 16384 || _currentBatchSize > max_batch_size_bytes)
                        {
                            FlushBatch(readyDocQueue);
                        }
                        Source.GetNextDoc(out document, out isDiscoveryComplete);

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
                           
                            string displayName = string.Empty;
                            if (!string.IsNullOrWhiteSpace(document.FileName))
                            {
                                // For all FileSystemDocuments, use the filename. This avoids calling the extraction mehtod.
                                displayName = Path.GetFileNameWithoutExtension(document.FileName);
                            } else
                            {
                                displayName = document.DisplayName ?? "Unnamed"; // On FileSystemDocument this would call ExtractDataFromDocument which is undesired.
                            }
                            if (removeNotFound)
                            {
                                FoundFileNames.Add(document.FileName ?? displayName);
                            }

                            docProcessingQueue.Add(document);
                            Interlocked.Increment(ref _documentsProduced);
                        }
                        #endregion
                        if (document == null && !isDiscoveryComplete)
                        {
                            Thread.Sleep(50);
                        }
                    } while (document != null || !isDiscoveryComplete);

                    docProcessingQueue.CompleteAdding(); // Done producing documents.
                    //Wait for any last documents to be preloaded
                    await Task.WhenAll(workers);
                    // Flush any remaining ready docs
                    FlushBatch(readyDocQueue);

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
                    }
                    catch (Exception ex)
                    {
                        // Treat this as non-fatal.
                        Logger.Log(Severity.WARNING, "Error getting total number of documents in index", ex);
                    }

                    Severity finalSeverity = Severity.INFO;

                    if (Logger is ILogger2 logger2)
                    {
                        if (logger2.GetNumErrors() > 0)
                        {
                            finishStatus.AppendLine(S.Get("Error(s) during indexing. Check log for details."));
                            finalSeverity = Severity.ERROR;
                        }
                    }

                    _progressStatus = finishStatus.ToString();
                    _progress = 100;
                    _maxProgress = 100;
                    progressView.IsFinished = true;

                    

                    Logger.Log(finalSeverity, finishStatus.ToString());
                }

            }
            catch (FailedToOpenIndexException f)
            {
                Logger.Log(Severity.ERROR, "Failed to open Index", f.InnerException);
                if (throwOnFailedToOpen) throw;
            }
            catch (Exception ex)
            {
                Logger.Log(Severity.ERROR, "Fatal error during indexing", ex);


            }
            finally
            {
                Index.CloseWrite();
                progressView.EndWatching();

                if (Logger is IndexTaskLog logger)
                {
                    string indexLog = logger.BuildTxtLog("", "");
                    File.WriteAllText(
                        Path.Combine(Index.GetAbsolutePath(), "IndexTask.txt"), indexLog);
                }

            }
        }

        private void FlushBatch(ConcurrentQueue<IDocument> readyDocQueue)
        {
            try
            {
                List<IDocument> batch = new List<IDocument>();
                int dequeuedCount = 0;
                while (dequeuedCount < 16384 && readyDocQueue.TryDequeue(out var dequeuedDoc))
                {
                    batch.Add(dequeuedDoc);
                    dequeuedCount++;
                }

                if (batch.Count > 0)
                {
                    Index.AddDocuments(batch, out var failures);
                    foreach (var failure in failures)
                    {
                        Logger.Log(Severity.ERROR, $"Skipped {failure.Key.FileName} Due to a Parse Error", failure.Value);
                    }
                    Interlocked.Add(ref IndexedDocuments, batch.Count);  // Thread-safe
                    Interlocked.Add(ref _documentsFlushed, batch.Count);
                    foreach (var indexedDocument in batch)
                    {
                        Interlocked.Add(ref _currentBatchSize, -indexedDocument.FileSize); // This is actually subtract
                    }
                    _batchWatch.Restart();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Severity.ERROR, "Exception whilst adding documents to index " + ex.ToString());
            }
        }
    }
}
