using eSearch.Models.Search.Synonyms;
using eSearch.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Configuration
{
    public class SynonymsConfig
    {
        /// <summary>
        /// Whether or not to use the active synonym files.
        /// True by default.
        /// </summary>
        public bool UseSynonymFiles = true;

        /// <summary>
        /// Whether or not to use the English WordNet when UseSynonyms is on in search session.
        /// True by default.
        /// </summary>
        public bool UseEnglishWordNet = true;

        /// <summary>
        /// List of Synonym files that are currently active.
        /// </summary>
        public List<String> ActiveSynonymFiles = new List<String>();

        /// <summary>
        /// The last viewed synonyms file, if any.
        /// </summary>
        public string LastViewedSynonymsFile;

        /// <summary>
        /// Note this method performs FileIO nd should avoid being polled.
        /// </summary>
        /// <returns></returns>
        public IThesaurus[] GetActiveThesauri()
        {
            List<IThesaurus> thesauri = new List<IThesaurus>();
            // Ensure the eSearch Synonyms Directory exists.
            Directory.CreateDirectory(Program.ESEARCH_SYNONYMS_DIR);
            string[] files = Directory.GetFiles(Program.ESEARCH_SYNONYMS_DIR, "*.xml", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                bool active = Program.ProgramConfig.SynonymsConfig.ActiveSynonymFiles.Contains(System.IO.Path.GetFileName(file));
                if (active)
                {
                    thesauri.Add(UTP_Thesaurus.LoadThesaurus(file));
                }
            }
            return thesauri.ToArray();
        }
    }
}
