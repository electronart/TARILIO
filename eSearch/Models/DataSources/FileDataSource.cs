using eSearch.Interop;
using eSearch.Models.Configuration;
using eSearch.Models.Documents;
using eSearch.Models.Indexing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.DataSources
{
    public class FileDataSource : IDataSource, ISupportsIndexConfigurationDataSource
    {

        public string FilePath = string.Empty;

        /// <summary>
        /// Got the main doccument?
        /// </summary>
        private bool _got = false;

        private int _extractedDocumentIndex = 0;
        private List<string> _extractedDocuments = new List<string>();

        IIndexConfiguration? indexConfig = null;

        ILogger? _logger = null;

        int _subDocumentIndex = 0;

        int _knownTotalSubDocs = 0;

        int _totalSubDocumentsFoundSoFar = 0;

        FileSystemDocument MainDocument
        {
            get
            {
                if (_mainDocument == null)
                {
                    _mainDocument = new FileSystemDocument();
                    _mainDocument.SetDocument(FilePath);
                }
                return _mainDocument;
            }
        }

        FileSystemDocument _mainDocument = null;

        private IEnumerable<IDocument> _SubDocuments = null;

        private IEnumerator<IDocument> _SubDocumentsRecursiveEnumerator = null;

        public string Description()
        {
            return "File: " + FilePath;
        }

        public void GetNextDoc(out IDocument document, out bool isDiscoveryComplete)
        {
            isDiscoveryComplete = _got == true;
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
                        _extractedDocuments.AddRange(document.ExtractedFiles);
                    }
                    return;
                }

            }
            // If we got this far, all current sub documents are covered.
            _SubDocuments = null;
            _SubDocumentsRecursiveEnumerator = null;
            #endregion



            if (_got == true)
            {
                if (_extractedDocumentIndex < _extractedDocuments.Count)
                {
                    // Handle extracted files.
                    FileSystemDocument _document = new FileSystemDocument();
                    _document.SetDocument(_extractedDocuments[_extractedDocumentIndex]);
                    document = _document;
                    _SubDocuments = _document.SubDocuments;
                    _knownTotalSubDocs += _document.TotalKnownSubDocuments;
                    isDiscoveryComplete = true;
                    ++_extractedDocumentIndex;
                    return;
                } else
                {
                    // No more documents.
                    document = null;
                    return;
                }
            }
            else
            {
                
                if (MainDocument.ExtractedFiles != null && MainDocument.ExtractedFiles.Count() > 0)
                {
                    _extractedDocuments.AddRange(MainDocument.ExtractedFiles);
                }
                document = MainDocument;
                _SubDocuments = MainDocument.SubDocuments;
                _knownTotalSubDocs += _mainDocument.TotalKnownSubDocuments;
                isDiscoveryComplete = true;
                _got = true;
                return;
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
                        _knownTotalSubDocs += current.TotalKnownSubDocuments;
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

        public double GetProgress()
        {
            if (_got == false) return 0;
            return 100;
        }

        public int GetTotalDiscoveredDocuments()
        {
            return 1 + _extractedDocuments.Count + Math.Max(_knownTotalSubDocs, _totalSubDocumentsFoundSoFar);
        }

        public void Rewind()
        {
            _got = false;
            _extractedDocumentIndex = 0;
            _extractedDocuments.Clear();
            _subDocumentIndex = 0;
            _mainDocument = null;
            _SubDocuments = null;
            _SubDocumentsRecursiveEnumerator = null;
            _knownTotalSubDocs = 0;
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
            indexConfig = config;
        }

        public void UseIndexTaskLog(ILogger logger)
        {
            _logger = logger;
        }
    }
}
