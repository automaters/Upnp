using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Upnp.Timers;

namespace Upnp.Ssdp
{
    /// <summary>
    /// Server class for sending out SSDP announcements and responding to searches
    /// </summary>
    public class SsdpServer : SsdpListener
    {

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpServer"/> class.
        /// </summary>
        public SsdpServer()
        {
            this.Announcers = new Dictionary<SsdpAnnouncer, bool>();
        }

        #endregion

        #region Public Methods

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

        protected override void OnSsdpMessageReceived(SsdpMessage msg)
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

        protected Dictionary<SsdpAnnouncer, bool> Announcers
        {
            get;
            private set;
        }

        public bool IsListening
        {
            get { return this.Server.IsListening; }
        }

        #endregion

        #region IDisposable Implementation
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this.Announcers)
                {
                    this.Announcers.Clear();
                }
            }

            base.Dispose(disposing);
        }
        #endregion

    }
}
