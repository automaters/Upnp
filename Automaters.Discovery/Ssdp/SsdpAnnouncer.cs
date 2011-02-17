using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public SsdpAnnouncer(SsdpServer server = null)
        {
            if (server == null)
                server = new SsdpServer();

            this.Server = server;
            this.UserAgent = Protocol.DefaultUserAgent;
            this.MaxAge = Protocol.DefaultMaxAge;
            this.NotificationType = string.Empty;
            this.Location = string.Empty;
            this.USN = string.Empty;
            this.RemoteEndPoints = new SyncCollection<IPEndPoint>() { Protocol.DiscoveryEndpoints.IPv4 };
        }

        public void Start()
        {
            lock (this.SyncRoot)
            {
                // If we're already running then ignore this request
                if (this.IsRunning)
                    return;

                // Send our initial alive message
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
            }
        }

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

        public void SendAliveMessage()
        {
            // TODO: Do we need to make sure we join these multicast groups?

            foreach (IPEndPoint ep in this.RemoteEndPoints)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(Protocol.CreateAliveNotify(Protocol.DiscoveryEndpoints.IPv4,
                    this.Location, this.NotificationType, this.USN, this.MaxAge, this.UserAgent));
                this.Server.Send(bytes, bytes.Length, ep);
            }
        }

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

        #region Properties

        protected readonly object SyncRoot = new object();
        protected static readonly TimeoutDispatcher Dispatcher = new TimeoutDispatcher();
        protected IDisposable TimeoutToken = null;

        protected SsdpServer Server
        {
            get;
            set;
        }

        public bool IsRunning
        {
            get { return this.TimeoutToken != null; }
        }

        public SyncCollection<IPEndPoint> RemoteEndPoints
        {
            get;
            private set;
        }

        public string Location
        {
            get;
            set;
        }

        public string NotificationType
        {
            get;
            set;
        }

        public string USN
        {
            get;
            set;
        }

        public ushort MaxAge
        {
            get;
            set;
        }

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
            this.Server.Close();
        }

        #endregion

    }

}
