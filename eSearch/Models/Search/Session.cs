using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eSearch.Models.Indexing;

namespace eSearch.Models.Search
{
    /// <summary>
    /// A search session. eSearch remembers the users last search session(s) and restores them
    /// when the user next opens the application.
    /// </summary>
    internal class Session
    {
        Query? query;
        IIndex? index;
    }
}
