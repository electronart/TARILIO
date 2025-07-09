using sun.tools.tree;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Utils
{
    public static class Expando
    {
        public static void AddProperty(this ExpandoObject o, string propertyName, object property)
        {
            var expandoDict = o as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
            {
                expandoDict[propertyName] = property;
            }
            else
            {
                expandoDict.Add(propertyName, property);
            }
        }
    }
}
