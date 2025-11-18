using eSearch.Models.Documents;
using eSearch.Models.Indexing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using Directory = eSearch.Models.Indexing.Directory;
using eSearch.Models.Configuration;
using eSearch.Interop;
using static eSearch.Interop.ILogger;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace eSearch.Models.DataSources
{
    public class DirectoryDataSource : IDataSource,
        ISupportsIndexConfigurationDataSource
    {
        

        public Directory[]              Directories = [];
        private ConcurrentQueue<(string Path, bool Recursive)> _directoriesToProcess = new();
        private List<Task>              _discoveryTasks = new List<Task>();
        private int                     _activeDiscoveryWorkers = 0;

        private ConcurrentQueue<string> _discoveredFilePaths = new ConcurrentQueue<string>();

        public DirectoryDataSource(Directory[] startDirectories) {
            this.Directories = startDirectories;
        }

        #region File/Folder Discovery Work Tracking
        private int                 _totalDiscoveredFiles;

        /// <summary>
        /// How many sub documents are currently expected. This value can change during the indexing process and may be LOWER than the total number of sub documents.
        /// </summary>
        private int                 _totalExpectedSubDocuments = 0;

        private int                 _totalSubDocumentsFoundSoFar = 0;

        private bool                _discoveryFinished = false;
        private string              _discoveryStatus = string.Empty;
        private List<string>        _discoveryErrors = new List<string>();
        #endregion

        #region File Iteration Progress Tracking
        private int _iterationTotalFilesIterated = 0;
        private string _iterationStatus = string.Empty;
        #endregion

        private CancellationTokenSource _discoveryCts = new CancellationTokenSource();


        private IEnumerable<IDocument>? _SubDocuments   = null;

        private IEnumerator<IDocument>? _SubDocumentsRecursiveEnumerator = null;

        private IIndexConfiguration? _indexConfiguration = null;

        private ILogger? _logger = null;

#if DEBUG
        private List<string> _DEBUG_PATHS = new List<string>();
#endif

        public int GetTotalDiscoveredDocuments()
        {
            return _totalDiscoveredFiles + Math.Max(_totalExpectedSubDocuments, _totalSubDocumentsFoundSoFar);
        }

        private Stopwatch _stopWatch = new Stopwatch();

        public void GetNextDoc(out IDocument? document, out bool isDiscoveryComplete)
        {

            try
            {
                _stopWatch.Restart();
                isDiscoveryComplete = (_discoveryFinished == true); // Assign this first to prevent race conditions.
                #region Handle Sub Documents Recursively using an enumerator if sub documents are available.
                if (_SubDocuments != null && _SubDocumentsRecursiveEnumerator == null)
                {
                    _SubDocumentsRecursiveEnumerator = SubDocRecursiveEnumerator(_SubDocuments);
                }

                if (_SubDocumentsRecursiveEnumerator != null)
                {
                    if (_SubDocumentsRecursiveEnumerator.MoveNext())
                    {
                        document = _SubDocumentsRecursiveEnumerator.Current;
                        if (document.ExtractedFiles != null)
                        {
                            foreach(var extracted in document.ExtractedFiles)
                            {
                                _discoveredFilePaths.Enqueue(extracted);
                            }
                        }
                        _stopWatch.Stop();
                        return;
                    }

                }
                _stopWatch.Restart();
                // If we got this far, all current sub documents are covered.
                _SubDocuments = null;
                _SubDocumentsRecursiveEnumerator = null;
                #endregion
                #region init discovery/initial directory/current file list in dir if not yet initialized
                if (_discoveryTasks.Count == 0)
                {
                    StartParallelDiscovery();
                    document = null;
                    return;
                }
                #endregion

                if (_discoveredFilePaths.TryDequeue(out var filePath))
                {
                    FileSystemDocument _document = new FileSystemDocument();
                    _document.SetDocument(filePath);
                    document = _document;
                    if (document.SubDocuments != null)
                    {
                        _SubDocuments = document.SubDocuments;
                        _totalExpectedSubDocuments += document.TotalKnownSubDocuments;
                    }
                    if (document.ExtractedFiles != null)
                    {
                        foreach (var extracted in document.ExtractedFiles)
                        {
                            _discoveredFilePaths.Enqueue(extracted);  // Add to queue
                        }
                    }
                    ++_iterationTotalFilesIterated;
                    return;
                } else
                {
                    // Empty queue - Wait if discovery ongoing
                    document = null;
                    return;
                }
                
            } catch (Exception ex)
            {
                Debug.WriteLine("A fatal exception occurred whilst indexing?");
                Debug.WriteLine(ex);
                document = null;
                isDiscoveryComplete = true;

            }
        }

        public IEnumerator<IDocument> SubDocRecursiveEnumerator(IEnumerable<IDocument> topEnumerable)
        {
            if (topEnumerable == null) yield break;

            var enumeratorStack = new Stack<IEnumerator<IDocument>>();
            var currentEnumerator = topEnumerable.GetEnumerator();
            enumeratorStack.Push(currentEnumerator);

            while (enumeratorStack.Count > 0)
            {
                currentEnumerator = enumeratorStack.Peek();
                if (currentEnumerator.MoveNext())
                {
                    var current = currentEnumerator.Current;
                    ++_totalSubDocumentsFoundSoFar;
                    yield return current;

                    if (current.SubDocuments != null)
                    {
                        _totalExpectedSubDocuments += current.TotalKnownSubDocuments;
                        var subEnumerator = current.SubDocuments.GetEnumerator();
                        enumeratorStack.Push(subEnumerator);
                    }
                }
                else
                {
                    enumeratorStack.Pop();
                    currentEnumerator.Dispose();
                }
            }
        }

        private void StartParallelDiscovery()
        {
            foreach(var directory in Directories)
            {
                _directoriesToProcess.Enqueue((directory.Path,directory.Recursive));   
            }

            int workerCount = Math.Max(1, Math.Min(16, Environment.ProcessorCount));
            _activeDiscoveryWorkers = workerCount;
            for (int i = 0; i < workerCount; i++)
            {
                _discoveryTasks.Add(Task.Run(() => DiscoveryWorker(_discoveryCts.Token)));
            }
        }

        private void DiscoveryWorker(CancellationToken token)
        {
            while (
                !token.IsCancellationRequested &&
                _directoriesToProcess.TryDequeue(out var currentDir))
            {
                bool recursive = currentDir.Recursive;
                if (token.IsCancellationRequested) break;
                try
                {

                    var enumerationOptions = new EnumerationOptions
                    {
                        RecurseSubdirectories = false,  // We handle recursion manually
                        IgnoreInaccessible = true
                    };


                    DirectoryInfo dirInfo = new DirectoryInfo(currentDir.Path);

                    try
                    {
                        // Discover files
                        foreach (var file in dirInfo.EnumerateFiles("*", enumerationOptions))
                        {  // Use Enumerate for laziness
                            bool skip = false;
                            #region Check for reasons to skip this file
                            if (_indexConfiguration != null)
                            {
                                #region File Extension?
                                if (_indexConfiguration.SelectedFileExtensions != null)
                                {
                                    string extension = Path.GetExtension(file.FullName);
                                    if (extension.Length > 1) extension = extension.Substring(1).ToLower();
                                    if (!_indexConfiguration.SelectedFileExtensions.Contains(extension))
                                    {
                                        skip = true;
                                    }
                                }
                                #endregion
                                #region File Size?
                                if (_indexConfiguration.MaximumIndexedFileSizeMB > 0)
                                {
                                    double fileSizeMB = (file.Length / 1024f) / 1024f;
                                    if (fileSizeMB > _indexConfiguration.MaximumIndexedFileSizeMB)
                                    {
                                        skip = true;
                                    }
                                }
                                #endregion
                            }
                            #region Check the file is not hidden or system file
                            if (file.Attributes.HasFlag(FileAttributes.Hidden) || Path.GetFileName(file.Name).StartsWith("."))
                            {
                                skip = true;
                            }
                            if (file.Attributes.HasFlag(FileAttributes.System))
                            {
                                skip = true;
                            }
                            #endregion
                            #endregion
                            if (!skip)
                            {
                                Interlocked.Increment(ref _totalDiscoveredFiles);
                                _discoveredFilePaths.Enqueue(file.FullName);  // Enqueue path directly
                            }
                        }
                    }
                    catch (SecurityException sex)
                    {
                        _logger?.Log(Severity.WARNING, "Access Denied (1): " + currentDir, sex);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        _logger?.Log(Severity.WARNING, "No Such Directory: " + currentDir);
                    }

                    // Discover subdirs if recursive
                    if (recursive)
                    {
                        try
                        {
                            foreach (var subDir in dirInfo.EnumerateDirectories("*", enumerationOptions))
                            {
                                if (subDir.Attributes.HasFlag(FileAttributes.Hidden) || subDir.Name.StartsWith(".") || subDir.Attributes.HasFlag(FileAttributes.System)) continue;
                                _directoriesToProcess.Enqueue((subDir.FullName, recursive));
                            }
                        }
                        catch (Exception ex)
                        {  // Consolidated catch
                            _logger?.Log(Severity.WARNING, "Access Denied or Error: " + currentDir, ex);
                        }
                    }
                } catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    _logger?.Log(Severity.WARNING, "Access denied or error scanning: " + currentDir);
                }
            }

            if (Interlocked.Decrement(ref _activeDiscoveryWorkers) == 0)
            {
                _discoveryFinished = true;
            }
        }

        public double GetProgress()
        {
            if (_totalDiscoveredFiles == 0) return 0;
            if (_iterationTotalFilesIterated == 0) return 0;
            int subDocCount = Math.Max(_totalSubDocumentsFoundSoFar, _totalExpectedSubDocuments); // Whichever is greater we'll assume to be true.
            float val = (float.Parse(_iterationTotalFilesIterated.ToString()) / float.Parse((_totalDiscoveredFiles + subDocCount).ToString())) * 100.0f;
            int progress = (int)Math.Round(val);
            if (progress < 0) progress = 0;
            if (progress > 100) progress = 100;
            return progress;
        }

        public bool IsDiscoveryComplete()
        {
            return _discoveryFinished;
        }

        public void Rewind()
        {
            _iterationTotalFilesIterated = 0;
            _iterationStatus = string.Empty;

            _discoveryCts.Cancel();
            Task.WhenAll(_discoveryTasks).Wait();
            _discoveryCts = new CancellationTokenSource();
            _discoveryTasks.Clear();
            _directoriesToProcess = new ConcurrentQueue<(string Path, bool Recursive)>();
            _activeDiscoveryWorkers = 0;
            _discoveryFinished = false;


            _totalDiscoveredFiles = 0;
            _discoveryFinished = false;
            _discoveryStatus = string.Empty;
            _discoveryErrors = new List<string>();
            _SubDocuments = null;

            _totalExpectedSubDocuments = 0;
            _totalSubDocumentsFoundSoFar = 0;
            _discoveredFilePaths = new ConcurrentQueue<string>();  // Reset queue
        }

        public string Description()
        {
            if (Directories.Length > 0)
            {
                // TODO We changed the design such that the UI can only create a single directory in a datasource and instead use MultipleSourceDataSource for multiple directories. (Ie. a filesystem datasource per directory)
                return "Folder: " + Directories[0].Path;
            }
            // Unexpected;
            return "Error - No Folder Selected...";
        }

        public override string ToString()
        {
            return this.ToString(null, null);
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return Description();
        }

        public void UseIndexConfig(IIndexConfiguration config)
        {
            _indexConfiguration = config;
        }

        public void UseIndexTaskLog(ILogger logger)
        {
            _logger = logger;
        }
    }
}
