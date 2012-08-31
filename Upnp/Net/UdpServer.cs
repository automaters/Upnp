using System;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace Upnp.Net
{
    /// <summary>
    /// UDP Server designed for listening and eventing all data received
    /// </summary>
    public class UdpServer : UdpClient
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServer"/> class.
        /// </summary>
        /// <param name="localEp">The local ep.</param>
        public UdpServer(IPEndPoint localEp)
            : base()
        {
            this.Client = this.CreateSocket(localEp);
            this.Client.Bind(localEp);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts listening.
        /// </summary>
        public void StartListening()
        {
            using (this.GetReadLock())
            {
                if (this.IsListening)
                    return;

                this.IsListening = true;
                this.BeginReceive();
            }
        }

        /// <summary>
        /// Stops listening.
        /// </summary>
        public void StopListening()
        {
            using (this.GetWriteLock())
            {
                if (!this.IsListening)
                    return;

                this.IsListening = false;
            }
        }

        #endregion

        #region Events

        public event EventHandler<EventArgs<NetworkData>> DataReceived;

        /// <summary>
        /// Called when [data received].
        /// </summary>
        /// <param name="args">The args.</param>
        protected virtual void OnDataReceived(NetworkData args)
        {
            var handler = this.DataReceived;
            if (handler != null)
                handler(this, new EventArgs<NetworkData>(args));
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Creates the socket.
        /// </summary>
        /// <param name="localEp">The local ep.</param>
        /// <returns></returns>
        protected virtual Socket CreateSocket(IPEndPoint localEp)
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        /// <summary>
        /// Begins receiving.
        /// </summary>
        protected void BeginReceive()
        {
            this.BeginReceive((ar) =>
            {
                IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                byte[] buffer;

                try
                {
                    using (this.GetReadLock())
                    {
                        // If the server is stopped then return now
                        if (!this.IsListening)
                            return;

                        // Complete our receive by getting the data
                        buffer = this.EndReceive(ar, ref remoteEp);

                        // Continue receiving data
                        this.BeginReceive();
                    }
                }
                catch (SocketException)
                {
                    // An error occurred while receiving the data so stop receiving
                    return;
                }
                catch (ObjectDisposedException)
                {
                    // Socket was closed/disposed so stop listening
                    return;
                }

                // Send our event forward
                this.OnDataReceived(new NetworkData(buffer, remoteEp));

            }, null);
        }
        
        #endregion

        #region Locking
                
        private readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Gets the write lock.
        /// </summary>
        /// <returns></returns>
        protected IDisposable GetWriteLock()
        {
            return new Disposable(() => Lock.EnterWriteLock(), () => Lock.ExitWriteLock());
        }

        /// <summary>
        /// Gets the read lock.
        /// </summary>
        /// <returns></returns>
        protected IDisposable GetReadLock()
        {
            return new Disposable(() => Lock.EnterReadLock(), () => Lock.ExitReadLock());
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this instance is listening.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is listening; otherwise, <c>false</c>.
        /// </value>
        public bool IsListening
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the local endpoint.
        /// </summary>
        public IPEndPoint LocalEndpoint
        {
            get { return this.Client.LocalEndPoint as IPEndPoint; }
        }

        /// <summary>
        /// Gets or sets the multicast TTL.
        /// </summary>
        /// <value>
        /// The multicast TTL.
        /// </value>
        public short MulticastTtl
        {
            get
            {
                return (short)this.Client.GetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive);
            }
            set
            {
                this.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether address re-use is allowed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if address re-use is allowed; otherwise, <c>false</c>.
        /// </value>
        public bool ReuseAddress
        {
            get
            {
                return ((int)this.Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress) == 1);
            }
            set
            {
                this.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, (value ? 1 : 0));
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Net.Sockets.UdpClient"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                this.StopListening();
            }
        }

        #endregion

    }
        
}
