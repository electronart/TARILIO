
using com.sun.corba.se.spi.orb;
using eSearch.Models.Indexing;
using eSearch.Models.Search.Synonyms;
using eSearch.ViewModels;
using Lucene.Net.Tartarus.Snowball.Ext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search
{
    public class QueryExpander
    {

        public QueryExpander(IIndex index) {
            _index = index;
            
        }


        private IIndex _index;
        private string _rawQuery        = null;
        private QueryViewModel _qvm     = null;
        IThesaurus[] _thesauri          = null;

        private StringBuilder queryExpanderSB = new StringBuilder();
        private StringBuilder queryExpanderWordSB = new StringBuilder();

        private PorterStemmer? _porterStemmer;


        public void SetQuery(string query, QueryViewModel qvm)
        {
            _rawQuery = query.Trim();
            _qvm = qvm;
            _thesauri = qvm.Thesauruses;
            queryExpanderSB.Clear();
            queryExpanderWordSB.Clear();

        }

        public string Expand()
        {
            string[] connectors = { "and", "or", "not" };
            int i = 0;
            int len = _rawQuery.Length;
            bool phrase = false;
            string wordPhrase;
            string s;


            while (i < len)
            {
                char c = _rawQuery[i];
                if (c == '"')
                {
                    phrase = !phrase;
                }
                else
                {
                    if (c == ' ' && phrase == false)
                    {
                        // End of word/phrase.
                        wordPhrase = queryExpanderWordSB.ToString();
                        queryExpanderWordSB.Clear();
                        if (wordPhrase != "")
                        {
                            if (!connectors.Contains(wordPhrase.ToLower())) 
                            {
                                // Not a connector word such as AND, OR, NOT
                                queryExpanderSB
                                    .Append("(")
                                    .Append(_expandWordPhrase(wordPhrase))
                                    .Append(") ");
                            } else
                            {
                                // Connector word.
                                queryExpanderSB
                                    .Append(wordPhrase)
                                    .Append(" ");
                            }
                        }

                    }
                    else
                    {
                        queryExpanderWordSB.Append(c);
                    }
                }

                ++i;
            }
            wordPhrase = queryExpanderWordSB.ToString();
            if (wordPhrase != "")
            {
                if (!connectors.Contains(wordPhrase.ToLower()))
                {
                    // Not a connector word such as AND, OR, NOT
                    queryExpanderSB
                        .Append("(")
                        .Append(_expandWordPhrase(wordPhrase))
                        .Append(") ");
                }
                else
                {
                    // Connector word.
                    queryExpanderSB
                        .Append(wordPhrase)
                        .Append(" ");
                }
            }
            return queryExpanderSB.ToString();
        }


        private string _expandWordPhrase(string wordPhrase)
        {
            HashSet<string> words = new HashSet<string>();
            if (_qvm.UseSynonyms)
            {
                string[] synonyms = _getSynonyms(wordPhrase);
                int i = synonyms.Length;
                while (i-- > 0)
                {
                    words.Add(synonyms[i]);
                }
            } else
            {
                words.Add(wordPhrase);
            }

            if (_qvm.UseStemming)
            {
                string stemmedWord = _stemWordAccordingToSettings(wordPhrase);
                words.Add(stemmedWord);

                if (_qvm.UseSynonyms)
                {
                    // Additionally add the Synonyms of the Stem of the Word.
                    string[] synonyms = _getSynonyms(stemmedWord);
                    int i = synonyms.Length;
                    while (i-- > 0)
                    {
                        words.Add(synonyms[i]);
                    }
                }

                string[] wordsWithMatchingStemsInWordList = _getWordsWithMatchingStemsFromWordList(stemmedWord);
                int ii = wordsWithMatchingStemsInWordList.Length;
                while (ii --> 0)
                {
                    words.Add(wordsWithMatchingStemsInWordList[ii]);
                }
            }
            return string.Join(" OR ", words.ToArray());
        }

        private string[] _getWordsWithMatchingStemsFromWordList(string stemmedWord)
        {
            List<string> temp = new List<string>();
            
            int i = _index.WordWheel.GetTotalWords();
            string wheelWord;
            while ( i --> 0 )
            {
                wheelWord = _index.WordWheel.GetWheelWord(i).Word;
                if (stemmedWord == _stemWordAccordingToSettings(wheelWord))
                {
                    temp.Add(wheelWord);
                }
            }

            return temp.ToArray();
        }

        private string _stemWordAccordingToSettings(string wordPhrase)
        {
            if (Program.ProgramConfig.StemmingConfig.UseEnglishPorter)
            {
                return _getPorterStemmedWord(wordPhrase);
            } else if (_qvm.StemmingRules != null)
            {
                return _qvm.StemmingRules.StemWord(wordPhrase);
            }
            return wordPhrase;
        }

        private string _getPorterStemmedWord(string word)
        {
            if (_porterStemmer == null)
            {
                _porterStemmer = new PorterStemmer();
            }
            _porterStemmer.SetCurrent(word);
            if (_porterStemmer.Stem())
            {
                return _porterStemmer.Current;
            }
            return word;
        }

        private string[] _getSynonyms(string wordPhrase)
        {
            HashSet<string> synonyms = new HashSet<string>();
            int i = _thesauri.Length;
            int s;
            while (i --> 0)
            {
                string[] thesauriSynonyms = _thesauri[i].GetSynonyms(wordPhrase);
                s = thesauriSynonyms.Length;
                while (s --> 0)
                {
                    synonyms.Add(thesauriSynonyms[s]);
                }
            }
            return synonyms.ToArray();
        }

    }
}
