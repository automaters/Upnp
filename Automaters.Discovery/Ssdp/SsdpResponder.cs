using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Automaters.Core.Collections;
using Automaters.Core;
using System.Net;
using Automaters.Core.Timers;
using Automaters.Core.Net;
using System.Threading;
using System.IO;

namespace Automaters.Discovery.Ssdp
{
    /// <summary>
    /// Class used to respond to SSDP Searches
    /// </summary>
    /// <remarks>
    /// TODO: I think we should come up with a better way to do this.
    /// If we have one responder per service and each responder has to parse all SSDP messages received
    /// then that's a lot of unnecessary work. We should come up with a better class structure for this.
    /// </remarks>
    public class SsdpResponder : IDisposable
    {
        
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpAnnouncer"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public SsdpResponder(SsdpServer server = null)
        {
            if (server == null)
            {
                server = new SsdpServer();
                this.OwnsServer = true;
            }

            this.Server = server;
            this.UserAgent = Protocol.DefaultUserAgent;
            this.MaxAge = Protocol.DefaultMaxAge;
            this.NotificationType = string.Empty;
            this.Location = string.Empty;
            this.USN = string.Empty;
            this.RemoteEndPoints = new SyncCollection<IPEndPoint>() { Protocol.DiscoveryEndpoints.IPv4 };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            lock (this.SyncRoot)
            {
                // If we're already running then ignore this request
                if (this.IsRunning)
                    return;

                // Start our server
                this.Server.StartListening();

                // Remove any previous handlers (in case our server didn't stop by us) and add ours
                this.Server.DataReceived -= OnDataReceived;
                this.Server.DataReceived += OnDataReceived;
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            lock (this.SyncRoot)
            {
                // If we're already running then ignore this request
                if (!this.IsRunning)
                    return;

                // Remove our handler so we no longer receive search requests
                this.Server.DataReceived -= OnDataReceived;

                // Stop listening if we own the server
                if (this.OwnsServer)
                    this.Server.StopListening();
            }
        }

        /// <summary>
        /// Determines whether the specified search message is a match.
        /// </summary>
        /// <param name="msg">The search message.</param>
        /// <returns>
        ///   <c>true</c> if the specified search message is a match; otherwise, <c>false</c>.
        /// </returns>
        public bool IsMatch(SsdpMessage msg)
        {
            if (msg.SearchType == Protocol.SsdpAll)
                return true;

            if (msg.SearchType.StartsWith("uuid:"))
                return (msg.SearchType == this.USN);

            return (msg.SearchType == this.NotificationType);
        }

        #endregion

        #region Protected Methods

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

            // Check to see if this matches our data before responding
            if (!this.IsMatch(msg))
                return;

            // Set up our dispatcher to send the response
            Dispatcher.Add(() => this.SendSearchResponse(msg), TimeSpan.FromSeconds(msg.MaxAge));
        }

        /// <summary>
        /// Sends the bye bye message.
        /// </summary>
        protected void SendSearchResponse(SsdpMessage msg)
        {
            lock (this.SyncRoot)
            {
                // If we were stopped then don't bother sending this message
                if (!this.IsRunning)
                    return;

                byte[] bytes = Encoding.ASCII.GetBytes(Protocol.CreateAliveResponse(
                    this.Location, msg.SearchType, this.USN, this.MaxAge, Protocol.DefaultUserAgent)); ;
                this.Server.Send(bytes, bytes.Length, msg.Source);
            }
        }

        #endregion

        #region Properties

        protected readonly object SyncRoot = new object();
        protected static readonly TimeoutDispatcher Dispatcher = new TimeoutDispatcher();

        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        protected SsdpServer Server
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
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning
        {
            get { return this.Server.IsListening; }
        }

        /// <summary>
        /// Gets the remote end points.
        /// </summary>
        public SyncCollection<IPEndPoint> RemoteEndPoints
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        public string Location
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type of the notification.
        /// </summary>
        /// <value>
        /// The type of the notification.
        /// </value>
        public string NotificationType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the USN.
        /// </summary>
        /// <value>
        /// The USN.
        /// </value>
        public string USN
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the max age.
        /// </summary>
        /// <value>
        /// The max age.
        /// </value>
        public ushort MaxAge
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        /// <value>
        /// The user agent.
        /// </value>
        public string UserAgent
        {
            get;
            set;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.OwnsServer)
                this.Server.Close();
        }

        #endregion

    }
}
