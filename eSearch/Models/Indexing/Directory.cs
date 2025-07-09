using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Indexing
{
    public class Directory
    {
        public string Path;
        public bool Recursive;

        /// <summary>
        /// _SubDirectories is only populated by Discovery Worker in FileSystemDataSource. It won't always be populated.
        /// Will be null when not yet loaded
        /// </summary>
        private Directory[]? _DiscoveredSubDirectories = null;
        /// <summary>
        /// _DiscoveredFiles is only populated by Discovery Worker in FileSystemDataSource. It won't always be populated.
        /// Will be null when not yet loaded.
        /// </summary>
        private string[]? _DiscoveredFiles = null;

        public Directory(string Path, bool Recursive)
        {
            this.Path = Path;
            this.Recursive = Recursive;
        }

        public void SetDiscoveredFiles(string[] DiscoveredFiles)
        {
            _DiscoveredFiles = DiscoveredFiles;
        }

        public string[]? GetDiscoveredFiles()
        {
            return _DiscoveredFiles;
        }

        public void SetDiscoveredSubDirectories(Directory[] DiscoveredSubDirectories)
        {
            _DiscoveredSubDirectories = DiscoveredSubDirectories;
        }

        public Directory[]? GetDiscoveredSubDirectories()
        {
            return _DiscoveredSubDirectories;
        }
    }
}
