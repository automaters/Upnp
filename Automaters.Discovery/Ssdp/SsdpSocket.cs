using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Automaters.Core;
using Automaters.Core.Net;
using System.Net;
using System.Net.Sockets;

namespace Automaters.Discovery.Ssdp
{
    public class SsdpSocket : UdpServer
    {
        
        public SsdpSocket()
            : this(new IPEndPoint(IPAddress.Any, 0))
        {
            
        }

        public SsdpSocket(IPEndPoint localEp)
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
            //sock.ExclusiveAddressUse = false;
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            return sock;
        }

        protected override void OnDataReceived(NetworkData args)
        {
            base.OnDataReceived(args);

            // Queue this response to be processed
            ThreadPool.QueueUserWorkItem(data =>
            {
                try
                {
                    // Parse our message and fire our event
                    using (var stream = new MemoryStream(args.Buffer, 0, args.Length))
                    {
                        this.OnSsdpMessageReceived(new SsdpMessage(HttpMessage.Parse(stream), args.RemoteIPEndpoint));
                    }
                }
                catch (ArgumentException ex)
                {
                    System.Diagnostics.Trace.TraceError("Failed to parse SSDP response: {0}", ex.ToString());
                }
            });

        }

        /// <summary>
        /// Occurs when an SSDP message is received.
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SsdpMessageReceived;

        /// <summary>
        /// Called when an SSDP message is received.
        /// </summary>
        /// <param name="msg">The message.</param>
        protected virtual void OnSsdpMessageReceived(SsdpMessage msg)
        {
            var handler = this.SsdpMessageReceived;
            if (handler != null)
                handler(this, new EventArgs<SsdpMessage>(msg));
        }
    }
}
