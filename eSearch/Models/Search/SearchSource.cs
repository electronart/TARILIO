using eSearch.Models.Configuration;
using eSearch.Models.Indexing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search
{
    public class SearchSource
    {

        // 28 Jan 2025
        // There was a desire to shift the drop down that was previously index selection to also be able to show AI Sources
        // When we were adding AI Search. SearchSource is an abstraction layer to make this possible with the view model.

        /// <summary>
        /// 
        /// </summary>
        /// <param name="displayName">Will be displayed in the Selection drop down</param>
        /// <param name="source">An IIndex or an AISearchConfiguration</param>
        public SearchSource(string displayName, object source)
        {
            DisplayName = displayName;
            if (source is not IIndex && source is not AISearchConfiguration)
            {
                throw new Exception("Unsupported Source");
            }
            Source = source;
        }

        public string DisplayName { get; set; }

        /// <summary>
        /// The source object. This will either be an AISearchConfiguration object or an object that implements IIndex for a traditional index.
        /// </summary>
        public object Source { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }

    }
}
