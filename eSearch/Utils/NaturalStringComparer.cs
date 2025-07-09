using com.sun.corba.se.spi.orb;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace eSearch.Utils
{
    public class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            // Split strings into parts of numbers and non-numbers
            var partsX = SplitIntoParts(x);
            var partsY = SplitIntoParts(y);

            for (int i = 0; i < Math.Min(partsX.Length, partsY.Length); i++)
            {
                var partX = partsX[i];
                var partY = partsY[i];

                if (partX.IsNumeric && partY.IsNumeric)
                {
                    if (partX.Number != partY.Number)
                        return partX.Number.CompareTo(partY.Number);
                }
                else
                {
                    int result = string.Compare(partX.Text, partY.Text, StringComparison.Ordinal);
                    if (result != 0) return result;
                }
            }

            // If one string has more parts, it comes after
            return partsX.Length.CompareTo(partsY.Length);
        }


        private static StringPart[] SplitIntoParts(string s)
        {
            if (string.IsNullOrEmpty(s)) return Array.Empty<StringPart>();

            var parts = new List<StringPart>();
            int start = 0;
            bool wasDigit = char.IsDigit(s[0]);

            for (int i = 1; i <= s.Length; i++)
            {
                bool isDigit = i < s.Length && char.IsDigit(s[i]);
                if (i == s.Length || isDigit != wasDigit)
                {
                    string part = s.Substring(start, i - start);
                    if (wasDigit && long.TryParse(part, out long number))
                    {
                        parts.Add(new StringPart(part, true, number));
                    }
                    else
                    {
                        parts.Add(new StringPart(part, false));
                    }
                    start = i;
                    wasDigit = isDigit;
                }
            }

            return parts.ToArray();
        }

        // Represents a precomputed part of a string (numeric or non-numeric)
        private readonly struct StringPart
        {
            public readonly bool IsNumeric;
            public readonly string Text;
            public readonly long Number; // Use long to handle larger numbers

            public StringPart(string text, bool isNumeric, long number = 0)
            {
                Text = text;
                IsNumeric = isNumeric;
                Number = number;
            }
        }
    }
}
