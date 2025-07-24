using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading.Tasks;

namespace PortableBuilder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string configuration = args[0];
            if (!configuration.Contains("PORTABLE"))
            {
                Console.Error.WriteLine("PortableBuilder was ran but a portable configuration was not selected");
                throw new Exception("PortableBuilder is only for Portable Builds");
            }


            List<FolderCopy> FoldersToCopy = new List<FolderCopy>
            {
                new FolderCopy(
                    "../eSearch_installer/Include Files/Stemming",
                    "./bin/Release - Portable/net8.0/win-x64/ProgramData/Stemming"
                ),
                new FolderCopy(
                    "../eSearch_installer/Include Files/Synonyms",
                    "./bin/Release - Portable/net8.0/win-x64/ProgramData/Synonyms"
                ),
                new FolderCopy(
                   "./i18n",
                   "./bin/Release - Portable/net8.0/win-x64/ProgramData/i18n"
                ),
                new FolderCopy(
                    "../eSearch_installer/Include Files/Stop word files",
                    "./bin/Release - Portable/net8.0/win-x64/ProgramData/Stop"
                ),
                new FolderCopy(
                    "../eSearch_installer/Include Files/Portable Files",
                    "./bin/Release - Portable/TempDir"
                ),
                new FolderCopy(
                    "./bin/Release - Portable/net8.0/win-x64/",
                    "./bin/Release - Portable/TempDir/eSearchPortable"
                ),                
            };


            Console.WriteLine("Postbuild running...");
            

            if (args.Length > 0 && args[0].Contains("STANDALONE"))
            {
                Console.WriteLine("This is a portable build.");

                string version = GetAssemblyVersion2();
                Console.WriteLine("Version Info of eSearch: " + version);
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

                if (Directory.Exists("./bin/Release - Portable/net8.0/win-x64/ProgramData/Indexes"))
                {
                    Directory.Delete("./bin/Release - Portable/net8.0/win-x64/ProgramData/Indexes", true);
                }
                if (File.Exists("./bin/Release - Portable/net8.0/win-x64/ProgramData/Config.json"))
                {
                    File.Delete("./bin/Release - Portable/net8.0/win-x64/ProgramData/Config.json");
                }


                Console.WriteLine("Copying Complete.");
                Console.WriteLine("Packaging Portable Version");

                string SourceDir = "./bin/Release - Portable/TempDir";
                if (!Directory.Exists("./bin/Portable Releases"))
                {
                    Directory.CreateDirectory("./bin/Portable Releases");
                }
                string targetZip = "./bin/Portable Releases/eSearch Portable " + version + ".zip";
                ZipFile.CreateFromDirectory(SourceDir, targetZip);
                Console.WriteLine("Zip saved to " + targetZip);

            } else
            {
                Console.WriteLine("This is a installer build.");
            }
        }


        public static string GetAssemblyVersion2()
        {
            try
            {
                string filePath = "./bin/Release - Portable/net8.0/win-x64/eSearch.dll";
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
