using eSearch.Interop.AI;
using eSearch.Utils;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eSearch.Models.AI.MCP
{
    public class UserConfiguredMCPServer : IESearchMCPServer
    {
        public required string Json;

        private readonly List<string> _outputLines = new List<string>();
        private const int MaxLines = 100;

        private UserConfiguredMCPServerClientTransport? UserConfiguredMCPServerClientTransport;

        /// <summary>
        /// Performs some (very) basic validation to check the user has pasted in an mcpServers configuration json.
        /// If it validates, will return a userConfiguredMCPServer.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="userConfiguredMCPServer"></param>
        /// <returns></returns>
        public static bool TryGetValidUserConfiguredMCPServer(string json, out UserConfiguredMCPServer? userConfiguredMCPServer)
        {
            try
            {
                JObject config = JObject.Parse(json);
                if (!config.ContainsKey("mcpServers"))
                {
                    userConfiguredMCPServer = null;
                    return false;
                }
                if (config["mcpServers"] is JObject mcpServers)
                {
                    if (mcpServers.Properties().Count() > 0)
                    {
                        string name = mcpServers.Properties().First().Name;
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            // Consider this to be valid.
                            userConfiguredMCPServer = new UserConfiguredMCPServer { Json = json };
                            return true;
                        }
                    }
                }
                userConfiguredMCPServer = null;
                return false;
            }
            catch (JsonReaderException jrEx)
            {
                userConfiguredMCPServer = null;
                return false;
            }
        }

        public IClientTransport? GetClientTransport()
        {
            if (!IsServerRunning) return null;
            return new UserConfiguredMCPServerClientTransport(this);
        }

        public string DisplayName
        {
            get
            {
                try
                {
                    JObject config = JObject.Parse(Json);
                    if (config["mcpServers"] is JObject mcpServers)
                    {
                        var name = mcpServers.Properties().First().Name;
                        return name;
                    }
                }
                catch (Exception ex)
                {
                    // Just swallow this...
                    Debug.WriteLine(ex.ToString());
                }
                return string.Empty;
            }
        }

        public bool IsServerRunning
        {
            get
            {
                return UserConfiguredMCPServerClientTransport != null;
            }
        }

        public IReadOnlyList<string> ConsoleOutputDisplayLines => _outputLines;

        public bool IsErrorState
        {
            get
            {
               return 
                      IsServerRunning 
                      &&
                      ( 
                        UserConfiguredMCPServerClientTransport?   // Will be null when user stopped.
                            .DidProcessExit                       // Will return false when the process has exited.
                                ?? 
                        false
                      );                  
            }
        }

        public async Task<bool> StartServer()
        {
            try
            {
                _outputLines.Clear();
                if (IsServerRunning)
                {
                    ProcessOutputLine("Stopping Server...");
                    await StopServer();
                }

                ProcessOutputLine("Starting Server...");

                if (UserConfiguredMCPServerClientTransport == null)
                {
                    UserConfiguredMCPServerClientTransport = new UserConfiguredMCPServerClientTransport(this);
                    UserConfiguredMCPServerClientTransport.OnMCPServerLog += (sender, args) =>
                    {
                        ProcessOutputLine(args);
                    };
                }

                var res = await UserConfiguredMCPServerClientTransport.ConnectAsync();
                if (res == null) return false;
                return true;
            }
            catch (Exception ex)
            {
                _outputLines.Add(ex.ToString());
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }

        public async Task<bool> StopServer()
        {
            try
            {
                if (UserConfiguredMCPServerClientTransport != null)
                {
                    var res = await UserConfiguredMCPServerClientTransport.ShutdownProcess();
                    if (res)
                    {
                        UserConfiguredMCPServerClientTransport = null;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }



        private void ProcessOutputLine(string data)
        {
            // Handle carriage return for progress bar overwrites
            if (data.Contains("\r"))
            {
                var lastLine = data.Split('\r', StringSplitOptions.RemoveEmptyEntries).Last();
                if (_outputLines.Count > 0)
                {
                    _outputLines[_outputLines.Count - 1] = lastLine; // Overwrite last line
                }
                else
                {
                    _outputLines.Add(lastLine); // Add as new line if none exist
                }
            }
            else
            {
                _outputLines.Add(data); // Add new line
            }

            // Keep only the last 10 lines
            while (_outputLines.Count > MaxLines)
            {
                _outputLines.RemoveAt(0);
            }
        }
    }

    class UserConfiguredMCPServerClientTransport : IClientTransport
    {
        private UserConfiguredMCPServer _mcpServer;
        private ProcessTransport? _transport;

        public delegate void MCPServerLogEventHandler(bool isError, string message);

        public event MCPServerLogEventHandler? OnMCPServerLog;

        public bool DidProcessExit = false;

        private Process? MCPServerProcess
        {
            get
            {
                if (_mcpServerProcess == null)
                {
                    Process newProcess = BuildProcessFromConfig();
                    Process? existingProcess = ExistingProcessChecker.GetExistingProcess(newProcess);
                    if (existingProcess != null)
                    {
                        _mcpServerProcess = existingProcess;
                    }
                    else
                    {
                        _mcpServerProcess = newProcess;
                    }
                }
                return _mcpServerProcess;
            }
        }

        private Process? _mcpServerProcess = null;

        public async Task<bool> ShutdownProcess()
        {
            try
            {
                



                if (_transport != null)
                {
                    await _transport.StopAsync();
                }

                if (IsProcessRunning(_mcpServerProcess))
                {
                    // Attempt graceful shutdown (e.g., send Ctrl+C)
                    _mcpServerProcess?.StandardInput.WriteLine("\x03");
                    await Task.Delay(1000); // Wait for graceful exit
                    if (_mcpServerProcess?.HasExited == false)
                    {
                        _mcpServerProcess?.Kill();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
            finally
            {
                _mcpServerProcess = null;

            }
        }

        private static bool IsProcessRunning(Process? process)
        {
            if (process == null)
            {
                return false;
            }

            try
            {
                // Check if the process has not exited
                if (!process.HasExited)
                {
                    // Optionally, access the process ID to confirm it's still valid
                    _ = process.Id; // This will throw if the process is invalid
                    return true;
                }
                return false;
            }
            catch (InvalidOperationException)
            {
                // Thrown if the process was never started or has been disposed
                return false;
            }
            catch (Win32Exception)
            {
                // Thrown if the process cannot be accessed (e.g., due to permissions)
                return false;
            }
            catch (Exception)
            {
                // Handle any other unexpected errors
                return false;
            }
        }


        private Process BuildProcessFromConfig()
        {
            JObject jsonObj = JObject.Parse(_mcpServer.Json);
            JObject? mcpServers = jsonObj["mcpServers"].Value<JObject>();
            if (mcpServers == null) throw new InvalidOperationException("Invalid Config");

            var serverEntry = mcpServers.Properties().First();
            JObject? serverConfig = serverEntry.Value.Value<JObject>();
            if (serverConfig == null) throw new InvalidOperationException("Invalid Config");
            string? command = serverConfig["command"]?.Value<string>();
            JArray? argsArray = serverConfig["args"]?.Value<JArray>();
            JObject? envVars = serverConfig["env"]?.Value<JObject>();

            // Convert arguments to string array
            string?[]? args = argsArray?.Select(arg => arg.Value<string>()).ToArray() ?? null;

            UTF8Encoding noBomUTF8 = new(encoderShouldEmitUTF8Identifier: false);
            Process process = new Process();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                !string.Equals(Path.GetFileName(command), "cmd.exe", StringComparison.OrdinalIgnoreCase))
            {
                // On Windows, must wrap non-shell commands with cmd.exe /c {command} (for npx/uvicorn)
                // stdio transport will not work correctly if the command is not run within shell
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.ArgumentList.Add("/c");
                process.StartInfo.ArgumentList.Add(command ?? "MISSING_COMMAND");
            } else
            {
                process.StartInfo.FileName = command;
            }
            
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.StandardOutputEncoding = noBomUTF8;
            process.StartInfo.StandardInputEncoding = noBomUTF8;
            process.StartInfo.StandardErrorEncoding = noBomUTF8;
            // Set any command line arguments
            if (args != null)
            {
                foreach (string? arg in args)
                {
                    if (arg != null) process.StartInfo.ArgumentList.Add(arg);
                }
            }
            // Set environment variables if they exist
            if (envVars != null)
            {
                foreach (var envVar in envVars)
                {
                    if (envVar.Value != null) process.StartInfo.EnvironmentVariables[envVar.Key] = envVar.Value.Value<string>();
                }
            }
            //process.OutputDataReceived += (sender, args) =>
            //{
            //    OnMCPServerLog?.Invoke(false, args.Data ?? "");
            //};
            process.ErrorDataReceived += (sender, args) =>
            {
                OnMCPServerLog?.Invoke(true, args.Data ?? "");
            };
            process.Exited += (sender, args) =>
            {
                OnMCPServerLog?.Invoke(false, "Server Process Exited (Code " + process.ExitCode + ")");
                DidProcessExit = true;
            };
            return process;
        }

        public UserConfiguredMCPServerClientTransport(UserConfiguredMCPServer server)
        {
            _mcpServer = server;
        }

        public string Name => _mcpServer.DisplayName;


        public bool IsServerRunning
        {
            get
            {
                return IsProcessRunning(_mcpServerProcess);
            }
        }

        public async Task<ITransport> ConnectAsync(CancellationToken cancellationToken = default)
        {

            if (!IsServerRunning)
            {
                if (_transport != null) await _transport.StopAsync();
                _transport = null;
                if (MCPServerProcess != null)
                {
                    
                    _transport = new ProcessTransport(MCPServerProcess);
                    _transport.OutputDataReceived += (sender, output) =>
                    {
                        if (output != null) OnMCPServerLog?.Invoke(false, output);
                    };
                    await _transport.StartAsync(cancellationToken);
                    
                    MCPServerProcess.Start();
                    MCPServerProcess.BeginErrorReadLine();
                    MCPServerProcess.BeginOutputReadLine();
                }
            }
            if (MCPServerProcess == null) throw new NullReferenceException("MCPServerProcess should not be null");
            if (_transport == null)
            {
                _transport = new ProcessTransport(MCPServerProcess);
                await _transport.StartAsync(cancellationToken);
            }
            return _transport;

        }
    }
}