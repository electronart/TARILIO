using Installer_Helper;
using System.IO.Compression;

Console.WriteLine("### Installer Helper ###");

/**
 * The installer helper will give the MSI files the correct version, zip them up and also sign release builds.
 */
string configuration = args[0];
// TODO I was having trouble passing the soution path from installer project post build command line
// For now we're using ESEARCH_OS_SOLUTION_PATH environment variable.
string solutionDir = Environment.GetEnvironmentVariable("ESEARCH_OS_SOLUTION_PATH") ?? string.Empty;
if (string.IsNullOrEmpty(solutionDir))
{
    throw new Exception("ERROR: ESEARCH_OS_SOLUTION_PATH environment variable is not set");
}
// string installer_dir  = args[1];
// string solutionDir   = Path.GetDirectoryName(installer_dir);
string msi_file      = args[1];

Console.WriteLine("Installer Helper Supplied Arguments:");
Console.WriteLine("configuration:" + configuration);
Console.WriteLine("solutionDir: " + solutionDir);
Console.WriteLine("msi_file: " + msi_file);

if (configuration == null) throw new ArgumentNullException(nameof(configuration));
if (solutionDir == null) throw new ArgumentNullException(nameof(solutionDir));

if (configuration.Contains("PORTABLE")) return; // portable builds shouldn't run this at all.



string dll_path = Path.Combine([solutionDir,"eSearch","bin",configuration,"net8.0","win-x64","eSearch.dll"]);
if (!File.Exists(dll_path)) throw new FileNotFoundException(dll_path);
if (!File.Exists(msi_file)) throw new FileNotFoundException(msi_file);



string dll_version = Utils.GeteSearchVersion(dll_path);
string dll_version_msi_version = Utils.GeteSearchVersionMSIFriendly(dll_path);

Console.WriteLine($"This is a {configuration} Build - The version will be {dll_version}...");

Utils.SetMSIProductVersion(msi_file, dll_version_msi_version);

#region We no longer use the eSearch name... MSI File should either be TARILIO or TARILIO PRO
string branding = configuration;
branding = branding.Replace("TARILIO", "TARILIO-PRO");
branding = branding.Replace("eSearch", "TARILIO");
#endregion

string msi_file_name = branding.Replace(" ", "-") + "-" + dll_version.Replace(" ", "-").Replace("(", "").Replace(")", "").Replace(".","-") + ".msi";

string new_msi_file_path = Path.Combine( Path.GetDirectoryName(msi_file), msi_file_name);
new_msi_file_path.Replace(" ", "-");
new_msi_file_path.Replace("(", "-");
File.Copy(msi_file, new_msi_file_path);

if (configuration.Contains("RELEASE"))
{
    Console.WriteLine("Release build has been selected - Follow instructions to sign build when prompted...");
    Utils.SignMSI(new_msi_file_path, configuration);
}

// Finally, create a zip file.

string zip_path = Path.Combine(Path.GetDirectoryName(msi_file), Path.GetFileNameWithoutExtension(new_msi_file_path) + ".zip");
Console.WriteLine($"Compressing {zip_path}");
using (ZipArchive zip = ZipFile.Open(zip_path, ZipArchiveMode.Create))
{
    zip.CreateEntryFromFile(new_msi_file_path, Path.GetFileName(new_msi_file_path));
}

Console.WriteLine("Done!");
Utils.RevealInFolderCrossPlatform(zip_path);




