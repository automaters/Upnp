using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Automaters.Core;
using System.Net;
using Automaters.Core.Net;
using System.Net.Sockets;

namespace Automaters.Discovery.Ssdp
{
    /// <summary>
    /// Class for listening to SSDP Advertisements (Alive/ByeBye)
    /// </summary>
    public class SsdpListener : IDisposable
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpListener"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public SsdpListener(SsdpSocket server = null)
        {
            if (server == null)
            {
                server = new SsdpSocket(new IPEndPoint(IPAddress.Any, 1900));
                this.OwnsServer = true;
            }

            this.Server = server;
        }

        #endregion

        #region Public Methods

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

        #endregion

        #region Events

        /// <summary>
        /// Called when an SSDP message is received.
        /// </summary>
        /// <param name="msg">The message.</param>
        protected void OnSsdpMessageReceived(SsdpMessage msg)
        {
            var handler = this.SsdpMessageReceived;
            if (handler != null)
                handler(this, new EventArgs<SsdpMessage>(msg));

            var handler2 = (msg.IsAlive ? this.SsdpAlive : this.SsdpByeBye);
            if (handler != null)
                handler2(this, new EventArgs<SsdpMessage>(msg));
        }

        /// <summary>
        /// Occurs when an SSDP message is received.
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SsdpMessageReceived;

        /// <summary>
        /// Occurs when SSDP alive received.
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SsdpAlive;

        /// <summary>
        /// Occurs when SSDP bye bye.
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SsdpByeBye;

        #endregion

        #region Properties

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
        /// Gets or sets a value indicating whether [owns server].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [owns server]; otherwise, <c>false</c>.
        /// </value>
        protected bool OwnsServer
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is listening.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is listening; otherwise, <c>false</c>.
        /// </value>
        public bool IsListening
        {
            get { return this.Server.IsListening; }
        }

        #endregion
            
        #region IDisposable Implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void  Dispose()
        {
            // Only close the server if we own it
            if (this.OwnsServer)
 	            this.Server.Close();
        }

        #endregion

    }
}
