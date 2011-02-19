using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Automaters.Core;
using System.Net;

namespace Automaters.Discovery.Ssdp
{
    /// <summary>
    /// Class to combine the functionality of SsdpListener and SsdpSearch
    /// </summary>
    public class SsdpClient
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpClient"/> class.
        /// </summary>
        public SsdpClient()
        {
            this.Server = new SsdpSocket(new IPEndPoint(IPAddress.Any, 1900));
            this.Listener = this.CreateListener();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a search.
        /// </summary>
        /// <returns></returns>
        public virtual SsdpSearch CreateSearch(bool requireUniqueLocation)
        {
            var search = new SsdpSearch(this.Server);

            Dictionary<string, SsdpMessage> dict = new Dictionary<string, SsdpMessage>();
            search.ResultFound += (sender, e) =>
            {
                lock (dict)
                {
                    // Restrict duplicate search responses based on location or UDN/USN
                    // The reason for this is that there is potential for devices to share the same UDN
                    // However, each unique location is definitely a separate result
                    // And there's no potential for two devices to share the same location
                    string key = (requireUniqueLocation ? e.Value.Location : e.Value.USN);
                    if (dict.ContainsKey(key))
                        return;

                    dict.Add(key, e.Value);
                }

                this.OnSsdpMessageReceived(sender, e);
                this.OnSearchResponse(sender, e);   
            };
            return search;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Creates a listener.
        /// </summary>
        /// <returns></returns>
        protected virtual SsdpListener CreateListener()
        {
            var listener = new SsdpListener(this.Server);
            listener.SsdpMessageReceived += OnSsdpMessageReceived;
            listener.SsdpAlive += OnSsdpAlive;
            listener.SsdpByeBye += OnSsdpByeBye;
            return listener;
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when [SSDP message received].
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SsdpMessageReceived;
        /// <summary>
        /// Occurs when [search response].
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SearchResponse;
        /// <summary>
        /// Occurs when [SSDP alive].
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SsdpAlive;
        /// <summary>
        /// Occurs when [SSDP bye bye].
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SsdpByeBye;

        /// <summary>
        /// Called when [SSDP message received].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Automaters.Core.EventArgs&lt;Automaters.Discovery.Ssdp.SsdpMessage&gt;"/> instance containing the event data.</param>
        protected virtual void OnSsdpMessageReceived(object sender, EventArgs<SsdpMessage> e)
        {
            var handler = this.SsdpMessageReceived;
            if (handler != null)
                handler(sender, e);
        }

        /// <summary>
        /// Called when [search response].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Automaters.Core.EventArgs&lt;Automaters.Discovery.Ssdp.SsdpMessage&gt;"/> instance containing the event data.</param>
        protected virtual void OnSearchResponse(object sender, EventArgs<SsdpMessage> e)
        {
            var handler = this.SearchResponse;
            if (handler != null)
                handler(sender, e);
        }

        /// <summary>
        /// Called when [SSDP alive].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Automaters.Core.EventArgs&lt;Automaters.Discovery.Ssdp.SsdpMessage&gt;"/> instance containing the event data.</param>
        protected virtual void OnSsdpAlive(object sender, EventArgs<SsdpMessage> e)
        {
            var handler = this.SsdpAlive;
            if (handler != null)
                handler(sender, e);
        }

        /// <summary>
        /// Called when [SSDP bye bye].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Automaters.Core.EventArgs&lt;Automaters.Discovery.Ssdp.SsdpMessage&gt;"/> instance containing the event data.</param>
        protected virtual void OnSsdpByeBye(object sender, EventArgs<SsdpMessage> e)
        {
            var handler = this.SsdpByeBye;
            if (handler != null)
                handler(sender, e);
        }

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
        /// Gets or sets the listener.
        /// </summary>
        /// <value>
        /// The listener.
        /// </value>
        public SsdpListener Listener
        {
            get;
            protected set;
        }

        #endregion

    }
}
