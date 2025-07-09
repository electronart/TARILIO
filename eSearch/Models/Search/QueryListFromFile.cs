using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static eSearch.ViewModels.QueryViewModel;

namespace eSearch.Models.Search
{
    public class QueryListFromFile
    {
        private string txtFile;

        public QueryListFromFile(string txtFile)
        {
            this.txtFile = txtFile;
        }

        public string GetQuery(SearchType searchType)
        {
            List<string> wordPhrases = GetQueries();
            switch(searchType)
            {
                case SearchType.AnyWords:
                    return string.Join(" OR ", wordPhrases);
                default:
                    return string.Join(" AND ", wordPhrases);
            }
        }

        public List<string> GetQueries()
        {
            if (_queries == null)
            {
                _queries = new List<string>();
                loadQueries();
            }
            return _queries;
        }

        private List<string> _queries = null;



        private void loadQueries()
        {
            try
            {
                string txt = File.ReadAllText(txtFile);
                string[] temp = txt.Split(Environment.NewLine);
                foreach(string line in temp)
                {
                    _queries.Add( line );
                }
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

    }
}
