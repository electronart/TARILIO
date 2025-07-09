using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Plugins
{
    public class eSearchPluginAssemblyLoadContext : AssemblyLoadContext
    {

        private readonly AssemblyDependencyResolver _resolver;

        private string _pluginDir = null;

        public eSearchPluginAssemblyLoadContext(string pluginDir) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginDir);
            _pluginDir = pluginDir;
        }


        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name == "eSearch.Interop")
            {
                return Assembly.Load(assemblyName); // Load from default context
            }
            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;

        }
    }
}
