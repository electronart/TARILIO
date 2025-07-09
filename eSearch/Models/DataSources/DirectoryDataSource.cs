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

namespace eSearch.Models.DataSources
{
    public class DirectoryDataSource : IDataSource, ISupportsIndexConfigurationDataSource
    {
        

        public Directory[]          Directories;

        public DirectoryDataSource(Directory[] directories) {
            this.Directories = directories;
        }

        #region File/Folder Discovery Work Tracking
        private BackgroundWorker?   _discoveryWorker;
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
        private int _iterationFileIndexInDir = 0;
        /// <summary>
        /// Each int in the list represents index of folder in level. If there's only one int, it's the top level, two ints, a second level etc.
        /// </summary>
        private List<int> _iterationDirectoryIndex = new List<int> { 0 };
        private string _iterationStatus = string.Empty;
        #endregion

        private Directory       _currentDir     = null;
        private List<string>    _currentFileList      = null;


        private IEnumerable<IDocument> _SubDocuments   = null;

        private IEnumerator<IDocument> _SubDocumentsRecursiveEnumerator = null;

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
                            _currentFileList.AddRange(document.ExtractedFiles);
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
                
                if (_discoveryWorker == null)
                {
                    Debug.WriteLine("Starting Worker..");
                    _discoveryWorker = new BackgroundWorker();
                    _discoveryWorker.DoWork += _discoveryWorker_DoWork;
                    _discoveryWorker.RunWorkerCompleted += _discoveryWorker_RunWorkerCompleted;
                    _discoveryWorker.WorkerSupportsCancellation = true;
                    _discoveryWorker.RunWorkerAsync();
                    document = null;
                    return;
                }
                if (_currentDir == null)
                {
                    // First run.
                    if (Directories.Length == 0)
                    {
                        document = null;
                        return;
                    }
                    _currentDir = Directories[0];
                }
                if (_currentFileList == null)
                {
                    // Will return null OR the full list of discovered files.
                    _currentFileList = new List<string>();
                    var discovered_files = _currentDir.GetDiscoveredFiles();
                    if (discovered_files == null)
                    {
                        document = null;
                        return; // Not yet discovered this directory.
                    } else
                    {
                        _currentFileList.AddRange(discovered_files);
                    }
                }

                
                #endregion
                _stopWatch.Restart();
                
                FileSystemDocument _document = new FileSystemDocument();



                if (_currentFileList != null)
                {
                    if (_iterationFileIndexInDir < _currentFileList.Count)
                    {
                        string filePath = Path.Combine(_currentDir.Path, _currentFileList[_iterationFileIndexInDir]);
                        _document.SetDocument(filePath);
                        document = _document;
                        if (document.SubDocuments != null)
                        {
                            _SubDocuments = document.SubDocuments;
                            _totalExpectedSubDocuments += document.TotalKnownSubDocuments;
                        }
                        if (document.ExtractedFiles != null)
                        {
                            _currentFileList.AddRange(document.ExtractedFiles);
                        }
                        ++_iterationFileIndexInDir;
                        ++_iterationTotalFilesIterated;
                        return;
                    }
                    else
                    {
                        // No more documents in this directory. 
                        // Only proceed to next Directory if SubDirs has loaded.



                        
                        var subDirs = _currentDir.GetDiscoveredSubDirectories();
                        if (subDirs == null)
                        {
                            // SubDirs not yet loaded. Await discovery thread.
                            document = null;
                            return;
                        }
                        else
                        {
                            if (_goToNextDirectory())
                            {
                                _iterationFileIndexInDir = 0;
#if DEBUG
                                if (_currentDir.Path.Contains("Views"))
                                {
                                    Debug.WriteLine("Views");
                                }
                                _DEBUG_PATHS.Add(_currentDir.Path);
#endif
                                _currentFileList = null;
                                GetNextDoc(out document, out bool complete);
                                isDiscoveryComplete = complete;
#if DEBUG
                                if (document == null && complete)
                                {
                                    Debug.WriteLine("-- INDEXED PATHS --");
                                    foreach(var path in _DEBUG_PATHS)
                                    {
                                        Debug.WriteLine(" - " + path);
                                    }
                                    
                                    Debug.WriteLine("Complete");
                                }
#endif


                                return;
                            }
                            else
                            {
                                // No more directories.
                                document = null;
                                return;
                            }

                        }
                    }
                }
                else
                {
                    // The file list has not yet loaded. Await discovery thread.
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
            if (topEnumerable != null) {
                var enumerator = topEnumerable.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    ++_totalSubDocumentsFoundSoFar;
                    yield return current;
                    if (current.SubDocuments != null)
                    {
                        _totalExpectedSubDocuments += current.TotalKnownSubDocuments;
                        var subEnumerator = SubDocRecursiveEnumerator(current.SubDocuments);
                        while (subEnumerator.MoveNext())
                        {
                            ++_totalSubDocumentsFoundSoFar;
                            yield return subEnumerator.Current;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Goes to the next directory. This may be a sub directory, a directory on the same level, or a directory on a level above depending on conditions.
        /// WARNING - Only call method if SubDirs in current directory has loaded.
        /// </summary>
        private bool _goToNextDirectory()
        {
            
            #region Calculate current directory
            Directory dir = _calculateCurrentDirectory();
            #endregion
            #region If there are subdirectories of current subdirectory, go down a level
            var currentSubDirs = dir.GetDiscoveredSubDirectories();
            if (currentSubDirs == null) throw new Exception("Unexpected - Current SubDirs null"); // Should not happen due to the order we iterate directories.
            if (currentSubDirs.Length > 0)
            {
                _iterationDirectoryIndex.Add(0);
                _currentDir = currentSubDirs[0];
                _iterationFileIndexInDir = 0;
                return true;
            }
            #endregion
            #region Else, stay at current level then progressively go upwards until there are no more directories to index.
            int level = _iterationDirectoryIndex.Count - 1;
            while (level > -1)
            {
                _iterationDirectoryIndex[level] = _iterationDirectoryIndex[level] + 1;
                if (_directoryExistsAtCurrentPosition())
                {
                    _currentDir = _calculateCurrentDirectory();
                    _iterationFileIndexInDir = 0;
                    return true;
                }
                // No further directories at this level. Go up a level.
                _iterationDirectoryIndex.RemoveAt(level);
                --level;
            }
            #endregion
            // No further directories at all levels.
            return false;

        }

        private bool _directoryExistsAtCurrentPosition()
        {
            int dirIndex = _iterationDirectoryIndex[0];
            if (dirIndex >= Directories.Length) return false;
            Directory dir = Directories[dirIndex]; // Current Directory at Level 0
            int level = 1;
            while (level < _iterationDirectoryIndex.Count)
            {
                int indexAtLevel = _iterationDirectoryIndex[level];
                var subdirs = dir.GetDiscoveredSubDirectories();
                if (subdirs == null) return false;
                if (subdirs.Length > indexAtLevel)
                {
                    dir = subdirs[indexAtLevel];
                } else
                {
                    return false;
                }
                ++level;
            }
            return true;
        }

        private Directory _calculateCurrentDirectory()
        {
            Directory dir = Directories[_iterationDirectoryIndex[0]]; // Current Directory at Level 0
            int i = 1;
            while (i < _iterationDirectoryIndex.Count)
            {
                int indexAtLevel = _iterationDirectoryIndex[i];
                var subdirs = dir.GetDiscoveredSubDirectories();
                if (subdirs == null) throw new Exception("Unexpected - SubDirs not loaded");
                dir = subdirs[indexAtLevel];
                ++i;
            }
            return dir;
        }

        private void _discoveryWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            _discoveryFinished = true;
        }

        private void _discoveryWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            try
            {
                foreach (var directory in Directories)
                {
                    _discoverDirectory(directory);
                }
            } catch (Exception ex)
            {
                _logger?.Log(Severity.ERROR, "Unhandled error discovering directories", ex);
            }
        }

        /// <summary>
        /// Note this method is recursive if the given directory has the Recursive property set true.
        /// </summary>
        /// <param name="directory">The directory to discover.</param>
        private void _discoverDirectory(Directory directory)
        {
            try
            {

                var enumerationOptions = new EnumerationOptions
                {
                    RecurseSubdirectories = false,
                    IgnoreInaccessible = true
                };

                List<string> temp = new List<string>();
                FileInfo[] fileInfo = null;
                temp.Clear();
                #region 1. Discover Files in this Directory.
                _discoveryStatus = "Discovering " + directory.Path;

                DirectoryInfo drInfo = new DirectoryInfo(directory.Path);
                try
                {
                    
                    fileInfo = drInfo.GetFiles("*", enumerationOptions );
                    foreach (var file in fileInfo)
                    {
                        bool skip = false;
                        #region Check for reasons to skip this file
                        if (_indexConfiguration != null)
                        {
                            #region File Extension?
                            if (_indexConfiguration.SelectedFileExtensions != null)
                            {
                                string extension = Path.GetExtension(file.FullName);
                                if (extension.Length > 1) extension = extension.Substring(1).ToLower(); // Length check is necessary due to files with no extension...

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
                        #region Check the file is not hidden or system file.
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
                        if (!skip) {
                            ++_totalDiscoveredFiles;
                            temp.Add(file.Name);
                        }
                        
                    }
                    directory.SetDiscoveredFiles(temp.ToArray());
                    temp.Clear();
                }
                catch (SecurityException sex) { _logger?.Log(Severity.WARNING, "Access Denied (1): " + directory.Path, sex); }
                catch (DirectoryNotFoundException) { _logger?.Log(Severity.WARNING, "No Such Directory: " + directory.Path); }
                #endregion
                temp.Clear();
                #region 2. If the Directory is set Recursive, Discover each subdirectory.
                if (directory.Recursive)
                {
                    try
                    {
                        var subDirs = drInfo.GetDirectories("*", enumerationOptions);
                        List<Directory> temp2 = new List<Directory>();
                        foreach (var subDir in subDirs)
                        {
                            if (subDir.Attributes.HasFlag(FileAttributes.Hidden)) continue; // Skip hidden directories.
                            if (subDir.Name.StartsWith(".")) continue; // Skip hidden directories (linux)
                            if (subDir.Attributes.HasFlag(FileAttributes.System)) continue; // Skip system directories.
                            temp2.Add(new Directory(subDir.FullName, true));
                        }
                        directory.SetDiscoveredSubDirectories(temp2.ToArray());
                        foreach (var dir in temp2)
                        {
                            _discoverDirectory(dir);
                        }
                    }
                    catch (SecurityException sex)
                    {
                        directory.SetDiscoveredSubDirectories(new Directory[0]);
                        _logger.Log(Severity.WARNING, "Access Denied (2): " + directory.Path, sex);
                    }
                    catch (UnauthorizedAccessException uaex)
                    {
                        directory.SetDiscoveredSubDirectories(new Directory[0]);
                        _logger.Log(Severity.WARNING, "Access Denied (3): " + directory.Path, uaex); }
                    catch (DirectoryNotFoundException) {
                        directory.SetDiscoveredSubDirectories(new Directory[0]);
                        _logger.Log(Severity.WARNING, "No Such Directory: " + directory.Path); 
                    }
                }
                #endregion
            } catch (Exception e)
            {
                _logger?.Log(Severity.ERROR, "Unhandled Error whilst discovering directory " + directory.Path, e);
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
            _currentDir = null;
            _currentFileList = null;
            _iterationTotalFilesIterated = 0;
            _iterationFileIndexInDir = 0;
            _iterationDirectoryIndex = new List<int> { 0 };
            _iterationStatus = string.Empty;

            _discoveryWorker = null;
            _totalDiscoveredFiles = 0;
            _discoveryFinished = false;
            _discoveryStatus = string.Empty;
            _discoveryErrors = new List<string>();
            _SubDocuments = null;

            _totalExpectedSubDocuments = 0;
            _totalSubDocumentsFoundSoFar = 0;
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
