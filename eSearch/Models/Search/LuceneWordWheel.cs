using DocumentFormat.OpenXml.Linq;
using eSearch.Models.Indexing;
using eSearch.Utils;
using eSearch.ViewModels;
using Lucene.Net.Index;
using Lucene.Net.Util;
using org.apache.commons.io.serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using S = eSearch.ViewModels.TranslationsViewModel;


namespace eSearch.Models.Search
{
    public class LuceneWordWheel : IWordWheel
    {

        private LuceneIndex _luceneIndex;
        private string      _startSequence = "";

        List<WheelWord> _wheelTermsContentOnly;
        List<WheelWord> _wheelTermsAllFields;

        BackgroundWorker wordsLoadingWorker = null;
        bool contentOnly = true;

        public event EventHandler AvailableWordsChanged;

        public LuceneWordWheel(LuceneIndex luceneIndex)
        {
            _luceneIndex = luceneIndex;
            _wheelTermsContentOnly = new List<WheelWord>();
            _wheelTermsAllFields = new List<WheelWord>();
            AvailableWordsChanged?.Invoke(this, new EventArgs());
        }

        public async Task BeginLoad()
        {
            #region Display Loading
            _wheelTermsContentOnly?.Clear();
            _wheelTermsAllFields?.Clear();
            var loadingWord = new WheelWord(S.Get("Loading..."), 0, 1);
            _wheelTermsAllFields.Add(loadingWord);
            _wheelTermsContentOnly.Add(loadingWord);
            AvailableWordsChanged?.Invoke(this, new EventArgs());
            #endregion
            #region Load/Populate the wheel asynchrnously
            var res = await LoadWordsAsync();
            _wheelTermsContentOnly = res.wheelWordsContentOnly;
            _wheelTermsAllFields = res.wheelWordsAllFields;
            AvailableWordsChanged?.Invoke(this, new EventArgs());
            #endregion
        }

        

        public void SetContentOnly(bool contentOnly)
        {
            if (this.contentOnly != contentOnly)
            {
                this.contentOnly = contentOnly;
                AvailableWordsChanged?.Invoke(this, new EventArgs());
            }
        }

        public int GetTotalWords()
        {
            if (contentOnly)
            {
                if (_wheelTermsContentOnly != null)
                {
                    return _wheelTermsContentOnly.Count;
                }
                else
                {
                    return 0;
                }
            } else
            {
                if (_wheelTermsAllFields != null)
                {
                    return _wheelTermsAllFields.Count;
                }
                else
                {
                    return 0;
                }
            }
        }

        public WheelWord GetWheelWord(int i)
        {
            if (contentOnly)
            {
                if (i >= 0 && _wheelTermsContentOnly.Count > i)
                {
                    return _wheelTermsContentOnly[i];
                }
                return new WheelWord("", 0, 0);
            } else
            {
                if (i >= 0 && _wheelTermsAllFields.Count > i)
                {
                    return _wheelTermsAllFields[i];
                }
                return new WheelWord("", 0, 0);
            }
        }

        /// <summary>
        /// May return -1 if words have not yet been loaded. Call LoadWords and await AvailableWordsChanged.
        /// </summary>
        /// <param name="startSequence"></param>
        /// <returns></returns>
        public int GetBestMatchIndex(string startSequence)
        {
            startSequence = startSequence.ToLower();
            _startSequence = startSequence;
            int bestMatchingCharacters = 0;
            int bestMatch = 0;
            string word;
            int c;
            int matchingCharacters;
            List<WheelWord> wordsToUse;
            if (contentOnly)
            {
                wordsToUse = _wheelTermsContentOnly;
            } else
            {
                wordsToUse = _wheelTermsAllFields;
            }

            if (wordsToUse == null)
            {
                return -1;
            }
            for(int i = 0; i < wordsToUse.Count; i++)
            {
                var wheelWord = wordsToUse[i];
                word = wheelWord.Word.ToLower();
                c = 0;
                matchingCharacters = 0;
                while (c < word.Length && c < startSequence.Length)
                {
                    if (word[c] == startSequence[c])
                    {
                        matchingCharacters++;
                    }
                    else
                    {
                        break;
                    }
                    ++c;
                }
                wheelWord.MatchedCharacters = matchingCharacters;
                if (matchingCharacters > bestMatchingCharacters)
                {
                    bestMatch = i;
                    bestMatchingCharacters = matchingCharacters;
                }
            }
            return bestMatch;
        }

        private async Task<(List<WheelWord> wheelWordsContentOnly, List<WheelWord> wheelWordsAllFields)> LoadWordsAsync()
        {

            var res = await Task.Run(() =>
            {
                // Also during load, if there was a previous start sequence, pre-calculate matched characters.
                var tempContentOnly = new HashSet<WheelWord>();
                var tempAllFields = new HashSet<WheelWord>();
                string term;
                int matchingCharacters;
                int c;

                var indexReader = _luceneIndex.GetCurrentReader();
                if (indexReader != null)
                {
                    Fields fields = MultiFields.GetFields(indexReader);
                    if (fields != null)
                    {
                        foreach (string field in fields)
                        {
                            if (field.StartsWith("_")) continue; // hidden field.
                            Terms terms = fields.GetTerms(field);
                            
                            TermsEnum iterator = terms.GetEnumerator();
                            
                            while (iterator.MoveNext())
                            {
                                long termFreq = 1;
                                if (iterator.TotalTermFreq > 1) termFreq = iterator.TotalTermFreq;
                                term = iterator.Term.Utf8ToString();
                                c = 0;
                                matchingCharacters = 0;
                                while (c < term.Length && c < _startSequence.Length)
                                {
                                    if (term[c] == _startSequence[c])
                                    {
                                        matchingCharacters++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                    ++c;
                                }
                                WheelWord wheelTerm = new WheelWord(term, matchingCharacters, termFreq);
                                if (field.Equals("Content")) AddTermToList(wheelTerm, tempContentOnly);
                                AddTermToList(wheelTerm, tempAllFields);
                            }
                        }
                    }
                }
                // now, sort the wheelterms.
                var culture = Utils.GetPreferredCulture(out bool isError);
                //return (tempContentOnly.ToList(), tempAllFields.ToList());
                return (
                tempContentOnly.OrderBy(i => i.Word, StringComparer.Create(culture, false)).ToList(),
                tempAllFields.OrderBy(i => i.Word, StringComparer.Create(culture, false)).ToList()
                );
            });
            return res;
        }

        private void AddTermToList(WheelWord wheelTerm, HashSet<WheelWord> wheelWords)
        {
            if (!wheelWords.Contains(wheelTerm))
            {
                wheelWords.Add(wheelTerm);
            } else
            {
                if (wheelWords.TryGetValue(wheelTerm, out var existingTerm))
                {
                    existingTerm.Frequency += wheelTerm.Frequency;
                }
            }
        }

        public struct WheelWord
        {
            /// <summary>
            /// Word in the WordWheel.
            /// </summary>
            /// <param name="word">The word</param>
            /// <param name="matched_characters">How many charaters matched against start sequence</param>
            /// <param name="frequency">The total times this term occurred in documents</param>
            public WheelWord(string word, int matched_characters, long frequency)
            {
                Word = word;
                MatchedCharacters = matched_characters;
                Frequency = frequency;
            }

            public string Word { get; }
            public int MatchedCharacters { get; set; }

            public long Frequency { get; set; }

            public string FrequencyShortString { 
                
                get
                {

                    if (Frequency > 1000000)
                    {
                        // Display in Millions.
                        string strFreq = Frequency.ToString();
                        int digits = strFreq.Length;
                        string millions = strFreq.Substring(0, strFreq.Length - 6);
                        string hundred_thousands = strFreq.Substring(strFreq.Length - 6, strFreq.Length - 6);

                        return millions + "M" + hundred_thousands;

                    }
                    if (Frequency > 1000)
                    {
                        // Thousands.
                        string strFreq = Frequency.ToString();
                        int digits = strFreq.Length;
                        string thousands = strFreq.Substring(0, strFreq.Length - 3);
                        string hundreds = strFreq.Substring(strFreq.Length - 3, strFreq.Length - 3);

                        return thousands + "K" + hundreds;
                    }
                    return Frequency.ToString();
                } 
            }

            #region The following is so that we can use HashSet on WheelTerm.
            public override int GetHashCode()
            {
                return Word.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is WheelWord && ((WheelWord)obj).Word == this.Word;
            }

            #endregion
        }
    }
}
