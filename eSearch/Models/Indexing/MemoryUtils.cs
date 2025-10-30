using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace eSearch.Models.Indexing
{
    public class MemoryUtils
    {
        /// <summary>
        /// Gets a recommended value for RAMBufferSizeMB based on available free physical memory.
        /// The value is clamped between 128 MB and 4096 MB.
        /// Returns a default of 256 MB if unable to determine free memory.
        /// </summary>
        /// <returns>The recommended RAMBufferSizeMB.</returns>
        public static double GetRecommendedRAMBufferSizeMB()
        {
            long freeBytes = GetFreePhysicalMemoryBytes();
            if (freeBytes == -1)
            {
                return 256; // Default value if unable to retrieve free memory
            }

            double freeMB = freeBytes / (1024.0 * 1024);
            double suggested = freeMB * 0.25; // Approx 25% of free memory.
            return Math.Clamp(suggested, 128, 4096);
        }

        /// <summary>
        /// Retrieves the free physical memory in bytes.
        /// Returns -1 if unable to retrieve the value.
        /// </summary>
        /// <returns>Free physical memory in bytes, or -1 on failure.</returns>
        private static long GetFreePhysicalMemoryBytes()
        {
            if (OperatingSystem.IsWindows())
            {
                return GetWindowsFreeMemoryBytes();
            }
            else if (OperatingSystem.IsLinux())
            {
                return GetLinuxFreeMemoryBytes();
            }
            else if (OperatingSystem.IsMacOS())
            {
                return GetMacOSFreeMemoryBytes();
            }

            return -1; // Unsupported platform
        }

        private static long GetWindowsFreeMemoryBytes()
        {
            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "OS get FreePhysicalMemory /Value",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(info))
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    var parts = output.Split('=', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        long freeKB = long.Parse(parts[1]);
                        return freeKB * 1024;
                    }
                }
            }
            catch
            {
                // Ignore exceptions, return -1
            }

            return -1;
        }

        private static long GetLinuxFreeMemoryBytes()
        {
            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"free -m\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(info))
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();
                    var lines = output.Split('\n');
                    if (lines.Length >= 2)
                    {
                        var memory = lines[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (memory.Length >= 4)
                        {
                            double freeMB = double.Parse(memory[3]);
                            return (long)(freeMB * 1024 * 1024);
                        }
                    }
                }
            }
            catch
            {
                // Ignore exceptions, return -1
            }

            return -1;
        }

        private static long GetMacOSFreeMemoryBytes()
        {
            try
            {
                // Get page size
                string pageSizeStr = RunCommand("sysctl", "-n hw.pagesize").Trim();
                long pageSize = long.Parse(pageSizeStr);

                // Get vm_stat output
                string vmStat = RunCommand("vm_stat", "");

                var lines = vmStat.Split('\n');
                long pagesFree = 0;
                long pagesInactive = 0;

                foreach (var line in lines)
                {
                    if (line.StartsWith("Pages free:", StringComparison.OrdinalIgnoreCase))
                    {
                        pagesFree = ParsePages(line);
                    }
                    else if (line.StartsWith("Pages inactive:", StringComparison.OrdinalIgnoreCase))
                    {
                        pagesInactive = ParsePages(line);
                    }
                }

                long totalFreePages = pagesFree + pagesInactive;
                return totalFreePages * pageSize;
            }
            catch
            {
                // Ignore exceptions, return -1
            }

            return -1;
        }

        private static string RunCommand(string command, string args)
        {
            var info = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(info))
            {
                process.WaitForExit();
                return process.StandardOutput.ReadToEnd();
            }
        }

        private static long ParsePages(string line)
        {
            // Extract the number after ':' and remove trailing '.'
            var parts = line.Split(':', StringSplitOptions.RemoveEmptyEntries)[1].Trim().TrimEnd('.');
            return long.Parse(parts);
        }
    }
}