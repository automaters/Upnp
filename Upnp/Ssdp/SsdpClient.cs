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
    public class SsdpClient : SsdpListener
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpClient"/> class.
        /// </summary>
        public SsdpClient()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a search.
        /// </summary>
        /// <returns></returns>
        public virtual SsdpSearch CreateSearch(bool requireUniqueLocation)
        {
            var search = new SsdpSearch();

            Dictionary<string, SsdpMessage> dict = new Dictionary<string, SsdpMessage>();
            search.Filter = (msg) =>
            {
                lock (dict)
                {
                    // Restrict duplicate search responses based on location or UDN/USN
                    // The reason for this is that there is potential for devices to share the same UDN
                    // However, each unique location is definitely a separate result
                    // And there's no potential for two devices to share the same location
                    string key = (requireUniqueLocation ? msg.Location : msg.USN);
                    if (dict.ContainsKey(key))
                        return false;

                    dict.Add(key, msg);
                    return true;
                }
            };

            search.ResultFound += (sender, e) =>
            {
                this.OnSsdpMessageReceived(e.Value);
                this.OnSearchResponse(sender, e);   
            };

            return search;
        }

        #endregion

        #region Protected Methods

        #endregion

        #region Events

        /// <summary>
        /// Occurs when [search response].
        /// </summary>
        public event EventHandler<EventArgs<SsdpMessage>> SearchResponse;
        
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

        #endregion

        #region Properties

        #endregion

    }
}
