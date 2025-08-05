using eSearch.Models.Indexing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Configuration
{
    public class IndexLibrary
    {
        string LibraryFileLocation = "";

        public List<LuceneIndexConfiguration> LuceneIndexes = new List<LuceneIndexConfiguration>();

        public static IndexLibrary LoadLibrary(string LibraryFileLocation)
        {
            Debug.WriteLine("Load Index Library " + LibraryFileLocation);
            if (File.Exists(LibraryFileLocation))
            {
               IndexLibrary library = JsonConvert.DeserializeObject<IndexLibrary>(File.ReadAllText(LibraryFileLocation).Replace("DesktopSearch2","eSearch")) ?? new IndexLibrary();
               library.LibraryFileLocation = LibraryFileLocation;
               return library;
            } else
            {
                IndexLibrary library = new IndexLibrary();
                library.LibraryFileLocation = LibraryFileLocation;
                return library;
            }
        }

        public void RemoveIndex(string indexID)
        {
            LuceneIndexes.RemoveAll(index => index.LuceneIndex.Id == indexID);
            // !! remember to add similar logic for each type of index.
        }

        public void SaveLibrary()
        {
            Debug.WriteLine("Save Index Library " + LibraryFileLocation);
            string dirName = new FileInfo(LibraryFileLocation).Directory.FullName;
            if (!System.IO.Directory.Exists(dirName))
            {
                System.IO.Directory.CreateDirectory(dirName);
            }
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(LibraryFileLocation, json);
            } catch (Exception ex)
            {
                string message = ex.Message;
                string trace = ex.StackTrace;
                Debug.WriteLine("BREAK" + message + " " + trace);
            }
            
        }

        public IIndexConfiguration? GetConfiguration(IIndex index)
        {
            if (index == null) return null;
            if (index is LuceneIndex lcn)
            {
                return GetConfiguration(lcn);
            }
            return null;
        }

        public LuceneIndexConfiguration? GetConfiguration(LuceneIndex index)
        {
            foreach(var config in LuceneIndexes)
            {
                if (config.LuceneIndex.Id == index.Id)
                {
                    return config;
                }
            }
            return null;
        }

        public void UpdateConfiguration(LuceneIndexConfiguration configuration)
        {
            int i = 0;
            int len = LuceneIndexes.Count;
            while (i < len)
            {
                if (LuceneIndexes[i].LuceneIndex.Id == configuration.LuceneIndex.Id)
                {
                    // TODO - Bit of a hack, This updates the configuration but maintains the column display settings
                    var existingColumnDisplaySettings = LuceneIndexes[i].ColumnDisplaySettings;
                    var existingColumnSizingSettings  = LuceneIndexes[i].ColumnSizingMode;
                    LuceneIndexes[i] = configuration;
                    LuceneIndexes[i].ColumnDisplaySettings = existingColumnDisplaySettings;
                    LuceneIndexes[i].ColumnSizingMode      = existingColumnSizingSettings;
                    break;
                }
                ++i;
            }
        }

        public IIndex? GetIndex(string indexName)
        {
            var indexes = GetAllIndexes();
            return indexes.FirstOrDefault(x => x?.Name == indexName, null);
        }

        public IIndex? GetIndexById(string indexId)
        {
            return GetAllIndexes().FirstOrDefault(x => x?.Id == indexId, null);
        }

        public List<IIndex> GetAllIndexes()
        {
            List<IIndex> indexes = new List<IIndex>();
            foreach(var index in LuceneIndexes)
            {
                indexes.Add(index.LuceneIndex);
            }
            indexes.Sort((x, y) => string.Compare(x.Name, y.Name));
            return indexes;
        }
    }
}
