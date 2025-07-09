using eSearch.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Plugins
{
    public class Plugin
    {

        eSearchPluginAssemblyLoadContext?   LoadContext;
        Assembly?                           PluginAssembly;
        private string?                     PluginFolder;


        /// <summary>
        /// Note that this does not check plugin compatibility itself.
        /// </summary>
        /// <param name="pluginFolder"></param>
        /// <returns></returns>
        public static Plugin? LoadPlugin(string pluginFolder)
        {
            
            string mainDLLPath = Path.Combine(pluginFolder, "MainDLL");
            if (!File.Exists(mainDLLPath)) return null;
            string mainDLL = File.ReadAllText( Path.Combine(pluginFolder, "MainDLL") );

            string dllPath = Path.Combine(pluginFolder, mainDLL);
            if (!File.Exists(dllPath))
            {
                throw new FileNotFoundException(dllPath);
            }

            var context = new eSearchPluginAssemblyLoadContext(dllPath);

            return new Plugin
            {
                PluginAssembly = context.LoadFromAssemblyPath(dllPath),
                PluginFolder = pluginFolder,
                LoadContext = context
            };
        }

        public string? GetPluginVersion()
        {
            return PluginAssembly?.GetName().Version?.ToString();
        }

        public void Unload()
        {
            PluginAssembly = null;
            LoadContext?.Unload();
        }

        public IPluginManifestESearch? GetPluginManifest()
        {
            try
            {
                Type? pluginType = PluginAssembly?.GetTypes()
                                        .FirstOrDefault(t => typeof(IPluginManifestESearch).IsAssignableFrom(t) && !t.IsAbstract);
                if (pluginType != null)
                {
                    if (Activator.CreateInstance(pluginType) is IPluginManifestESearch instance)
                    {
                        return instance;
                    }
                }
                return null;
            } catch (Exception ex)
            {
                Debug.WriteLine("Exception getting Plugin Manifest");
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        public IEnumerable<IPluginDataSourceManager> GetPluginDataSourceManagers()
        {
            List<IPluginDataSourceManager> managers = new List<IPluginDataSourceManager>();
            try
            {
                foreach (Type type in PluginAssembly.GetTypes())
                {
                    if (typeof(IPluginDataSourceManager).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                    {
                        if (Activator.CreateInstance(type) is IPluginDataSourceManager instance)
                        {
                            managers.Add(instance);
                        }
                    }
                }
                return managers;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception looking for plugin datasource managers");
                Debug.WriteLine(ex.ToString());
                return managers;
            }
        }

        public override string ToString()
        {
            return GetPluginManifest()?.GetPluginName() ?? "???";
        }

        public string GetPluginFolder()
        {
            return PluginFolder ?? "";
        }
    }
}
