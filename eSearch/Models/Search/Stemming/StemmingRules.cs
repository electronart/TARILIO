using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search.Stemming
{
    public class StemmingRules
    {
        /// <summary>
        /// A Stemming.dat rule file.
        /// </summary>
        public string FileName
        {
            get; set;
        }

        public string DisplayName
        {
            get
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(FileName);
                if (fileNameWithoutExtension.StartsWith("ST_"))
                {
                    return fileNameWithoutExtension.Substring(3);
                }
                return fileNameWithoutExtension;
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }

        // Based on
        // https://support.dtsearch.com/webhelp/dtsearch/Stemming_rules_format.htm
        // 3+ies -> Y
        // 4+ing ->
        // etc.

        /// <summary>
        /// The following 3 lists run parallel. Each index is a line in the stemrule file.
        /// </summary>
        int[]       stemrule_minimum_letters;       // 3
        string[]    stemrule_suffix_to_remove;      // ies
        string[]    stemrule_suffix_to_replace;     // Y

        /// <summary>
        /// Read a Stemming.dat file to StemmingRules.
        /// No exceptions caught here.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static StemmingRules FromFile(string file)
        {
            List<int>    rule_minimum_letters   = new List<int>();
            List<string> rule_suffix_to_remove  = new List<string>();
            List<string> rule_suffix_to_replace = new List<string>();

            string[] lines = File.ReadAllLines(file);

            foreach (string line in lines)
            {
                if (line.Contains("---") || line.Contains("==="))
                {
                    break; // Comments only beyond this point.
                }
                if (line.Contains("->") && line.Contains("+"))
                {
                    string[] split = line.Split('+');
                    rule_minimum_letters.Add(int.Parse(split[0].Trim()));
                    split = split[1].Split("->");
                    rule_suffix_to_remove.Add(split[0].Trim());
                    rule_suffix_to_replace.Add(split[1].Trim());
                }
                if (line.Contains("->") && !line.Contains("+"))
                {
                    rule_minimum_letters.Add(0);
                    string[] split = line.Split("->");
                    rule_suffix_to_remove.Add(split[0].Trim());
                    rule_suffix_to_replace.Add(split[1].Trim());
                }
            }

            return new StemmingRules
            {
                FileName = file,
                stemrule_minimum_letters = rule_minimum_letters.ToArray(),
                stemrule_suffix_to_remove = rule_suffix_to_remove.ToArray(),
                stemrule_suffix_to_replace = rule_suffix_to_replace.ToArray()
            };
        }

        public string StemWord(string originalWord)
        {
            string word = originalWord.Trim().ToLower();

            int     minimum_letters;
            string  suffix_to_remove;
            string  suffix_to_replace;

            bool changed = true;
            while (changed)
            {
                changed = false;
                int i = 0;
                int len = stemrule_minimum_letters.Length;

                while (i < len)
                {
                    // Loop through stem rules.
                    minimum_letters = stemrule_minimum_letters[i];
                    suffix_to_remove = stemrule_suffix_to_remove[i].ToLower().Trim();
                    suffix_to_replace = stemrule_suffix_to_replace[i].ToLower().Trim();

                    if (word.EndsWith(suffix_to_remove) && word.Length >= (minimum_letters + suffix_to_remove.Length))
                    {
                        string newWord = word.Substring(0, word.Length - suffix_to_remove.Length) + suffix_to_replace;
                        if (newWord != word)
                        {
                            word = newWord;
                            changed = true;
                            break;
                        } else
                        {
                            // Matching rule, but the word didn't change.
                            changed = false;
                            break;
                        }
                    }

                    ++i;
                }
            }
            return word;
        }

    }
}
