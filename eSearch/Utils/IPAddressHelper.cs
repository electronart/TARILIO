using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Utils
{
    public class IPAddressHelper
    {
        public static string? GetLocalIPv4Address()
        {
            // Get all active network interfaces, excluding loopback and tunnels
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback
                          && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            // First, try to find an Ethernet (wired) interface
            var ethernetInterfaces = networkInterfaces
                .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet);

            foreach (var ni in ethernetInterfaces)
            {
                var ip = GetIPv4AddressFromInterface(ni);
                if (ip != null)
                {
                    return ip.ToString();
                }
            }

            // If no Ethernet found, fall back to other interfaces (e.g., Wi-Fi)
            foreach (var ni in networkInterfaces.Except(ethernetInterfaces))
            {
                var ip = GetIPv4AddressFromInterface(ni);
                if (ip != null)
                {
                    return ip.ToString();
                }
            }

            return null;
        }

        static IPAddress? GetIPv4AddressFromInterface(NetworkInterface ni)
        {
            var ipProps = ni.GetIPProperties();
            return ipProps?.UnicastAddresses
                .FirstOrDefault(ua => ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                                   && !IPAddress.IsLoopback(ua.Address)
                                   && !IsApipaAddress(ua.Address))  // Exclude APIPA (link-local) addresses
                ?.Address;
        }

        static bool IsApipaAddress(IPAddress ip)
        {
            byte[] bytes = ip.GetAddressBytes();
            return bytes[0] == 169 && bytes[1] == 254;
        }
    }
}
