using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Automaters.Core.Net;
using System.Net;
using System.Net.Sockets;

namespace Automaters.Discovery.Ssdp
{
    public class SsdpServer : UdpServer
    {
        
        public SsdpServer()
            : this(new IPEndPoint(IPAddress.Any, 0))
        {
        }

        public SsdpServer(IPEndPoint localEp)
            : base(localEp)
        {
        }

        public virtual void JoinMulticastGroupAllInterfaces(IPEndPoint remoteEp)
        {
            var localIps = IPAddressHelpers.GetUnicastAddresses(ip => ip.AddressFamily == remoteEp.AddressFamily);
            foreach (var addr in localIps)
            {
                try
                {
                    this.JoinMulticastGroup(remoteEp.Address, addr);
                }
                catch (SocketException)
                {
                    // If we're already joined to this group we'll throw an error so just ignore it
                }
            }
        }

        protected override Socket CreateSocket(IPEndPoint localEp)
        {
            var sock = base.CreateSocket(localEp);
            sock.ReceiveBufferSize = 4096;
            sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            return sock;
        }

    }
}
