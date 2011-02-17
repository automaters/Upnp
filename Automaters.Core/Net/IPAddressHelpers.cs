using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;

namespace Automaters.Core.Net
{
    /// <summary>
    /// Helper functions for IPAddress information
    /// </summary>
    public static class IPAddressHelpers
    {

        /// <summary>
        /// Determines whether the specified address is multicast.
        /// </summary>
        /// <param name="addr">The address.</param>
        /// <returns>
        ///   <c>true</c> if the specified address is multicast; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMulticast(IPAddress addr)
        {
            // If this is IPv6 then our check is easy
            if (addr.IsIPv6Multicast)
                return true;

            // Otherwise if we're IPv4 look for anything higher than 224.0.0.0
            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                // Special case for broadcast since it's above the multicast range
                if (addr.Equals(IPAddress.Broadcast))
                    return false;

                return (BitConverter.ToUInt32(new byte[] { 0, 0, 0, 224 }, 0) <= BitConverter.ToUInt32(addr.GetAddressBytes(), 0));
            }
            
            return false;
        }

        /// <summary>
        /// Gets the unicast addresses.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <returns></returns>
        public static IEnumerable<IPAddress> GetUnicastAddresses(Func<IPAddress, bool> condition)
        {
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var uni in iface.GetIPProperties().UnicastAddresses.Where(u => condition(u.Address)))
                    yield return uni.Address;
            }
        }

        /// <summary>
        /// Gets the IPv4 addresses.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IPAddress> GetIPv4Addresses()
        {
            return GetUnicastAddresses(addr => addr.AddressFamily == AddressFamily.InterNetwork);
        }

        /// <summary>
        /// Gets the IPv6 addresses.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IPAddress> GetIPv6Addresses()
        {
            return GetUnicastAddresses(addr => addr.AddressFamily == AddressFamily.InterNetworkV6);
        }

    }
}
