using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Utils
{
    /// <summary>
    /// Lucene only indexes strings, so to do date search, dates need to be stored lexicographically.
    /// </summary>
    public static class DateUtils
    {

        /// <summary>
        /// return dateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string Serialize(DateTime? dateTime)
        {
            if (dateTime != null) { return ((DateTime)dateTime).ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture); }
            return "";
        }

        /// <summary>
        /// return DateTime.ParseExact(str, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static DateTime? Deserialize(string str)
        {
            if (str != "" && str != null)
            {
                return DateTime.ParseExact(str, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            }
            return null;
        }
    }
}
