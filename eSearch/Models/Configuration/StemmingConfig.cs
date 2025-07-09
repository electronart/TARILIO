using eSearch.Models.Search.Stemming;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Configuration
{
    public class StemmingConfig
    {
        /// <summary>
        /// Whether or not to use English Porter Stemming.
        /// </summary>
        public bool     UseEnglishPorter = true;
        /// <summary>
        /// Full path to a stemming file or null if nothing selected.
        /// </summary>
        public string StemmingFile = null;

        /// <summary>
        /// May return null if no stemming file currently set or the stemming file does not exist in the specified directory.
        /// </summary>
        /// <returns></returns>
        public StemmingRules LoadActiveStemmingRules()
        {
            if (StemmingFile == null) return null;
            if (!File.Exists(StemmingFile)) return null;
            var rules = StemmingRules.FromFile(StemmingFile);
            return rules;
        }

    }
}
