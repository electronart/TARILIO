using eSearch.Models.Documents.Parse;
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
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Models.Search
{
    public class CopyResultsTask : IProgressQueryableTask, ICancellableTask
    {

        private IEnumerable<IResult> results;
        private string outputFolder;

        long? _totalSizeOfDocumentsToCopy   = null;
        long _totalSizeOfDocumentsCopied    = 0l;

        int? _maxProgress       = null;               // Starts as null, but will be calculated when GetProgress is called the first time.
        int _currentProgress    = 0;

        private string status = string.Empty;

        ManualResetEvent mrse = new ManualResetEvent(false);
        private bool cancelled = false;

        /// <summary>
        /// Construct a task to make a copy of all results 
        /// </summary>
        /// <param name="results"></param>
        /// <param name="outputFolder"></param>
        public CopyResultsTask(IEnumerable<IResult> results, string outputFolder)
        {
            this.results        = results;
            this.outputFolder   = outputFolder;
        }

        public void Execute()
        {
            mrse.Set();
            long fileSize;
            foreach(var result in results)
            {

                mrse.WaitOne(); // This is the point where the thread will pause if Pause() has been called.
                if (cancelled)
                {
                    return;
                }

                try
                {
                    status = result.Document.FileName ?? result.Title;
                    if (result.Document.IsVirtualDocument || !System.IO.File.Exists(result.Document.FileName))
                    {
                        // File is not a file system file. Just write the html render.
                        string fileName = Path.GetFileName(result.Document.FileName) + ".missing.txt";
                        File.WriteAllText(Path.Combine(outputFolder, fileName), result.Document.Text);
                    } else
                    {
                        string targetFile = Path.Combine(outputFolder, Path.GetFileName(result.Document.FileName));
                        File.Copy(result.Document.FileName, targetFile, true);
                        if (Path.GetExtension(result.Document.FileName).ToLower() == ".eml")
                        {
                            EmlParser parser = new EmlParser();
                            parser.Parse(result.Document.FileName, out var parseResult);
                            if (parseResult.ExtractedFiles != null)
                            {
                                string attachmentsDirectory = Path.Combine(outputFolder, "Attachments");
                                Directory.CreateDirectory(attachmentsDirectory);
                                foreach (var extractedFile in parseResult.ExtractedFiles)
                                {
                                    File.Copy(extractedFile, Path.Combine( attachmentsDirectory, Path.GetFileName(extractedFile)) , true);
                                }
                            }
                            
                        }
                    }
                }
                finally
                {
                    
                    if (long.TryParse(result.GetFieldValue("_FileSize", "1024"), out fileSize))
                    {
                        _totalSizeOfDocumentsCopied += fileSize;
                    }
                    else
                    {
                        _totalSizeOfDocumentsCopied += 1024L;
                    }
                }
                
            }
        }

        

        public int GetMaxProgress()
        {
            return 100000; // 100,000. Just to give the progress bar better precision.
        }



        public int GetProgress()
        {
            if (_totalSizeOfDocumentsToCopy == null)
            {
                // Initialize this value.
                _totalSizeOfDocumentsToCopy = 0L;
                long fileSize = 0L;
                foreach(var result in results)
                {
                    if (long.TryParse(result.GetFieldValue("_FileSize", "1024"), out fileSize))
                    {
                        _totalSizeOfDocumentsToCopy += fileSize;
                    } else
                    {
                        _totalSizeOfDocumentsToCopy += 1024L;
                    }
                }
            }

            

            if (_totalSizeOfDocumentsToCopy <= 0)
            {
                return 0;
            }
            if (_totalSizeOfDocumentsCopied <= 0)
            {
                return 0;
            }
            if (_totalSizeOfDocumentsCopied >= _totalSizeOfDocumentsToCopy)
            {
                return GetMaxProgress();
            }
            double progress = ProgressCalculator.GetXAsPercentOfYPrecise(_totalSizeOfDocumentsCopied, (double)_totalSizeOfDocumentsToCopy);
            int scaledProgress = (int)(progress * GetMaxProgress());
            return Math.Clamp(scaledProgress, 0, GetMaxProgress());
        }

        public string GetStatusString()
        {
            return status;
        }

        public void GetCancelConfirmationPrompt(out string title, out string message)
        {
            title =   S.Get("Cancel Copying?");
            message = S.Get("Folder will not contain all files");
        }

        public bool HasReceivedCancelRequest()
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void RequestCancel()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }
    }
}
