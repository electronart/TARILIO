using eSearch.Interop.AI;
using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
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

        public bool IsServerRunning => throw new NotImplementedException();

        public bool IsErrorState => throw new NotImplementedException();

        public IReadOnlyList<string> ConsoleOutputDisplayLines => throw new NotImplementedException();

        public IClientTransport? GetClientTransport()
        {
            throw new NotImplementedException();
        }

        public Task<bool> StartServer()
        {
            throw new NotImplementedException();
        }

        public Task<bool> StopServer()
        {
            throw new NotImplementedException();
        }
    }
}
