using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Utils
{
    public static class ExistingProcessChecker
    {
        /// <summary>
        /// Check if a Process with the same command and arguments as Process p is already running on the system.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Process? GetExistingProcess(Process p)
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                return GetExistingProcessWindows(p);
            }
            else
            {
                // TODO Support for other operating systems.
                return null;
            }
        }

        [SupportedOSPlatform("windows")]
        private static Process? GetExistingProcessWindows(Process newProcess)
            {
                // Get the command (FileName) and arguments from the new process
                string? newCommand = newProcess.StartInfo.FileName;
                string newArguments = string.Join(" ", newProcess.StartInfo.ArgumentList);
                string newCommandLine = $"\"{newCommand}\" {newArguments}".Trim();

                // Use WMI to query running processes
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process"))
                    {
                        foreach (ManagementObject process in searcher.Get())
                        {
                            try
                            {
                                string? executablePath = process["ExecutablePath"]?.ToString();
                                string? commandLine = process["CommandLine"]?.ToString();

                                // Check if both executable path and command line match
                                if (executablePath != null && commandLine != null &&
                                    executablePath.Equals(newCommand, StringComparison.OrdinalIgnoreCase) &&
                                    commandLine.Trim().Equals(newCommandLine, StringComparison.OrdinalIgnoreCase))
                                {
                                    // Get the ProcessId and find the corresponding Process object
                                    uint pid = (uint)process["ProcessId"];
                                    return Process.GetProcessById((int)pid);
                                }
                            }
                            catch
                            {
                                // Skip processes where we can't access properties due to permissions or other issues
                                continue;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle WMI query errors (e.g., insufficient permissions)
                    Console.WriteLine($"WMI query failed: {ex.Message}");
                    return null;
                }

                // No matching process found
                return null;
            }



}
}
