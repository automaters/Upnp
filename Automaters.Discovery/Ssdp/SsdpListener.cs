using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Automaters.Core;
using Automaters.Core.Net;

namespace Automaters.Discovery.Ssdp
{
    public class SsdpListener : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpServer"/> class.
        /// </summary>
        public SsdpListener()
        {
            this.Server = new SsdpSocket(new IPEndPoint(IPAddress.Any, 1900));
            this.Server.SsdpMessageReceived += this.OnSsdpMessageReceived;
        }

        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        protected SsdpSocket Server
        {
            get;
            set;
        }

        /// <summary>
        /// Starts listening on the specified remote endpoints.
        /// </summary>
        /// <param name="remoteEps">The remote eps.</param>
        public void StartListening(params IPEndPoint[] remoteEps)
        {
            if (remoteEps == null || remoteEps.Length == 0)
                remoteEps = new IPEndPoint[] { Protocol.DiscoveryEndpoints.IPv4 };

            lock (this.Server)
            {
                if (!this.Server.IsListening)
                    this.Server.StartListening();

                this.Server.EnableBroadcast = true;

                // Join all the multicast groups specified
                foreach (IPEndPoint ep in remoteEps.Where(ep => IPAddressHelpers.IsMulticast(ep.Address)))
                    this.Server.JoinMulticastGroupAllInterfaces(ep);
            }
        }

        /// <summary>
        /// Stops listening on the specified remote endpoints.
        /// </summary>
        /// <param name="remoteEps">The remote eps.</param>
        public void StopListeningOn(params IPEndPoint[] remoteEps)
        {
            // If nothing specified then just stop listening on all
            if (remoteEps == null || remoteEps.Length == 0)
            {
                this.StopListening();
                return;
            }

            lock (this.Server)
            {
                if (!this.Server.IsListening)
                    return;

                // Drop all the multicast groups specified
                foreach (IPEndPoint ep in remoteEps.Where(ep => IPAddressHelpers.IsMulticast(ep.Address)))
                {
                    try
                    {
                        this.Server.DropMulticastGroup(ep.Address);
                    }
                    catch (SocketException)
                    {
                        // If we're not part of this group then it will throw an error so just ignore it
                    }
                }
            }
        }

        /// <summary>
        /// Stops listening on all end points.
        /// </summary>
        public void StopListening()
        {
            lock (this.Server)
            {
                if (!this.Server.IsListening)
                    return;

                this.Server.StopListening();
            }
        }

        /// <summary>
        /// Occurs when an SSDP message is received.
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SsdpMessageReceived;

        /// <summary>
        /// Called when an SSDP message is received.
        /// </summary>
        /// <param name="sender"> </param>
        /// <param name="msg">The message.</param>
        private void OnSsdpMessageReceived(object sender, EventArgs<SsdpMessage> msg)
        {
            OnSsdpMessageReceived(msg.Value);
        }

        protected virtual void OnSsdpMessageReceived(SsdpMessage ssdpMessage)
        {
            var handler = this.SsdpMessageReceived;
            if (handler != null)
                handler(this, new EventArgs<SsdpMessage>(ssdpMessage));

            if (ssdpMessage.IsAlive)
                this.OnSsdpAlive(ssdpMessage);
            else if (ssdpMessage.IsByeBye)
                this.OnSsdpByeBye(ssdpMessage);
        }

        /// <summary>
        /// Occurs when SSDP alive received.
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SsdpAlive;

        /// <summary>
        /// Occurs when SSDP alive received
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void OnSsdpAlive(SsdpMessage msg)
        {
            var handler = this.SsdpAlive;
            if (handler != null)
                handler(this, new EventArgs<SsdpMessage>(msg));
        }

        /// <summary>
        /// Occurs when SSDP bye bye.
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SsdpByeBye;

        /// <summary>
        /// Occurs when SSDP bye bye
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void OnSsdpByeBye(SsdpMessage msg)
        {
            var handler = this.SsdpByeBye;
            if (handler != null)
                handler(this, new EventArgs<SsdpMessage>(msg));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if(!disposing)
                return;

            this.Server.Close();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}