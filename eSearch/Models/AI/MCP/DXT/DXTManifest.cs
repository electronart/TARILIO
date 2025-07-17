using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.AI.MCP.DXT
{

    // Based on https://github.com/anthropics/dxt/blob/main/MANIFEST.md

    public class DXTManifest
    {
        [JsonProperty("dxt_version")]
        public string DxtVersion { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("long_description")]
        public string LongDescription { get; set; }

        [JsonProperty("author")]
        public Author Author { get; set; }

        [JsonProperty("repository")]
        public Repository Repository { get; set; }

        [JsonProperty("homepage")]
        public string Homepage { get; set; }

        [JsonProperty("documentation")]
        public string Documentation { get; set; }

        [JsonProperty("support")]
        public string Support { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("screenshots")]
        public List<string> Screenshots { get; set; }

        [JsonProperty("server")]
        public Server Server { get; set; }

        [JsonProperty("tools")]
        public List<Tool> Tools { get; set; }

        [JsonProperty("prompts")]
        public List<Prompt> Prompts { get; set; }

        [JsonProperty("tools_generated")]
        public bool ToolsGenerated { get; set; }

        [JsonProperty("keywords")]
        public List<string> Keywords { get; set; }

        [JsonProperty("license")]
        public string License { get; set; }

        [JsonProperty("compatibility")]
        public Compatibility Compatibility { get; set; }

        [JsonProperty("user_config")]
        public UserConfig UserConfig { get; set; }
    }

    public class AllowedDirectories
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("multiple")]
        public bool Multiple { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; }

        [JsonProperty("default")]
        public List<string> Default { get; set; }
    }

    public class ApiKey
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("sensitive")]
        public bool Sensitive { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; }
    }

    public class Author
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Compatibility
    {
        [JsonProperty("claude_desktop")]
        public string ClaudeDesktop { get; set; }

        [JsonProperty("platforms")]
        public List<string> Platforms { get; set; }

        [JsonProperty("runtimes")]
        public Runtimes Runtimes { get; set; }
    }

    public class Env
    {
        [JsonProperty("ALLOWED_DIRECTORIES")]
        public string ALLOWEDDIRECTORIES { get; set; }
    }

    public class MaxFileSize
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("default")]
        public int Default { get; set; }

        [JsonProperty("min")]
        public int Min { get; set; }

        [JsonProperty("max")]
        public int Max { get; set; }
    }

    public class McpConfig
    {
        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("args")]
        public List<string> Args { get; set; }

        [JsonProperty("env")]
        public Env Env { get; set; }
    }

    public class Prompt
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("arguments")]
        public List<string> Arguments { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class Repository
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Runtimes
    {
        [JsonProperty("python")]
        public string Python { get; set; }

        [JsonProperty("node")]
        public string Node { get; set; }
    }

    public class Server
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("entry_point")]
        public string EntryPoint { get; set; }

        [JsonProperty("mcp_config")]
        public McpConfig McpConfig { get; set; }
    }

    public class Tool
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class UserConfig
    {
        [JsonProperty("allowed_directories")]
        public AllowedDirectories AllowedDirectories { get; set; }

        [JsonProperty("api_key")]
        public ApiKey ApiKey { get; set; }

        [JsonProperty("max_file_size")]
        public MaxFileSize MaxFileSize { get; set; }
    }


}
