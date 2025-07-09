using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using eSearch.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Reflection;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Models.Documents
{
    public static class TikaServer
    {
        static Process tikaServerProcess = null;
        static int port = 9998;
        static bool hasAlreadyDetectedJava = false;

        public static bool TryExtractDocumentToHTML(string filePath, out string extractedHTML)
        {
            try
            {
                EnsureRunning();

                string url = "http://localhost:" + port + "/tika";

                using (var client = new HttpClient())
                {
                    using (var fileContent = new StreamContent(new FileStream(filePath, FileMode.Open)))
                    {
                        using (var httpReq = new HttpRequestMessage(HttpMethod.Put, url))
                        {
                            httpReq.Headers.Add("Accept", "text/html");
                            httpReq.Content = fileContent;
                            using (var response = client.SendAsync(httpReq).Result)
                            {
                                int statusCode = (int)response.StatusCode;
                                //Debug.WriteLine("Tika status code " + statusCode);
                                if (statusCode >= 200 && statusCode < 300)
                                {
                                    string content = response.Content.ReadAsStringAsync().Result;
                                    //Debug.WriteLine("Tika response " + content);
                                    extractedHTML = content;
                                    return true;
                                }
                                else
                                {
                                    extractedHTML = "Bad status code from tika server " + statusCode;
                                    extractedHTML += response.Content.ReadAsStringAsync().Result;
                                    return false;
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex)
            {
                extractedHTML = "An exception occurred. " + ex.ToString();
                return false;
            }
        }

        private static string getStartArgs()
        {
            string jarFileName = "tika-server-standard-2.9.0.jar";
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string jarPath = Path.Combine(exePath, jarFileName);
#if DEBUG
            return "-jar \"" + jarPath + "\" --port=" + port; // + " -s";
#else
            return "-jar \"" + jarPath + "\" --port=" + port + "";
#endif
        }

        public static void EnsureRunning()
        {
            if (!hasAlreadyDetectedJava)
            {
                if (!Utils.IsJavaInstalledAndCorrectVersion(out string errorTitle, out string errorMsg))
                {
                    TaskDialogWindow.OKDialog(errorTitle, errorMsg, Program.GetMainWindow(), S.Get("Close"));
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
                    {
                        desktopApp.Shutdown();
                    }
                } else
                {
                    hasAlreadyDetectedJava = true; // Prevent the app continuously checking java is installed, just once the first tika server is requested.
                }
            }

            if (tikaServerProcess == null)
            {
                DetectExistingTika(); // In case it is already running from another process.
            }

            if (tikaServerProcess == null)
            {

                string javaPath = Utils.GetJavaExePath();


                string arguments = getStartArgs();
                tikaServerProcess = new System.Diagnostics.Process();
                tikaServerProcess.StartInfo.FileName = javaPath;
                tikaServerProcess.StartInfo.Arguments = arguments;
                tikaServerProcess.StartInfo.UseShellExecute = false;
                tikaServerProcess.StartInfo.CreateNoWindow = false;
                tikaServerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                tikaServerProcess.StartInfo.CreateNoWindow = true;
                tikaServerProcess.StartInfo.RedirectStandardOutput = true;
                //launchJar.StartInfo.RedirectStandardOutput = true;
                //launchJar.StartInfo.CreateNoWindow = true;
                //launchJar.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                tikaServerProcess.OutputDataReceived += TikaServerProcess_OutputDataReceived;
                tikaServerProcess.ErrorDataReceived += TikaServerProcess_ErrorDataReceived;
                tikaServerProcess.Exited += TikaServerProcess_Exited;
                tikaServerProcess.Start();
            }
        }

        private static void TikaServerProcess_Exited(object? sender, EventArgs e)
        {
            Debug.WriteLine("???");
        }

        private static void DetectExistingTika()
        {
            var processes = Process.GetProcessesByName("java");
            foreach (var process in processes)
            {
                
                try
                {
                    var cmdLine = GetCommandLine(process);
                    if (cmdLine != null && cmdLine.Contains("tika-server-standard"))
                    {
                        tikaServerProcess = process; // Found tika already running.
                        return;
                    }
                }
                catch { 
                    // Just swallow these exceptions.
                }
            }
        }

        // Define an extension method for type System.Process that returns the command 
        // line via WMI.
        private static string GetCommandLine(this Process process)
        {
            string cmdLine = null;
            using (var searcher = new ManagementObjectSearcher(
              $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
            {
                // By definition, the query returns at most 1 match, because the process 
                // is looked up by ID (which is unique by definition).
                using (var matchEnum = searcher.Get().GetEnumerator())
                {
                    if (matchEnum.MoveNext()) // Move to the 1st item.
                    {
                        cmdLine = matchEnum.Current["CommandLine"]?.ToString();
                    }
                }
            }
            if (cmdLine == null)
            {
                // Not having found a command line implies 1 of 2 exceptions, which the
                // WMI query masked:
                // An "Access denied" exception due to lack of privileges.
                // A "Cannot process request because the process (<pid>) has exited."
                // exception due to the process having terminated.
                // We provoke the same exception again simply by accessing process.MainModule.
                var dummy = process.MainModule; // Provoke exception.
            }
            return cmdLine;
        }

        private static void TikaServerProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine(e.Data);
        }

        private static void TikaServerProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine(e.Data);
        }

        public static void StopServer()
        {
            if (tikaServerProcess != null)
            {
                try
                {
                    tikaServerProcess.Kill();
                } catch (Exception ex)
                {
                    Debug.WriteLine("Failed to stop Tika Server - An exception occurred: " + ex.ToString());
                }
            }
        }
    }
}
