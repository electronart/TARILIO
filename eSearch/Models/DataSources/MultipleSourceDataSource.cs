using eSearch.Interop;
using eSearch.Models.Configuration;
using eSearch.Models.Documents;
using eSearch.Models.Indexing;
using ProgressCalculation;
using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace eSearch.Models.DataSources
{
    public class MultipleSourceDataSource : IDataSource, ISupportsIndexConfigurationDataSource
    {

        public List<IDataSource> Sources { get; set; }

        private IDataSource _currentDataSource = null;

        private ILogger? _logger;

        int _sourceIndex = 0;

        int retrievedDocCount = 0;

        Stopwatch _stopWatch = new Stopwatch();

        public MultipleSourceDataSource(List<IDataSource> sources)
        {
            this.Sources = sources;
            if (Sources.Count > 0)
            {
                _currentDataSource = Sources[0];
            }
        }

        public void GetNextDoc(out IDocument document, out bool isDiscoveryComplete)
        {
            _stopWatch.Restart();
            _currentDataSource.GetNextDoc(out document, out bool srcComplete);
            _stopWatch.Stop();
            _stopWatch.Restart();
            if (document != null)
            {
                ++retrievedDocCount;
                isDiscoveryComplete = false;
                return; // Got a document.

            }
            else
            {
                if (!srcComplete)
                {
                    // This source hasn't finished discovering yet.
                    isDiscoveryComplete = false;
                    document = null;
                    return;
                }
                else
                {
                    // This source has finished discovering. Go to the next one if there are any.
                    if (_sourceIndex < (Sources.Count - 1))
                    {
                        // Another source is in the list, go to the next source.
                        ++_sourceIndex;
                        _currentDataSource = Sources[_sourceIndex];
                        GetNextDoc(out document, out isDiscoveryComplete);
                        if (document != null) retrievedDocCount++;
                        return;
                    }
                    else
                    {
                        // No more sources. Finished indexing.
                        document = null;
                        isDiscoveryComplete = true;
                        return;
                    }
                }
            }
        }

        public int GetTotalDiscoveredDocuments()
        {
            int total = 0;
            foreach(var source in Sources)
            {
                total += source.GetTotalDiscoveredDocuments();
            }
            return total;
        }

        public double GetProgress()
        {
            try
            {
                int totalDiscoveredDocs = GetTotalDiscoveredDocuments();
                if (retrievedDocCount < totalDiscoveredDocs)
                {
                    return ProgressCalculator.GetXAsPercentOfYPrecise(retrievedDocCount, GetTotalDiscoveredDocuments());
                } else
                {
                    return 100;
                }
            } catch
            {
                return 0;
            }
        }

        public void Rewind()
        {
            _sourceIndex = 0;
            if (Sources.Count > 0)
            {
                _currentDataSource = Sources[0];
            }
            foreach (IDataSource source in Sources)
            {
                source.Rewind();
            }
            retrievedDocCount = 0;
        }

        public string Description()
        {
            // Multiple source datasource should never be used as a source in sourcelist, it is internal, so this description isn't shown anywhere.
            return "Multiple Source Datasource";
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return Description();
        }

        public void UseIndexConfig(IIndexConfiguration config)
        {
            foreach(var source in Sources)
            {
                if (source is ISupportsIndexConfigurationDataSource sicDS)
                {
                    sicDS.UseIndexConfig(config);
                }
            }
        }

        public void UseIndexTaskLog(ILogger logger)
        {
            foreach(var source in Sources)
            {
                source.UseIndexTaskLog(logger);
            }
            _logger = logger;
        }
    }
}
