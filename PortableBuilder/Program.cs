using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading.Tasks;

namespace PortableBuilder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("## Portable Builder ##");
            string configuration = args[0];
            if (!configuration.Contains("PORTABLE"))
            {
                Console.Error.WriteLine("PortableBuilder was ran but a portable configuration was not selected");
                throw new Exception("PortableBuilder is only for Portable Builds");
            }


            List<FolderCopy> FoldersToCopy = new List<FolderCopy>
            {
                new FolderCopy(
                    "../eSearchInstaller/Include Files/Stemming",
                    $"./bin/{configuration}/net8.0/win-x64/ProgramData/Stemming"
                ),
                new FolderCopy(
                    "../eSearchInstaller/Include Files/Synonyms",
                    $"./bin/{configuration}/net8.0/win-x64/ProgramData/Synonyms"
                ),
                new FolderCopy(
                   "./i18n",
                   $"./bin/{configuration}/net8.0/win-x64/ProgramData/i18n"
                ),
                new FolderCopy(
                    "../eSearchInstaller/Include Files/Stop word files",
                    $"./bin/{configuration}/net8.0/win-x64/ProgramData/Stop"
                ),
                new FolderCopy(
                    "../eSearchInstaller/Include Files/Portable Files",
                    $"./bin/{configuration}/TempDir"
                ),
                new FolderCopy(
                    $"./bin/{configuration}/net8.0/win-x64/",
                    $"./bin/{configuration}/TempDir/eSearchPortable"
                ),                
            };

            Console.WriteLine("Deleting existing configuration and indexes...");

            if (Directory.Exists($"./bin/{configuration}/net8.0/win-x64/ProgramData/Indexes"))
            {
                Directory.Delete($"./bin/{configuration}/net8.0/win-x64/ProgramData/Indexes", true);
            }
            if (File.Exists($"./bin/{configuration}/net8.0/win-x64/ProgramData/Config.json"))
            {
                File.Delete($"./bin/{configuration}/net8.0/win-x64/ProgramData/Config.json");
            }


            string version = GetAssemblyVersion2(configuration);
            Console.WriteLine($"Version Info of {configuration}: " + version);
            Console.WriteLine("Current Working Directory:");
            Console.WriteLine(Directory.GetCurrentDirectory());
            Console.WriteLine("Arguments:");
            Console.WriteLine(string.Join(Environment.NewLine, args));


            Console.WriteLine("Copying Additional Directories...");
            foreach (var folderToCopy in FoldersToCopy)
            {
                if (!Directory.Exists(folderToCopy.SourceDirectory))
                {
                    throw new Exception("Source folder does not exist: " + folderToCopy.SourceDirectory);
                }

                if (Directory.Exists(folderToCopy.TargetDirectory))
                {
                    Directory.Delete(folderToCopy.TargetDirectory, true);
                }
                Directory.CreateDirectory(folderToCopy.TargetDirectory);
                Console.WriteLine("Copy: " + folderToCopy.SourceDirectory + " -> " + folderToCopy.TargetDirectory);
                CopyDirectory(folderToCopy.SourceDirectory, folderToCopy.TargetDirectory, true);
            }

                


            Console.WriteLine("Copying Complete.");
            Console.WriteLine("Packaging Portable Version");

            string SourceDir = $"./bin/{configuration}/TempDir";
            string TargetDir = $"./bin/{configuration} Releases";
            if (!Directory.Exists(TargetDir))
            {
                Directory.CreateDirectory(TargetDir);
            }

            #region brand workaround
            // TODO Currently our build configurations don't match our product brand names..
            string zipName = configuration;
            configuration.Replace("eSearch PORTABLE", "TARILIO PORTABLE");

            #endregion

            string targetZip = Path.Combine(TargetDir, $"{configuration}-{version.Replace(".","-").Replace("(","").Replace(")","").Replace(" ","-")}.zip");
            ZipFile.CreateFromDirectory(SourceDir, targetZip);
            Console.WriteLine("Zip saved to " + targetZip);
            RevealInFolderCrossPlatform(targetZip);
        }


        public static string GetAssemblyVersion2(string configuration)
        {
            try
            {
                string filePath = $"./bin/{configuration}/net8.0/win-x64/eSearch.dll";
                Console.WriteLine(Path.GetFullPath(filePath));
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var peReader = new PEReader(stream))
                    {
                        if (!peReader.HasMetadata)
                        {
                            throw new InvalidOperationException("The file does not contain metadata.");
                        }

                        var metadataReader = peReader.GetMetadataReader();
                        var assemblyDefinition = metadataReader.GetAssemblyDefinition();
                        var version = assemblyDefinition.Version;

                        StringBuilder sb = new StringBuilder();

                        sb.Append(version.Major).Append(".");
                        sb.Append(version.Minor).Append(".");
                        sb.Append(version.Build.ToString("0"));
                        sb.Append(" (");
                        sb.Append(version.Revision);
                        sb.Append(")");
                        return sb.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading assembly version: {ex.Message}");
                return null;
            }

            return null;
        }

        /// <summary>
        /// Cross platform method of revealing a file on the filesystem.
        /// 
        /// </summary>
        /// <param name="path"></param>
        public static async void RevealInFolderCrossPlatform(string path)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process fileOpener = new Process();
                    fileOpener.StartInfo.FileName = "explorer";
                    fileOpener.StartInfo.Arguments = "/select," + path + "\"";
                    fileOpener.Start();
                    fileOpener.WaitForExit();
                    return;
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process fileOpener = new Process();
                    fileOpener.StartInfo.FileName = "explorer";
                    fileOpener.StartInfo.Arguments = "-R " + path;
                    fileOpener.Start();
                    fileOpener.WaitForExit();
                    return;
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process dbusShowItemsProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dbus-send",
                            Arguments = "--print-reply --dest=org.freedesktop.FileManager1 /org/freedesktop/FileManager1 org.freedesktop.FileManager1.ShowItems array:string:\"file://" + path + "\" string:\"\"",
                            UseShellExecute = true
                        }
                    };
                    dbusShowItemsProcess.Start();
                    dbusShowItemsProcess.WaitForExit();

                    if (dbusShowItemsProcess.ExitCode == 0)
                    {
                        // The dbus invocation can fail for a variety of reasons:
                        // - dbus is not available
                        // - no programs implement the service,
                        // - ...
                        return;
                    }
                }

                Process folderOpener = new Process();
                folderOpener.StartInfo.FileName = Path.GetDirectoryName(path);
                folderOpener.StartInfo.UseShellExecute = true;
                folderOpener.Start();
                folderOpener.WaitForExit();

            }
            catch (Exception ex)
            {
                // TODO Error handling
                throw;
            }
        }

        //static string GetVersionStr()
        //{
        //    string exeFile = "./bin/Release - Portable/net8.0/win-x64/eSearch.dll";
        //    Console.WriteLine(Path.GetFullPath(exeFile));
        //    var asm = Assembly.LoadFile(Path.GetFullPath(exeFile));
        //    StringBuilder sb = new StringBuilder();

        //    sb.Append(asm.GetName().Version.Major).Append(".");
        //    sb.Append(asm.GetName().Version.Minor).Append(".");
        //    sb.Append(asm.GetName().Version.Build.ToString("0"));
        //    sb.Append(" (");
        //    sb.Append(asm.GetName().Version.Revision);
        //    sb.Append(")");
        //    return sb.ToString();
        //}

        static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }
}
