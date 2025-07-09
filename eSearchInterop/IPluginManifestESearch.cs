using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Interop
{
    public interface IPluginManifestESearch
    {
        public string GetPluginName();

        public string GetPluginAuthor();

        public string GetPluginDescription();
        /// <summary
        /// Retrieve the minimum version of eSearch this plugin requires to function correctly.
        /// </summary>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        public void RequiresESearchVersion(out int major, out int minor);
    }
}
