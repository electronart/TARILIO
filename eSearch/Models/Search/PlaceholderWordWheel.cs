
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search
{
    public class PlaceholderWordWheel : IWordWheel
    {
        public event EventHandler AvailableWordsChanged;

        List<LuceneWordWheel.WheelWord> _wheelTerms;

        public PlaceholderWordWheel()
        {
            _wheelTerms = new List<LuceneWordWheel.WheelWord>
            {
                new LuceneWordWheel.WheelWord("...", 10, 4),
            };
            _wheelTerms = _wheelTerms.OrderBy(i => i.Word).ToList();
        }

        public int GetBestMatchIndex(string startSequence)
        {
            startSequence = startSequence.ToLower();
            int bestMatchingCharacters = 0;
            int bestMatch = 0;
            string word;
            int c;
            int matchingCharacters;
            for (int i = 0; i < _wheelTerms.Count; i++)
            {
                var wheelWord = _wheelTerms[i];
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

        public int GetTotalWords()
        {
            return _wheelTerms.Count;
        }

        public LuceneWordWheel.WheelWord GetWheelWord(int i)
        {
            try
            {
                return _wheelTerms[i];
            } catch (ArgumentOutOfRangeException)
            {
                return new LuceneWordWheel.WheelWord("Out of range!", 0, 0);
            }
        }

        public void SetContentOnly(bool contentOnly)
        {
            return; // Does nothing in placeholder.
        }

        public async Task BeginLoad()
        {
            return; // does nada in placeholder
        }

    }
}
