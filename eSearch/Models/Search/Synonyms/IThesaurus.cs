using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search.Synonyms
{
    public interface IThesaurus
    {
        /// <summary>
        /// Get Synonyms for this word, as defined in this thesaurus.
        /// </summary>
        /// <param name="wordOrPhrase">The word or phrase to seek. Case insensitive.</param>
        /// <returns>array of synonyms.</returns>
        public string[] GetSynonyms(string wordOrPhrase);

        /// <summary>
        /// Get the Synonym Groups in this thesaurus.
        /// </summary>
        /// <returns></returns>
        public SynonymGroup[] GetSynonymGroups();
    }
}
