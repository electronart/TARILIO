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
    public class PluginLoader
    {

        private List<Plugin>? LoadedPlugins = null;


        /// <summary>
        /// Install a plugin .esplugin file to eSearch.
        /// </summary>
        /// <param name="pluginFile"></param>
        public async Task InstallPlugin(string pluginFile)
        {
            await Task.Run(async () =>
            {

                if (!pluginFile.ToLower().EndsWith(".esplugin"))
                {
                    throw new ArgumentException("Plugin does not have the .esplugin extension");
                }

                var pluginDir = Path.Combine(Program.ESEARCH_PLUGINS_DIR, Guid.NewGuid().ToString());
                Directory.CreateDirectory(pluginDir);

                System.IO.Compression.ZipFile.ExtractToDirectory(pluginFile, pluginDir);

                Plugin? plugin          = Plugin.LoadPlugin(Path.Combine(Program.ESEARCH_PLUGINS_DIR, Path.GetFileNameWithoutExtension(pluginDir)));
                var pluginManifest  = plugin?.GetPluginManifest();

                if (pluginManifest == null) throw new InvalidDataException("The file is not a valid plugin file or is corrupt.");
                if (!IsPluginCompatible(pluginManifest)) throw new NotSupportedException("This plugin requires a newer version of eSearch. Please update.");
                Program.ProgramConfig.InstalledPlugins.Add(Path.GetFileNameWithoutExtension(pluginDir));
                Program.SaveProgramConfig();
                if (LoadedPlugins == null)
                {
                    LoadedPlugins = new List<Plugin>();
                }

                int i = LoadedPlugins.Count;
                while (i --> 0)
                {
                    Plugin loadedPlugin = LoadedPlugins[i];
                    if (loadedPlugin.ToString() == plugin?.ToString())
                    {
                        await UninstallPlugin(loadedPlugin);
                        i = LoadedPlugins.Count;
                    }
                }

                if (plugin != null) LoadedPlugins.Add(plugin);
            });
        }

        public async Task UninstallPlugin(Plugin plugin)
        {
            await Task.Run(() =>
            {
                var directory = plugin.GetPluginFolder();
                
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    try
                    {
                        plugin.Unload();
                        // Directory.Delete(directory, true); Save this for Program Startup
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        if (LoadedPlugins?.Contains(plugin) ?? false)
                        {
                            LoadedPlugins?.Remove(plugin);
                        }
                        Program.ProgramConfig.InstalledPlugins.Remove(Path.GetFileName(directory));
                        Program.SaveProgramConfig();
                    }
                }
            });
        }


        public async Task<IEnumerable<Plugin>> GetInstalledPlugins()
        {
            return await Task.Run(() =>
            {
                if (LoadedPlugins == null)
                {
                    Directory.CreateDirectory(Program.ESEARCH_PLUGINS_DIR); // Ensure Plugins Dir exists

                    LoadedPlugins = new List<Plugin>();
                    foreach(var pluginID in Program.ProgramConfig.InstalledPlugins)
                    {
                        if (Directory.Exists(Path.Combine(Program.ESEARCH_PLUGINS_DIR, pluginID)))
                        {
                            Plugin? plugin = Plugin.LoadPlugin(Path.Combine(Program.ESEARCH_PLUGINS_DIR, pluginID));
                            var pluginManifest = plugin?.GetPluginManifest();
                            if (plugin != null && IsPluginCompatible(pluginManifest))
                            {
                                LoadedPlugins.Add(plugin);
                            }
                        }
                    }
                    #region Cleanup any unused plugin folders at initiation
                    var pluginDirectories = Directory.GetDirectories(Program.ESEARCH_PLUGINS_DIR);
                    foreach(var pluginDirectory in pluginDirectories)
                    {
                        var pluginID = Path.GetFileName(pluginDirectory);
                        if (!Program.ProgramConfig.InstalledPlugins.Contains(pluginID))
                        {
                            // This isn't an installed plugin. Attempt to delete the folder.
                            try
                            {
                                Directory.Delete(pluginDirectory, true);
                            } catch (Exception ex)
                            {
                                Debug.WriteLine("Error cleaning up plugin dir: " + ex.ToString()); // Consider this non-fatal. eSearch will try to clean it up next time.
                            }
                        }
                    }
                    #endregion
                }
                return LoadedPlugins;
            });
        }

        private bool IsPluginCompatible(IPluginManifestESearch? pluginManifest)
        {
            if (pluginManifest == null) return false;
            pluginManifest.RequiresESearchVersion(out int pluginMajor, out int pluginMinor);
            Assembly asm = Assembly.GetExecutingAssembly();
            StringBuilder sb = new StringBuilder();

            int major = asm.GetName().Version.Major;
            int minor = asm.GetName().Version.Minor;

            if (pluginMajor > major) return false;
            if (pluginMinor > minor && pluginMajor == major) return false;
            return true;
        }

    }
}
