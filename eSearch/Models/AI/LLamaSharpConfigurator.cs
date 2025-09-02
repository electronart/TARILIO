using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management; // Add reference to System.Management
using System.Runtime.InteropServices;
using LLama.Native;
using LLama.Common;
using eSearch;
using eSearch.Interop;

public class LLamaBackendConfigurator
{
    // Assume DLLs are bundled in this subfolder relative to exe
    private const string NativeLibsFolder = "native_libs";

    // DLL names from LLamaSharp packages (adjust if different)
    private const string CpuDll = "llama.dll";
    private const string Cuda11Dll = "llama-cuda-11.dll"; // Actual name might be llama.dll in CUDA11 package; check NuGet
    private const string Cuda12Dll = "llama-cuda-12.dll"; // Similarly
    private const string VulkanDll = "llama-vulkan.dll";
    private const string OpenClDll = "llama-opencl.dll";

    // WMI class for GPUs
    private const string WmiVideoController = "Win32_VideoController";

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    /// <summary>
    /// Represents detected GPU information.
    /// </summary>
    public class GPUInfo
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public string Vendor { get; set; } // e.g., "NVIDIA", "AMD", "Intel"
        public bool IsIntegrated { get; set; } // True if likely integrated (e.g., Intel HD)
    }

    /// <summary>
    /// Detects available GPUs on the system using WMI.
    /// Filters out software adapters and prioritizes discrete GPUs.
    /// </summary>
    /// <returns>List of detected GPUs.</returns>
    public static List<GPUInfo> GetAvailableGPUs()
    {
        var gpus = new List<GPUInfo>();
        int index = 0;

        using (var searcher = new ManagementObjectSearcher($"SELECT * FROM {WmiVideoController}"))
        {
            foreach (ManagementObject obj in searcher.Get())
            {
                string name = obj["Name"]?.ToString() ?? "";
                string adapterType = obj["AdapterCompatibility"]?.ToString() ?? "";
                string description = obj["Description"]?.ToString() ?? "";

                // Skip software/virtual adapters
                if (name.Contains("Microsoft") || name.Contains("Virtual") || string.IsNullOrEmpty(name))
                    continue;

                string vendor = DetermineVendor(name + " " + adapterType);
                bool isIntegrated = IsLikelyIntegrated(name, vendor);

                gpus.Add(new GPUInfo
                {
                    Index = index++,
                    Name = name,
                    Vendor = vendor,
                    IsIntegrated = isIntegrated
                });
            }
        }

        // Sort to prefer discrete over integrated
        return gpus.OrderBy(g => g.IsIntegrated ? 1 : 0).ToList();
    }

    private static string DetermineVendor(string info)
    {
        info = info.ToLower();
        if (info.Contains("nvidia")) return "NVIDIA";
        if (info.Contains("amd") || info.Contains("ati") || info.Contains("radeon")) return "AMD";
        if (info.Contains("intel")) return "Intel";
        return "Unknown";
    }

    private static bool IsLikelyIntegrated(string name, string vendor)
    {
        name = name.ToLower();
        return vendor == "Intel" || name.Contains("integrated") || name.Contains("hd graphics") || name.Contains("uhd");
    }

    /// <summary>
    /// Configures the LLamaSharp backend based on detected hardware.
    /// Prioritizes discrete GPUs if available.
    /// </summary>
    /// <param name="gpuIndex">Optional: Index of GPU to use (from GetAvailableGPUs). If null, selects best (discrete NVIDIA preferred).</param>
    /// <param name="promptCallback">Optional: Callback to prompt user (e.g., show message box). Arg is message string.</param>
    /// <returns>True if configuration succeeded (including fallback), false if critical failure (e.g., no CPU backend).</returns>
    public static bool ConfigureBackend(int? gpuIndex = null, Action<string> promptCallback = null, Microsoft.Extensions.Logging.ILogger logger = null)
    {
        // Enable logging for diagnostics
        if (logger != null) NativeLibraryConfig.All.WithLogCallback(logger);

        var gpus = GetAvailableGPUs();
        if (gpus.Count == 0)
        {
            // No GPUs, fallback to CPU
            return SetBackend(CpuDll, null);
        }

        GPUInfo selectedGpu = SelectGpu(gpus, gpuIndex);
        if (selectedGpu == null)
        {
            promptCallback?.Invoke("Invalid GPU index specified.");
            return false;
        }

        // For multi-GPU, set environment for selection (CUDA-specific; Vulkan/OpenCL may need different handling)
        if (gpuIndex.HasValue && selectedGpu.Vendor == "NVIDIA")
        {
            Environment.SetEnvironmentVariable("CUDA_VISIBLE_DEVICES", gpuIndex.Value.ToString());
        }

        string dllPath = null;
        string fallbackReason = null;

        if (selectedGpu.Vendor == "NVIDIA")
        {
            // Check CUDA installation and version
            var cudaInfo = GetCudaVersion();
            if (cudaInfo.Version == null)
            {
                fallbackReason = "CUDA Toolkit not detected. For NVIDIA GPU acceleration, install CUDA Toolkit from https://developer.nvidia.com/cuda-downloads.";
                promptCallback?.Invoke(fallbackReason);
            }
            else
            {
                // Select CUDA backend based on version
                string cudaDll = cudaInfo.MajorVersion >= 12 ? Cuda12Dll : Cuda11Dll;
                dllPath = GetDllPath(cudaDll);

                // Dry run to check compatibility (e.g., old card with new CUDA)
                NativeLibraryConfig.All.WithLibrary(dllPath, null);
                var success = NativeLibraryConfig.All.DryRun(out var loadedLib, out var ignored);
                if (!success)
                {
                    // Try the other CUDA version as fallback
                    string altCudaDll = cudaInfo.MajorVersion >= 12 ? Cuda11Dll : Cuda12Dll;
                    dllPath = GetDllPath(altCudaDll);
                    NativeLibraryConfig.All.WithLibrary(dllPath, null);
                    var success2 = NativeLibraryConfig.All.DryRun(out var loadedLib2, out var ignored2);
                    if (!success2)
                    {
                        fallbackReason = $"CUDA backend failed to load ({loadedLib2}). Possible incompatible GPU or driver. Falling back.";
                        dllPath = null;
                    }
                }
            }
        }

        if (dllPath == null && fallbackReason != null)
        {
            // Try Vulkan as fallback for NVIDIA/AMD/Intel
            dllPath = GetDllPath(VulkanDll);
            NativeLibraryConfig.All.WithLibrary(dllPath, null);
            var success = NativeLibraryConfig.All.DryRun(out var _, out var __);
            if (!success)
            {
                // Then OpenCL
                dllPath = GetDllPath(OpenClDll);
                NativeLibraryConfig.All.WithLibrary(dllPath, null);
                var success2 = NativeLibraryConfig.All.DryRun(out var ___, out var ____);
                if (!success2)
                {
                    fallbackReason += "\nVulkan and OpenCL also failed. Falling back to CPU.";
                }
            }
        }

        // Final fallback to CPU if needed
        if (dllPath == null)
        {
            return SetBackend(CpuDll, fallbackReason, promptCallback);
        }

        // Set the selected backend
        return SetBackend(dllPath, fallbackReason, promptCallback);
    }

    private static GPUInfo SelectGpu(List<GPUInfo> gpus, int? gpuIndex)
    {
        if (gpuIndex.HasValue)
        {
            return gpus.FirstOrDefault(g => g.Index == gpuIndex.Value);
        }

        // Auto-select: Prefer discrete NVIDIA, then AMD, then Intel
        return gpus.FirstOrDefault(g => !g.IsIntegrated && g.Vendor == "NVIDIA") ??
               gpus.FirstOrDefault(g => !g.IsIntegrated && g.Vendor == "AMD") ??
               gpus.FirstOrDefault(g => !g.IsIntegrated && g.Vendor == "Intel") ??
               gpus.FirstOrDefault(); // Any
    }

    private static (string? Version, int MajorVersion) GetCudaVersion()
    {
        try
        {
            // Run nvcc --version
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "nvcc.exe",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Parse e.g., "release 12.1"
            var releaseLine = output.Split('\n').FirstOrDefault(l => l.Contains("release"));
            if (releaseLine != null)
            {
                var parts = releaseLine.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                var versionIndex = Array.IndexOf(parts, "release") + 1;
                if (versionIndex < parts.Length)
                {
                    string version = parts[versionIndex];
                    if (int.TryParse(version.Split('.')[0], out int major))
                    {
                        return (version, major);
                    }
                }
            }
        }
        catch
        {
            // nvcc not found or error
        }

        // Fallback: Check if CUDA DLL loads
        IntPtr handle = LoadLibrary("nvcuda.dll");
        if (handle != IntPtr.Zero)
        {
            // CUDA present, but version unknown; assume 12
            return ("Unknown (detected)", 12);
        }

        return (null, 0);
    }

    private static string GetDllPath(string dllName)
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, NativeLibsFolder, dllName);
    }

    private static bool SetBackend(string dllPathOrName, string fallbackReason = null, Action<string> promptCallback = null)
    {
        string fullPath = Path.IsPathRooted(dllPathOrName) ? dllPathOrName : GetDllPath(dllPathOrName);
        NativeLibraryConfig.All
            .WithSearchDirectory(Path.GetDirectoryName(fullPath))
            .WithLibrary(fullPath, null)
            .WithAutoFallback(true); // Fallback if load fails

        var success = NativeLibraryConfig.All.DryRun(out var loadedLib, out var ignored);
        if (!success)
        {
            promptCallback?.Invoke($"Failed to load backend: {loadedLib}. {fallbackReason ?? ""}");
            return false;
        }

        if (!string.IsNullOrEmpty(fallbackReason))
        {
            promptCallback?.Invoke(fallbackReason);
        }

        return true;
    }
}

// Usage example:
// var gpus = LLamaBackendConfigurator.GetAvailableGPUs();
// foreach (var gpu in gpus) { Console.WriteLine($"{gpu.Index}: {gpu.Vendor} - {gpu.Name} (Integrated: {gpu.IsIntegrated})"); }

// bool success = LLamaBackendConfigurator.ConfigureBackend(gpuIndex: 0, promptCallback: msg => Console.WriteLine("Prompt: " + msg));

// Then load model with GpuLayerCount > 0 for GPU offload