using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PseudoLocalizer;
using UglyToad.PdfPig.Fonts.TrueType.Names;

namespace eSearch.Models.Localization
{
    public class Language
    {
        String LanguageName;
        public Dictionary<String, Tuple<int, string>> Translations;

        bool _psuedolocalise = false;

        Language()
        {
            Translations = new Dictionary<string, Tuple<int, string>>();
        }

        public static Language DynamicPsuedolocalisationLanguage()
        {
            Language language = new Language();
            language._psuedolocalise = true;
            return language;
        }


        public Boolean TryGetTranslation(string identifier, out string translation, out int line)
        {
            if (_psuedolocalise)
            {
                translation = Psuedolocalise(identifier);
                line = 0;
                return true;
            }
            if (Translations.TryGetValue(identifier, out var tuple))
            {
                line = tuple.Item1;
                translation = tuple.Item2;
                return true;
            }
            else
            {
                line = -1;
                translation = "";
                return false;
            }
        }

        private string Psuedolocalise(string identifier)
        {
            return identifier.PseudoLocalize();
        }

        public void ShowDebugDialog()
        {
            //var translation_debug_dialog = new translation_debug(Translations);
            //translation_debug_dialog.ShowDialog();
        }

        public String GetLanguageName()
        {
            return LanguageName;
        }

        public static Language LoadLanguage(String LanguageFile)
        {
            Language l = new Language();
            String Identifier = "";
            String Str = "";
            int i = 0; // Even = Identifier, Odd = Translation.
            int lineNumber = 0;
            using (StreamReader fileStream = new StreamReader(LanguageFile))
            {
                while ((Str = fileStream.ReadLine()) != null)
                {
                    ++lineNumber;
                    if (Str.StartsWith("#"))
                    {
                        // It's a comment. Don't count it.
                    }
                    else
                    {
                        if (i % 2 == 0)
                        {
                            // even
                            Identifier = Str;
                        }
                        else
                        {
                            // odd
                            try
                            {
                                l.Translations.Add(Identifier, Tuple.Create(lineNumber, Str));
                            }
                            catch (ArgumentException ar)
                            {
                                // Duplicate key. Just continue;
                            }

                        }
                        i++;
                    }
                }
            }
            l.LanguageName = Path.GetFileName(LanguageFile).Split('.').First();
            return l;
        }
    }
}
