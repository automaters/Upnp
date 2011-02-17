using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Automaters.Discovery.Ssdp
{
    /// <summary>
    /// Class containing several protocol specific constants and helper functions
    /// </summary>
    public static class Protocol
    {
        /// <summary>
        /// Class containing all the IPEndPoints used in discovery
        /// </summary>
        public static class DiscoveryEndpoints
        {
            public static readonly IPEndPoint IPv4 = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
            public static readonly IPEndPoint Broadcast = new IPEndPoint(IPAddress.Broadcast, 1900);
            public static readonly IPEndPoint IPv6LinkLocal = new IPEndPoint(IPAddress.Parse("FF02::C"), 1900);
            public static readonly IPEndPoint IPv6SiteLocal = new IPEndPoint(IPAddress.Parse("FF05::C"), 1900);
            public static readonly IPEndPoint IPv6OrganizationLocal = new IPEndPoint(IPAddress.Parse("FF08::C"), 1900);
            public static readonly IPEndPoint IPv6Global = new IPEndPoint(IPAddress.Parse("FF0E::C"), 1900);
        }

        public const string DefaultUserAgent = "Automaters.Discovery.Ssdp";

        public const string SsdpSearchMethod = "M-SEARCH";
        public const string GenaNotifyMethod = "NOTIFY";

        public const string SsdpAliveNts = "ssdp:alive";
        public const string SsdpByeByeNts = "ssdp:byebye";
        public const string SsdpAll = "ssdp:all";
        public const string SsdpDiscover = "ssdp:discover";

        public const ushort DefaultMaxAge = 1800;
        public const ushort DefaultMx = 3;
        public const ushort MaxMX = 120;

        public const ushort SocketTtl = 4;

        private static string OSString = String.Format("{0}/{1}", Environment.OSVersion.Platform, Environment.OSVersion.Version);

        private static string DiscoveryRequest =
            "M-SEARCH * HTTP/1.1\r\n" +
            "HOST: {0}:{1}\r\n" +
            "MAN: \"ssdp:discover\"\r\n" +
            "ST: {2}\r\n" +
            "MX: {3}\r\n" +
            "\r\n";

        private static string AliveNotify =
            "NOTIFY * HTTP/1.1\r\n" +
            "HOST: {0}:{1}\r\n" +
            "CACHE-CONTROL: max-age = {2}\r\n" +
            "LOCATION: {3}\r\n" +
            "NT: {4}\r\n" +
            "NTS: ssdp:alive\r\n" +
            "SERVER: {5} UPnP/1.1 {6}\r\n" +
            "USN: {7}\r\n" +
            "\r\n";

        private static string AliveResponse =
            "HTTP/1.1 200 OK\r\n" +
            "CACHE-CONTROL: max-age = {0}\r\n" +
            "DATE: {1}\r\n" +
            "EXT:\r\n" +
            "LOCATION: {2}\r\n" +
            "SERVER: {3} UPnP/1.1 {4}\r\n" +
            "ST: {5}\r\n" +
            "USN: {6}\r\n" +
            "\r\n";

        private static readonly string ByebyeNotify =
            "NOTIFY * HTTP/1.1\r\n" +
            "HOST: {0}:{1}\r\n" +
            "NT: {2}\r\n" +
            "NTS: ssdp:byebye\r\n" +
            "USN: {3}\r\n" +
            "\r\n";

        /// <summary>
        /// Creates the discovery request.
        /// </summary>
        /// <param name="dest">The destination.</param>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="mx">The mx.</param>
        /// <returns></returns>
        public static string CreateDiscoveryRequest(IPEndPoint dest, string serviceType, ushort mx)
        {
            return string.Format(DiscoveryRequest, dest.Address, dest.Port, serviceType, mx);
        }

        /// <summary>
        /// Creates the alive notify.
        /// </summary>
        /// <param name="dest">The destination.</param>
        /// <param name="location">The location.</param>
        /// <param name="notificationType">Type of the notification.</param>
        /// <param name="usn">The usn.</param>
        /// <param name="maxAge">The max age.</param>
        /// <param name="userAgent">The user agent.</param>
        /// <returns></returns>
        public static string CreateAliveNotify(IPEndPoint dest, string location, string notificationType, string usn, ushort maxAge, string userAgent)
        {
            return string.Format(AliveNotify, dest.Address, dest.Port, maxAge, location, notificationType, OSString, userAgent, usn);
        }

        /// <summary>
        /// Creates the alive response.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="searchType">Type of the search.</param>
        /// <param name="usn">The usn.</param>
        /// <param name="maxAge">The max age.</param>
        /// <param name="userAgent">The user agent.</param>
        /// <returns></returns>
        public static string CreateAliveResponse(string location, string searchType, string usn, ushort maxAge, string userAgent)
        {
            return string.Format(AliveResponse, maxAge, DateTime.Now.ToString("r"), location, OSString, userAgent, searchType, usn);
        }

        /// <summary>
        /// Creates the bye bye notify.
        /// </summary>
        /// <param name="dest">The destination.</param>
        /// <param name="notificationType">Type of the notification.</param>
        /// <param name="usn">The usn.</param>
        /// <returns></returns>
        public static string CreateByeByeNotify(IPEndPoint dest, string notificationType, string usn)
        {
            return string.Format(ByebyeNotify, dest.Address, dest.Port, notificationType, usn);
        }

    }
}
