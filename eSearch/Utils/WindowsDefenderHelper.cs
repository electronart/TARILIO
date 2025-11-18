using NetFwTypeLib; // Firewall API reference
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Utils
{
    [SupportedOSPlatform("windows")]
    public class WindowsDefenderHelper
    {

        /// <summary>
        /// May also throw exceptions, treat exceptions as unable to detect.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> IsAllowedThroughFirewall()
        {
            var res = await Task.Run<bool>(() =>
            {


                Type? type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                if (type == null) throw new Exception("type cannot be null");
                INetFwPolicy2? firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(type);
                if (firewallPolicy == null) throw new Exception("Firewall policy null..");
                string? appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName; // Your exe's full path

                // Check across profiles; you can limit to specific ones like NET_FW_PROFILE2_PRIVATE (2)
                foreach (INetFwRule rule in firewallPolicy.Rules)
                {
                    if (rule.ApplicationName != null &&
                        rule.ApplicationName.Equals(appPath, StringComparison.OrdinalIgnoreCase) &&
                        rule.Direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN && // Adjust for OUT if needed
                        rule.Action == NET_FW_ACTION_.NET_FW_ACTION_ALLOW &&
                        rule.Enabled)
                    {
                        return true; // Found an active allow rule
                    }
                }
                return false; // No matching allow rule found
            });
            return res;
        }

        private bool IsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }


        public void AddFirewallException()
        {
            if (!IsAdmin())
            {
                throw new UnauthorizedAccessException("REQUIRE_ELEVATION");
            }

            Type type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(type);

            INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            firewallRule.Name = Program.GetBaseProductTag(); // Descriptive name
            firewallRule.Description = $"Allow {Program.GetBaseProductTag()} through firewall";
            firewallRule.ApplicationName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName; // Path to your exe
            firewallRule.Enabled = true;
            firewallRule.InterfaceTypes = "All";
            firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN; // Or OUT for outbound
            firewallRule.Protocol = 6; // TCP; use 17 for UDP, or 256 for any
            firewallRule.LocalPorts = "*"; // e.g., "80,443" or "*" for all

            firewallPolicy.Rules.Add(firewallRule);
        }
    }
}
