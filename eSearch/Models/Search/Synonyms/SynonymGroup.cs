using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search.Synonyms
{
    public class SynonymGroup
    {
        public string Name          { get; set; }
        public string[] Synonyms    { get; set; }

        public void SetSynonyms(string[] synonyms)
        {
            this.Synonyms = synonyms;
        }
    }
}
