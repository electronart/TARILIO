using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Text;
using System.Diagnostics;

namespace Installer_Helper
{
    public static class Utils
    {
        public static string GeteSearchVersion(string filePath)
        {
            AssemblyName assemblyName = AssemblyName.GetAssemblyName(filePath);
            Version version = assemblyName.Version;

            StringBuilder sb = new StringBuilder();
            sb.Append(version.Major).Append(".");
            sb.Append(version.Minor).Append(".");
            sb.Append(version.Build.ToString("0"));
            sb.Append(" (");
            sb.Append(version.Revision);
            sb.Append(")");
            string versionString = sb.ToString();
            return versionString;
        }

        public static void SetMSIProductVersion(string msiPath, string newVersion)
        {
                var installer = MsiLib.Installer.Open(msiPath, 1);
                installer.SetProperty("ProductVersion", newVersion);
                installer.SetProperty("ProductCode", Guid.NewGuid().ToString());
                installer.SetProperty("Publisher", "ElectronArt Design Ltd"); // Shows in Add / Remove Programs.
                installer.Save();
        }

        public static void SignMSI(string msi_path, string configuration_name)
        {
            try
            {
                // Path to signtool.exe
                string signToolPath = @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe";

                // Command-line arguments for signtool
                string arguments = $"sign /a /d \"{configuration_name}\" /n \"ELECTRONART DESIGN LIMITED\" /fd SHA256 /tr http://rfc3161timestamp.globalsign.com/advanced /td SHA256 \"{msi_path}\"";

                // Configure the process start info
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = signToolPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Start the process
                using (Process process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    process.Start();

                    // Read output and error streams
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    // Check the exit code
                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine("File signed successfully.");
                        Console.WriteLine(output);
                    }
                    else
                    {
                        Console.WriteLine("Error signing file:");
                        Console.WriteLine(error);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
