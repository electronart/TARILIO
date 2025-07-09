using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search
{
    public class FixedListWordWheel : IWordWheel
    {
        public event EventHandler AvailableWordsChanged;


        List<string> words;

        public FixedListWordWheel(List<string> words)
        {
            this.words = words;
        }

        public static FixedListWordWheel GetAICompletionsWheel()
        {
            List<string> phrases = new List<string>
            {
                "What is",
                "What are",
                "What do",
                "What does",
                "What can",
                "Where is",
                "Where are",
                "Where do",
                "Where does",
                "Where can",
                "When is",
                "When are",
                "When do",
                "When does",
                "When can",
                "Why is",
                "Why are",
                "Why do",
                "Why does",
                "Why can",
                "Who is",
                "Who are",
                "Who do",
                "Who does",
                "Who can",
                "How is",
                "How are",
                "How can",
                "How do",
                "How does",
                "How long",
                "How many",
                "How tall",
                "How wide",
            };
            phrases.Sort();

            return new FixedListWordWheel(phrases);
        }

        public async Task BeginLoad()
        {
            return; // Does Nada
        }

        public int GetBestMatchIndex(string startSequence)
        {
            startSequence = startSequence.Trim().ToLower();
            int charsToBeat = -1;
            int bestIx = 0;
            int i = 0;
            while (i < words.Count)
            {
                string word = words[i].ToLower();
                int matchingChars = 0;
                for(int ci = 0; ci < word.Length; ci++)
                {
                    char c = word[ci];
                    if (startSequence.Length > ci)
                    {
                        if (startSequence[ci] == c)
                        {
                            ++matchingChars;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                if (matchingChars > charsToBeat)
                {
                    bestIx = i;
                    charsToBeat = matchingChars;
                }
                ++i;
            }
            return bestIx;
        }

        public int GetTotalWords()
        {
            return words.Count;
        }

        public LuceneWordWheel.WheelWord GetWheelWord(int i)
        {
            return new LuceneWordWheel.WheelWord(words[i], 0, int.MaxValue);
        }

        public void SetContentOnly(bool contentOnly)
        {
            return; // Ignored.
        }
    }
}
