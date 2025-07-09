using eSearch.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search
{
    public static class ResultExtensions
    {
        public static string? GetMetadataValue(this IResult result, string key)
        {
            if (result == null) return null;
            if (result.Metadata == null) return null;
            foreach (var item in result.Metadata)
            {
                if (item.Key == key) return item.Value;
            }
            return null;
        }

        public static string? GetMetadataValue(this ResultViewModel rvm, string key)
        {
            if (rvm == null) return null;
            var result = rvm.GetResult();
            return GetMetadataValue(result, key);
        }
    }
}
