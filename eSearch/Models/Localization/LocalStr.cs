using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Localization
{
    public static class LocalStr
    {
        static Language LoadedLanguage;

        public static void SetLanguage(Language Lang)
        {
            LoadedLanguage = Lang;
        }

        public static CultureInfo GetCulture()
        {
            string req = "RFC4646";
            string output = Get(req);
            if (output.Equals(req))
            {
                // No Culture, use current system culture
                return CultureInfo.CurrentCulture;
            }
            else
            {
                try
                {
                    return new CultureInfo(output);
                }
                catch (CultureNotFoundException cnf)
                {
                    //Program.SetLastError("Culture not found: " + output, cnf);
                    return CultureInfo.CurrentCulture;
                }
            }
        }

        public static string Get(string s)
        {
            if (LoadedLanguage == null)
            {
                return s;
            }
            else
            {
                if (LoadedLanguage.TryGetTranslation(s, out string translation, out int ignored))
                {
                    return translation;
                }
                else
                {
                    // No translation for this string available in the given language
                    return s;
                }
            }
        }
    }
}
