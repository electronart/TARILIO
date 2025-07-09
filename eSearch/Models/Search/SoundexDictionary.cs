using Lucene.Net.Analysis.Phonetic.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search
{
    public class SoundexDictionary
    {
        private IWordWheel loadedWheel;

        private Dictionary<string, HashSet<string>> SoundexDict = new();

        Soundex SoundexEncoder
        {
            get
            {
                if (_soundexEncoder == null)
                {
                    _soundexEncoder = new Soundex();
                }
                return _soundexEncoder;
            }
        }

        Soundex? _soundexEncoder = null;

        /// <summary>
        /// loaded_wheel should be a word wheel that has already loaded its words.
        /// </summary>
        /// <param name="loaded_wheel"></param>
        public SoundexDictionary(IWordWheel loaded_wheel)
        {
            this.loadedWheel = loaded_wheel;
        }

        public async Task BuildDictionary()
        {
            await Task.Run(() =>
            {
                var soundexEncoder = new Soundex();
                LuceneWordWheel.WheelWord word;
                string soundex;
                int c = loadedWheel.GetTotalWords();
                int i = 0;
                while (i < c)
                {
                    word = loadedWheel.GetWheelWord(i);
                    try
                    {
                        if (IsAlphabetic(word.Word)) // This check is to avoid as many performance heavy exceptions as possible.
                        {
                            soundex = soundexEncoder.Encode(word.Word);
                        } else
                        {
                            soundex = word.Word;
                        }
                    }
                    catch (ArgumentException)
                    {
                        soundex = word.Word;
                    }
                    if (SoundexDict.ContainsKey(soundex))
                    {
                        // Already contains a matching soundex key, add this string as another word that matches the soundex.
                        SoundexDict[soundex].Add(word.Word);
                    }
                    else
                    {
                        // Doesn't yet contain a matching soundex key.
                        SoundexDict.Add(soundex, new HashSet<string> { word.Word });
                    }
                    
                    ++i;
                }
            });
        }

        public static bool IsAlphabetic(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            foreach (char c in input)
            {
                if ((c < 'A' || c > 'Z') && (c < 'a' || c > 'z'))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// May return null if no words in the index match the soundex key.
        /// </summary>
        /// <param name="soundexCode"></param>
        /// <returns></returns>
        public HashSet<string>? GetMatchingWordsBySoundexCode(string soundexCode)
        {
            if (SoundexDict.ContainsKey(soundexCode)) return SoundexDict[soundexCode];
            return null;
        }

        public HashSet<string>? GetSoundexMatchesForWord(string word)
        {
            return GetMatchingWordsBySoundexCode(SoundexEncoder.Encode(word));
        }

    }
}
