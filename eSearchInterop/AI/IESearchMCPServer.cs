using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace eSearch.Interop.AI
{
    public interface IESearchMCPServer
    {
        public string DisplayName
        {
            get;
        }

        public bool IsServerRunning
        {
            get;
        }

        public bool IsErrorState
        {
            get;
        }

        public Task<bool> StartServer();

        public Task<bool> StopServer();

        public IReadOnlyList<string> ConsoleOutputDisplayLines
        {
            get;
        }

        public IClientTransport? GetClientTransport();

        /// <summary>
        /// MCP Servers frequently use Kebab-Case but Microsoft SemanticKernel expects Snake_Case (WHY?)
        /// They also only accept ASCII Characters/Digits and Underscores
        /// This function will convert to Snake Case and ensure only safe characters remain in the string
        /// All non ASCII Characters will be replaced with an _Underscore.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ToSemanticKernelSafePluginName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            StringBuilder result = new StringBuilder();

            foreach (char c in input)
            {
                if (c == '-') // Convert hyphen to underscore
                {
                    result.Append('_');
                }
                else if (char.IsLetterOrDigit(c) && c < 128) // Keep ASCII letters and digits
                {
                    result.Append(char.ToLower(c));
                }
                else // Replace non-ASCII letters, digits, or underscores with underscore
                {
                    result.Append('_');
                }
            }

            // Replace multiple consecutive underscores with a single underscore
            string final = Regex.Replace(result.ToString(), @"_+", "_");

            // Trim leading and trailing underscores
            return final.Trim('_');
        }
    }
}
