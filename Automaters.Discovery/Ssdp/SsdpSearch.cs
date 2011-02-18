using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Automaters.Core;
using System.Threading;
using Automaters.Core.Net;
using System.IO;
using Automaters.Core.Timers;

namespace Automaters.Discovery.Ssdp
{
    /// <summary>
    /// Class representing an SSDP Search
    /// </summary>
    public class SsdpSearch : IDisposable
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpSearch"/> class.
        /// </summary>
        public SsdpSearch(SsdpServer server = null)
        {
            if (server == null)
            {
                server = new SsdpServer();
                this.OwnsServer = true;
            }

            this.Server = server;
            this.HostEndpoint = Protocol.DiscoveryEndpoints.IPv4;
            this.SearchType = Protocol.SsdpAll;
            this.Mx = Protocol.DefaultMx;
        }

        #region Public Methods

        /// <summary>
        /// Finds the first result.
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        /// <returns></returns>
        public SsdpMessage FindFirst(params IPEndPoint[] destinations)
        {
            object syncRoot = new object();
            SsdpMessage result = null;
            EventHandler<EventArgs<SsdpMessage>> resultHandler = null;
            
            // Create our handler to make all the magic happen
            resultHandler = (sender, e) =>
            {
                lock (syncRoot)
                {
                    // If we already got our first result then ignore this
                    if (result != null)
                        return;

                    // This is our first result so set our value, remove the handler, and cancel the search
                    result = e.Value;
                    this.ResultFound -= resultHandler;
                    this.CancelSearch();
                }
            };

            try
            {
                lock (this.SearchLock)
                {
                    // Add our handler and start the async search
                    this.ResultFound += resultHandler;
                    this.SearchAsync(destinations);
                }

                // Wait until our search is complete
                this.WaitForSearch();
            }
            finally
            {
                // Make sure we remove our handler when we're done
                this.ResultFound -= resultHandler;
            }

            return result;
        }

        /// <summary>
        /// Searches the specified destinations.
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        /// <returns></returns>
        public List<SsdpMessage> Search(params IPEndPoint[] destinations)
        {
            List<SsdpMessage> results = new List<SsdpMessage>();
            EventHandler<EventArgs<SsdpMessage>> resultHandler = (sender, e) =>
            {
                lock (results)
                {
                    results.Add(e.Value);
                }
            };
            EventHandler completeHandler = (sender, e) =>
            {
                lock (results)
                {
                    Monitor.PulseAll(results);
                }
            };

            try
            {
                lock (this.SearchLock)
                {
                    // Add our handlers and start the async search
                    this.ResultFound += resultHandler;
                    this.SearchComplete += completeHandler;
                    this.SearchAsync(destinations);
                }

                // Wait until our search is complete
                lock (results)
                {
                    Monitor.Wait(results);
                }

                // Return the results
                return results;
            }
            finally
            {
                // Make sure we remove our handlers when we're done
                this.ResultFound -= resultHandler;
                this.SearchComplete -= completeHandler;
            }
        }

        /// <summary>
        /// Searches asynchronously.
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        public void SearchAsync(params IPEndPoint[] destinations)
        {
            lock (this.SearchLock)
            {
                // If we're already searching then this is not allowed so throw an error
                if (this.IsSearching)
                    throw new InvalidOperationException("Search is already in progress.");

                this.IsSearching = true;
                this.Server.DataReceived += OnServerDataReceived;

                // TODO: Come up with a good calculation for this
                // Just double our mx value for the timeout for now
                this.CreateSearchTimeout(TimeSpan.FromSeconds(this.Mx * 2));
            }

            // If no destinations were specified then default to the IPv4 discovery
            if (destinations == null || destinations.Length == 0)
                destinations = new IPEndPoint[] { Protocol.DiscoveryEndpoints.IPv4 };

            // Start the server
            this.Server.StartListening();

            // Do we really need to join the multicast group to send out multicast messages? Seems that way...
            foreach (IPEndPoint dest in destinations.Where(ep => IPAddressHelpers.IsMulticast(ep.Address)))
                this.Server.JoinMulticastGroupAllInterfaces(dest);

            // If we're sending out any searches to the broadcast address then be sure to enable broadcasts
            if (!this.Server.EnableBroadcast)
                this.Server.EnableBroadcast = destinations.Any(ep => ep.Address.Equals(IPAddress.Broadcast));

            // Now send out our search data
            foreach (IPEndPoint dest in destinations)
            {
                // Make sure we respect our option as to whether we use the destination as the host value
                IPEndPoint host = (this.UseRemoteEndpointAsHost ? dest : this.HostEndpoint);
                string req = Protocol.CreateDiscoveryRequest(host, this.SearchType, this.Mx);
                byte[] bytes = Encoding.ASCII.GetBytes(req);

                // TODO: Should we make this configurable?
                // NOTE: It's recommended to send two searches
                this.Server.Send(bytes, bytes.Length, dest);
                this.Server.Send(bytes, bytes.Length, dest);
            }
        }

        /// <summary>
        /// Cancels the search.
        /// </summary>
        public void CancelSearch()
        {
            lock (this.SearchLock)
            {
                // If we're not searching then nothing to do here
                if (!this.IsSearching)
                    return;

                // If we were called from the timeout then this will be null
                if (this.CurrentSearchTimeout != null)
                {
                    this.CurrentSearchTimeout.Dispose();
                    this.CurrentSearchTimeout = null;
                }

                // Cleanup our handler and notify everyone that we're done
                this.Server.DataReceived -= OnServerDataReceived;

                // If this is our server then go ahead and stop listening
                if (this.OwnsServer)
                    this.Server.StopListening();

                this.IsSearching = false;
                this.OnSearchComplete();
            }
        }

        /// <summary>
        /// Waits for the current search to complete.
        /// </summary>
        public void WaitForSearch()
        {
            // Create an object for signaling and an event handler to signal it
            object signal = new object();
            EventHandler handler = (sender, args)  =>
            {
                lock (signal)
                {
                    Monitor.Pulse(signal);
                }
            };

            try
            {
                lock (signal)
                {
                    lock (this.SearchLock)
                    {
                        // If we're not searching then nothing to do here
                        if (!this.IsSearching)
                            return;

                        // Attach our handler
                        this.SearchComplete += handler;
                    }

                    // Wait for our event handler to trigger our signal
                    Monitor.Wait(signal);
                }
            }
            finally
            {
                // Make sure to remove our handler
                this.SearchComplete -= handler;
            }
        }

        #endregion

        #region Protected Methods

        protected void CreateSearchTimeout(TimeSpan timeout)
        {
            lock (this.SearchLock)
            {
                this.CurrentSearchTimeout = Dispatcher.Add(() =>
                {
                    lock (this.SearchLock)
                    {
                        // Search may have already been cancelled
                        if (!this.IsSearching)
                            return;

                        // Make sure we remove ourself before calling CancelSearch
                        this.CurrentSearchTimeout = null;

                        // Cancel search will clean up everything
                        // Seems kind of wrong but it does exactly what we want
                        // If we add a special cancel event or something then we'll want to change this
                        this.CancelSearch();
                    }
                }, timeout);
            }
        }

        #endregion

        #region Events

        protected virtual void OnServerDataReceived(object sender, EventArgs<NetworkData> e)
        {
            // Queue this response to be processed
            ThreadPool.QueueUserWorkItem(data =>
            {
                try
                {
                    // Parse our message and fire our event
                    using (var stream = new MemoryStream(e.Value.Buffer, 0, e.Value.Length))
                    {
                        this.OnResultFound(new SsdpMessage(HttpMessage.Parse(stream), e.Value.RemoteIPEndpoint));
                    }
                }
                catch (ArgumentException ex)
                {
                    System.Diagnostics.Trace.TraceError("Failed to parse SSDP response: {0}", ex.ToString());
                }
            });
        }

        public event EventHandler SearchComplete;

        protected virtual void OnSearchComplete()
        {
            var handler = this.SearchComplete;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public event EventHandler<EventArgs<SsdpMessage>> ResultFound;

        protected virtual void OnResultFound(SsdpMessage result)
        {
            // This is a search so ignore any advertisements we get
            if (result.IsAdvertisement)
                return;

            var handler = this.ResultFound;
            if (handler != null)
                handler(this, new EventArgs<SsdpMessage>(result));
        }

        #endregion

        #region Properties

        protected static readonly TimeoutDispatcher Dispatcher = new TimeoutDispatcher();

        protected readonly object SearchLock = new object();

        protected IDisposable CurrentSearchTimeout
        {
            get;
            set;
        }

        protected bool OwnsServer
        {
            get;
            set;
        }

        protected SsdpServer Server
        {
            get;
            set;
        }

        public bool IsSearching
        {
            get;
            protected set;
        }

        public bool UseRemoteEndpointAsHost
        {
            get;
            set;
        }

        public IPEndPoint HostEndpoint
        {
            get;
            set;
        }

        public string SearchType
        {
            get;
            set;
        }

        public ushort Mx
        {
            get;
            set;
        }

        #endregion

        #region IDisposable Interface

        public virtual void Dispose()
        {
            // Only close the server if we created it
            if (this.OwnsServer)
                this.Server.Close();
        }

        #endregion

    }
}
