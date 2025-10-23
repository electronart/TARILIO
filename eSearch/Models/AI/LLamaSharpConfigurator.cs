using eSearch;
using LLama.Common;
using LLama.Native;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static java.security.cert.CertPathValidatorException;

// TODO Supports LLama only, NOT LLava despite method signatures.

public class LLamaBackendConfigurator
{
    // Base folder for native DLLs relative to executable
    private const string NativeLibsFolder = "native_libs";

    // Subfolders for each backend, containing llama.dll and other dependencies
    private const string CpuSubfolder = "llama-cpu-avx2";
    private const string Cuda11Subfolder = "llama-cuda11-win-x64";
    private const string Cuda12Subfolder = "llama-cuda12-win-x64";
    private const string VulkanSubfolder = "llama-vulkan-win-x64";
    //private const string OpenClSubfolder = "llama-opencl-win-x64";

    // Common DLL name used in all backend folders
    private const string LlamaDllName = "llama.dll";

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
    /// Configures backend with resets to avoid singleton state issues. Call this EARLY (e.g., in Program.Main before any LLamaSharp use).
    /// </summary>
    public static async Task<bool> ConfigureBackend2(int? gpuIndex = null, bool useLlava = false, Action<string> promptCallback = null, ILogger logger = null)
    {
        // Base config: Apply ONCE at start (before any DryRun/WithLibrary)
        NativeLibraryConfig.All
            .WithSearchDirectory(NativeLibsFolder)  // Search all subfolders
            .WithAutoFallback(true);  // Allow fallback if specific fails

        if (logger != null)
        {
            // Custom log callback for your ILogger
            NativeLibraryConfig.All.WithLogCallback((level, msg) =>
            {
                switch (level)
                {
                    case LLamaLogLevel.Error: logger.LogError(msg); break;
                    case LLamaLogLevel.Warning: logger.LogWarning(msg); break;
                    case LLamaLogLevel.Info: logger.LogInformation(msg); break;
                    case LLamaLogLevel.Debug: logger.LogDebug(msg); break;
                }
            });
        }

        NativeLibraryConfig.LLava.SkipCheck(true); // Bypass llava validation in dryrun
        List<GPUInfo> gpus = new List<GPUInfo>();
        int retries = 0;
    retryGetGPUs:
        try
        {
            gpus = GetAvailableGPUs();
        }
        catch (Exception ex)
        {
            ++retries;
            if (retries < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(retries * 30));
                goto retryGetGPUs;
            } else
            {
                throw new InvalidOperationException("Could not detect available GPU's");
            }
        }

        if (gpus.Count == 0)
        {
            return SetBackend(CpuSubfolder, null, promptCallback);
        }

        var selectedGpu = SelectGpu(gpus, gpuIndex);
        if (selectedGpu == null)
        {
            promptCallback?.Invoke("Invalid GPU index specified.");
            return false;
        }

        // Set CUDA_VISIBLE_DEVICES EARLY for multi-GPU (before any config)
        if (gpuIndex.HasValue && selectedGpu.Vendor == "NVIDIA")
        {
            Environment.SetEnvironmentVariable("CUDA_VISIBLE_DEVICES", gpuIndex.Value.ToString());
        }

        string reason = string.Empty;
        // Check/prep CUDA env
        var cudaInfo = GetCudaVersion();
        if (selectedGpu.Vendor == "NVIDIA" && cudaInfo.Version != null)
        {
            // Prompt for common missing deps
            var cudaBinPath = Environment.GetEnvironmentVariable("CUDA_PATH");
            if (cudaBinPath == null || !Directory.Exists(cudaBinPath))
            {
                promptCallback?.Invoke("CUDA bin path invalid. Check that CUDA Toolkit is installed. Ensure that CUDA_PATH is set.");
                return SetBackend(CpuSubfolder, "CUDA env incomplete", promptCallback);
            }

            if (cudaInfo.MajorVersion >= 13)
            {
                promptCallback?.Invoke("eSearch is only compatible with CUDA Version 11 or 12. The installed version, " + cudaInfo.Version + " is not compatible.");
                return SetBackend(CpuSubfolder, "CUDA Version incompatible", promptCallback);
            }

            // Try CUDA12 first
            if (TryBackendWithReset(Cuda12Subfolder, useLlava, promptCallback, logger, out var cuda12Success, out reason))
            {
                return true;
            }

            // Fallback to CUDA11 
            if (TryBackendWithReset(Cuda11Subfolder, useLlava, promptCallback, logger, out var cuda11Success, out var reason2))
            {
                return true;
            }

            reason = $"CUDA backends failed: {reason} {reason2}. Falling back to non-CUDA.";
            promptCallback?.Invoke(reason);
        }
        else if (selectedGpu.Vendor == "NVIDIA" && cudaInfo.Version == null)
        {
            promptCallback?.Invoke("CUDA Toolkit not detected (run 'nvcc --version'). Install from https://developer.nvidia.com/cuda-downloads.");
            return SetBackend(CpuSubfolder, "No CUDA", promptCallback);
        }

        // Non-NVIDIA or CUDA fallback: Vulkan -> OpenCL -> CPU
        bool hasVulkan = await VulkanChecker.IsVulkanAvailableAsync(TimeSpan.FromSeconds(5));
        string? reason3 = null;

        if (hasVulkan && TryBackendWithReset(VulkanSubfolder, useLlava, promptCallback, logger, out var vulkanSuccess, out var _reason3))
        {
            reason3 = _reason3;
            return true;
        }

        // Final CPU fallback
        var cpuReason = $"{reason} {reason3 ?? "Vulkan Support not detected"} Falling back to CPU.";
        promptCallback?.Invoke(cpuReason);
        return SetBackend(CpuSubfolder, cpuReason, promptCallback);
    }



    /// <summary>
    /// Tries a backend with config reset to avoid singleton side effects. Returns true if DryRun succeeds.
    /// </summary>
    private static bool TryBackendWithReset(string subfolder, bool useLlava, Action<string> promptCallback, ILogger? logger, out bool success, out string reason)
    {
        var dllFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, NativeLibsFolder, subfolder);
        var dllPath = Path.Combine(dllFolder, "llama.dll");
        var cpuDllFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, NativeLibsFolder, CpuSubfolder); // ggml.dll in cuda seems to depend on ggml-cpu.dll
        var llavaPath = useLlava ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, NativeLibsFolder, subfolder, "llava.dll") : null;

        success = false;
        reason = null;

        // Reset: Re-apply base config to clear any prior state (no-op if already set, but forces fresh)
        NativeLibraryConfig.All
            .WithSearchDirectories(new string[] { dllFolder, cpuDllFolder})
            .WithLogCallback(logger)
            .WithCuda(subfolder.Contains("cuda"))
            .WithAutoFallback(false);  // Disable for this test to isolate

        NativeLibraryConfig.LLava.SkipCheck(true);

        

        if (!File.Exists(dllPath))
        {
            reason = $"DLL not found: {dllPath}";
            return false;
        }

        // Apply specific library for test
        NativeLibraryConfig.All.WithLibrary(dllPath, llavaPath);

        // DryRun: Test without committing

        var drySuccess = NativeLibraryConfig.LLama.DryRun(out var loadedLib); // TODO LLama only, NOT llava

        //var drySuccess = NativeLibraryConfig.All.DryRun(out var loadedLib, out var ignored); 
        if (!drySuccess)
        {
            reason = $"DryRun failed for {subfolder}: {loadedLib?.ToString() ?? "Unknown error"}. Check logs for details (e.g., cuBLAS missing).";
            // Reset again after failed DryRun
            NativeLibraryConfig.All.WithSearchDirectory(NativeLibsFolder).WithAutoFallback(true);
            return false;
        }
        Program.LLAMA_BACKEND = subfolder;
        success = true;
        return true;
        
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




    private static string GetDllPath(string backendSubfolder)
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, NativeLibsFolder, backendSubfolder, LlamaDllName);
    }

    private static bool SetBackend(string backendSubfolder, string? fallbackReason = null, Action<string>? promptCallback = null)
    {
        Program.LLAMA_BACKEND = backendSubfolder;
        string dllPath = GetDllPath(backendSubfolder);
        string searchPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, NativeLibsFolder, backendSubfolder);
        NativeLibraryConfig.All
            .WithSearchDirectory(searchPath)
            .WithLibrary(dllPath, null)
            .WithAutoFallback(true); // Fallback if load fails

        var success = NativeLibraryConfig.All.DryRun(out var loadedLib, out var ignored);
        if (!success)
        {
            promptCallback?.Invoke($"Failed to load backend from {backendSubfolder}: {loadedLib}. {fallbackReason ?? ""}");
            return false;
        }

        if (!string.IsNullOrEmpty(fallbackReason))
        {
            promptCallback?.Invoke(fallbackReason);
        }
        
        return true;
    }
}

public class VulkanChecker
{
    public static async Task<bool> IsVulkanAvailableAsync(TimeSpan timeout)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "vulkaninfo";
            process.StartInfo.Arguments = "--summary";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            // Wait for exit with timeout
            var exited = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds));

            if (!exited)
            {
                process.Kill();
                return false; // Timed out
            }

            if (process.ExitCode != 0)
            {
                // Error occurred (e.g., command not found or Vulkan not supported)
                return false;
            }

            string output = process.StandardOutput.ReadToEnd();
            return !string.IsNullOrEmpty(output) && output.Contains("deviceName"); // Basic validation for GPU presence
        }
        catch (Exception)
        {
            // Command not found or other issues
            return false;
        }
    }
}