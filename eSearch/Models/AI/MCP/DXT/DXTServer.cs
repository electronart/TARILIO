using eSearch.Interop.AI;
using ModelContextProtocol.Client;
using org.quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace eSearch.Models.AI.MCP.DXT
{
    public class DXTServer : IESearchMCPServer
    {

        private DXTManifest Manifest;
        private string   _displayName;
        private Process? _serverProcess;
        private StdioClientTransport _transport;
        private List<string> _consoleOutput = new List<string>();

        /// <summary>
        /// Load DXT Server from DXT File Extracted Contents
        /// </summary>
        /// <param name="extracted_folder">The folder the contents of the .DXT file were extracted to</param>
        public DXTServer(string extracted_folder)
        {
            string manifest_path = Path.Combine(extracted_folder, "manifest.json");
            if (!File.Exists(manifest_path))
            {
                throw new FileNotFoundException("manifest.json not found in extracted DXT directory", manifest_path);
            }
            Manifest = JsonSerializer.Deserialize<DXTManifest>(File.ReadAllText(manifest_path)) 
                        ?? throw new Exception("DXTManifest may not be null");
            DisplayName = Manifest.Name;

        }


        public string DisplayName { get; set; }

        public bool IsServerRunning => _serverProcess != null && !_serverProcess.HasExited;

        public bool IsErrorState =>    _serverProcess != null 
                                    && _serverProcess.HasExited
                                    && _serverProcess.ExitCode != 0;

        public IReadOnlyList<string> ConsoleOutputDisplayLines => throw new NotImplementedException();

        public IClientTransport? GetClientTransport()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> StartServer()
        {
            if (IsServerRunning) return true;
            if (_serverProcess != null)
            {
                _serverProcess.Dispose();
                _serverProcess = null;
                _transport = null;
                _consoleOutput.Clear();
            }
            var cmd = Manifest.Server.McpConfig.Command;
            var args = Manifest.Server.McpConfig.Args;
            var env = Manifest.Server.McpConfig.Env;


        }

        public Task<bool> StopServer()
        {
            throw new NotImplementedException();
        }
    }
}
