using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Utils
{
    public static class EnumDescriptions
    {

        public static string GetDescription<TEnum>(this TEnum EnumValue) where TEnum : struct
        {
            var field = EnumValue.GetType().GetField(EnumValue.ToString());
            if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
            {
                return attribute.Description;
            }
            throw new ArgumentException("Item not found.", nameof(EnumValue));
        }
    }
}
