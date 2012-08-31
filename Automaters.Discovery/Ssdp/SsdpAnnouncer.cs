using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automaters.Core.Net;
using Automaters.Core.Timers;
using System.Net;
using Automaters.Core.Collections;

namespace Automaters.Discovery.Ssdp
{

    /// <summary>
    /// Class for announcing SSDP messages (Alive/ByeByes)
    /// </summary>
    public class SsdpAnnouncer : IDisposable
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpAnnouncer"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public SsdpAnnouncer(SsdpSocket server = null)
        {
            if (server == null)
            {
                server = new SsdpSocket();
                this.OwnsServer = true;
            }

            this.Server = server;
            this.UserAgent = Protocol.DefaultUserAgent;
            this.MaxAge = Protocol.DefaultMaxAge;
            this.NotificationType = string.Empty;
            this.Location = string.Empty;
            this.USN = string.Empty;
            this.RemoteEndPoints = new SyncCollection<IPEndPoint>() { Protocol.DiscoveryEndpoints.IPv4, Protocol.DiscoveryEndpoints.Broadcast };
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

                // Send our initial alive message
                Task.Factory.StartNew(() => { 
                    //add an initial random delay between 0-100ms;
                    var random = new Random();
                    Thread.Sleep(random.Next(0, 100));

                    this.SendAliveMessage();

                    // Create a new timeout to send out SSDP alive messages
                    // Also make sure we kick the first one off semi-instantly
                    this.TimeoutToken = Dispatcher.AddRepeating(() =>
                    {
                        lock (this.SyncRoot)
                        {
                            if (!this.IsRunning)
                                return;

                            this.SendAliveMessage();
                        }
                    }, TimeSpan.FromSeconds(this.MaxAge));
                });
            }
        }

        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public void Shutdown()
        {
            lock (this.SyncRoot)
            {
                // If we're already running then ignore this request
                if (!this.IsRunning)
                    return;

                // Kill our timeout token
                this.TimeoutToken.Dispose();
                this.TimeoutToken = null;

                // Now send our bye bye message
                this.SendByeByeMessage();
            }
        }

        /// <summary>
        /// Sends the alive message.
        /// </summary>
        public void SendAliveMessage()
        {
            // TODO: Do we need to make sure we join these multicast groups?
            foreach (IPEndPoint ep in this.RemoteEndPoints)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(Protocol.CreateAliveNotify(ep,
                    this.Location, this.NotificationType, this.USN, this.MaxAge, this.UserAgent));
                this.Server.Send(bytes, bytes.Length, ep);
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Sends the bye bye message.
        /// </summary>
        protected void SendByeByeMessage()
        {
            // TODO: Do we need to make sure we join these multicast groups?

            foreach (IPEndPoint ep in this.RemoteEndPoints)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(Protocol.CreateByeByeNotify(Protocol.DiscoveryEndpoints.IPv4,
                    this.NotificationType, this.USN));
                this.Server.Send(bytes, bytes.Length, ep);
            }
        }

        #endregion

        #region Properties

        protected readonly object SyncRoot = new object();
        protected static readonly TimeoutDispatcher Dispatcher = new TimeoutDispatcher();
        protected IDisposable TimeoutToken = null;

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
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning
        {
            get { return this.TimeoutToken != null; }
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
            this.Shutdown();

            if (this.OwnsServer)
                this.Server.Close();
        }

        #endregion

    }

}
