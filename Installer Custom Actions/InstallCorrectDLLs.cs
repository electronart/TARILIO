using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;

namespace DIEventSourceCreator
{
    // Disused


    //[RunInstaller(true)]
    //public class InstallCorrectDLLs : Installer
    //{
    //    public override void Install(System.Collections.IDictionary stateSaver)
    //    {
    //    //    base.Install(stateSaver);

    //    //    // Only run during actual installation
    //    //    if (Context != null && Context.Parameters.ContainsKey("assemblypath"))
    //    //    {
    //    //        try
    //    //        {
    //    //            // Get the installation directory (application folder)
    //    //            string installDir = Context.Parameters["assemblypath"];
    //    //            string appDir = Path.GetDirectoryName(installDir);

    //    //            // Define the overwrite_dlls folder path
    //    //            string overwriteDllsFolder = Path.Combine(appDir, "overwrite_dlls");

    //    //            // Check if the overwrite_dlls folder exists
    //    //            if (Directory.Exists(overwriteDllsFolder))
    //    //            {
    //    //                // Get all DLL files from the overwrite_dlls folder
    //    //                string[] dllFiles = Directory.GetFiles(overwriteDllsFolder, "*.dll", SearchOption.TopDirectoryOnly);

    //    //                foreach (string sourceDll in dllFiles)
    //    //                {
    //    //                    // Get the DLL file name
    //    //                    string dllName = Path.GetFileName(sourceDll);
    //    //                    // Construct the destination path in the application folder
    //    //                    string destDll = Path.Combine(appDir, dllName);

    //    //                    // Check if the DLL exists in the application folder
    //    //                    if (File.Exists(destDll))
    //    //                    {
    //    //                        // Overwrite the existing DLL
    //    //                        File.Copy(sourceDll, destDll, true);
    //    //                    }
    //    //                    else
    //    //                    {
    //    //                        // Copy the DLL if it doesn't exist
    //    //                        File.Copy(sourceDll, destDll);
    //    //                    }
    //    //                }

    //    //                // Optionally, delete the overwrite_dlls folder after copying
    //    //                Directory.Delete(overwriteDllsFolder, true);
    //    //            }
    //    //        }
    //    //        catch (Exception ex)
    //    //        {
    //    //            // Log the error or throw it to roll back the installation
    //    //            throw new InstallException("Failed to overwrite DLLs: " + ex.ToString());
    //    //        }
    //    //    }
    //    //}
    //}
}