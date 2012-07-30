using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Automaters.Core.Net;
using System.Net;
using System.Net.Sockets;
using Automaters.Core.Collections;
using System.Threading;
using Automaters.Core;
using System.IO;
using Automaters.Core.Timers;

namespace Automaters.Discovery.Ssdp
{
    /// <summary>
    /// Server class for sending out SSDP announcements and responding to searches
    /// </summary>
    public class SsdpServer : IDisposable
    {

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpServer"/> class.
        /// </summary>
        public SsdpServer()
        {
            this.Announcers = new Dictionary<SsdpAnnouncer, bool>();
            this.Server = new SsdpSocket(new IPEndPoint(IPAddress.Any, 1900));
            this.Server.DataReceived += this.OnDataReceived;
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
        /// Creates the announcer.
        /// </summary>
        /// <param name="respondToSearches">if set to <c>true</c> [respond to searches].</param>
        /// <returns></returns>
        public SsdpAnnouncer CreateAnnouncer(bool respondToSearches = true)
        {
            lock (this.Announcers)
            {
                var announcer = new SsdpAnnouncer(this.Server);
                this.Announcers.Add(announcer, respondToSearches);
                return announcer;
            }
        }

        /// <summary>
        /// Removes the announcer.
        /// </summary>
        /// <param name="announcer">The announcer.</param>
        public void RemoveAnnouncer(SsdpAnnouncer announcer)
        {
            lock (this.Announcers)
            {
                this.Announcers.Remove(announcer);
            }
        }

        /// <summary>
        /// Sets the search responses.
        /// </summary>
        /// <param name="announcer">The announcer.</param>
        /// <param name="respondToSearches">if set to <c>true</c> [respond to searches].</param>
        public void SetSearchResponses(SsdpAnnouncer announcer, bool respondToSearches)
        {
            lock (this.Announcers)
            {
                if (!this.Announcers.ContainsKey(announcer))
                    throw new KeyNotFoundException();

                this.Announcers[announcer] = respondToSearches;
            }
        }

        /// <summary>
        /// Gets the matching responders.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        /// <returns></returns>
        public IEnumerable<SsdpAnnouncer> GetMatchingResponders(SsdpMessage msg)
        {
            lock (this.Announcers)
            {
                foreach (var pair in this.Announcers.Where(pair => pair.Value))
                {
                    if (msg.SearchType == Protocol.SsdpAll)
                        yield return pair.Key;

                    if (msg.SearchType.StartsWith("uuid:") && msg.SearchType == pair.Key.USN)
                        yield return pair.Key;

                    if (msg.SearchType == pair.Key.NotificationType)
                        yield return pair.Key;
                }
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Sends the search response message.
        /// </summary>
        protected void SendSearchResponse(SsdpMessage msg, SsdpAnnouncer announcer)
        {
            lock (this.Server)
            {
                // If we were stopped then don't bother sending this message
                if (!this.Server.IsListening)
                    return;

                byte[] bytes = Encoding.ASCII.GetBytes(Protocol.CreateAliveResponse(
                    announcer.Location, msg.SearchType, announcer.USN, announcer.MaxAge, Protocol.DefaultUserAgent)); ;
                this.Server.Send(bytes, bytes.Length, msg.Source);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Called when [data received].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Automaters.Core.EventArgs&lt;Automaters.Core.Net.NetworkData&gt;"/> instance containing the event data.</param>
        protected virtual void OnDataReceived(object sender, EventArgs<NetworkData> e)
        {
            // Queue this response to be processed
            ThreadPool.QueueUserWorkItem(data =>
            {
                try
                {
                    // Parse our message and fire our event
                    using (var stream = new MemoryStream(e.Value.Buffer, 0, e.Value.Length))
                    {
                        this.OnSsdpMessageReceived(new SsdpMessage(HttpMessage.Parse(stream), e.Value.RemoteIPEndpoint));
                    }
                }
                catch (ArgumentException ex)
                {
                    System.Diagnostics.Trace.TraceError("Failed to parse SSDP response: {0}", ex.ToString());
                }
            });
        }

        protected virtual void OnSsdpMessageReceived(SsdpMessage msg)
        {
            // Ignore any advertisements
            if (msg.IsAdvertisement)
                return;

            // Set up our dispatcher to send the response to each matching announcer that supports responding
            foreach (var announcer in this.GetMatchingResponders(msg))
            {
                var temp = announcer;
                Dispatcher.Add(() => this.SendSearchResponse(msg, temp), TimeSpan.FromSeconds(new Random().Next(0, msg.MaxAge)));
            }
        }

        #endregion

        #region Properties

        protected static readonly TimeoutDispatcher Dispatcher = new TimeoutDispatcher();

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

        protected Dictionary<SsdpAnnouncer, bool> Announcers
        {
            get;
            private set;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            lock (this.Announcers)
            {
                this.Announcers.Clear();
            }

            this.Server.Close();
        }

        #endregion

    }
}
