using DocumentFormat.OpenXml.ExtendedProperties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Indexing
{
    public class StopWords
    {
        HashSet<string> words = new HashSet<string>();

        public static StopWords FromFiles(string[] files)
        {
            StopWords[] stopWordFiles = new StopWords[files.Length];
            int i = files.Length;
            while (i --> 0)
            {
                stopWordFiles[i] = FromFile(files[i]);
            }

            StopWords combined = new StopWords();

            i = files.Length;
            while (i --> 0)
            {
                var stopWordFile = stopWordFiles[i];
                foreach(var word in stopWordFile.words)
                {
                    combined.words.Add(word);
                }
            }

            return combined;
        }

        public List<string> GetWords()
        {
            return words.ToList();
        }

        public static StopWords FromFile(string fileName)
        {
            try
            {
                if (!Path.IsPathRooted(fileName))
                {
                    fileName = Path.Combine(Program.ESEARCH_STOP_FILE_DIR, fileName);
                    if (!fileName.ToLower().EndsWith(".dat")) fileName += ".dat";
                }


                var stopWords = new StopWords();
                string[] temp = File.ReadAllLines(fileName);
                foreach (var word in temp)
                {
                    if (word.Trim().Length > 0)
                    { 
                        stopWords.words.Add(word.Trim());
                    }
                }
                return stopWords;
            } catch (FileNotFoundException)
            {
                // TODO Should probably warn the user about this...
                return new StopWords();
            }
        }


    }
}
