using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Automaters.Core.Net
{
    /// <summary>
    /// Class that represents data transferred over the network
    /// </summary>
    public class NetworkData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkData"/> class.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="remoteEp">The remote ep.</param>
        public NetworkData(byte[] buffer, EndPoint remoteEp)
            : this(buffer, buffer.Length, remoteEp)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkData"/> class.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="length">The length.</param>
        /// <param name="remoteEp">The remote ep.</param>
        public NetworkData(byte[] buffer, int length, EndPoint remoteEp)
        {
            this.Buffer = buffer;
            this.Length = length;
            this.RemoteEndpoint = remoteEp;
        }

        /// <summary>
        /// Gets or sets the buffer.
        /// </summary>
        /// <value>
        /// The buffer.
        /// </value>
        public byte[] Buffer
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        public int Length
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the remote endpoint.
        /// </summary>
        /// <value>
        /// The remote endpoint.
        /// </value>
        public EndPoint RemoteEndpoint
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets the remote IP endpoint.
        /// </summary>
        public IPEndPoint RemoteIPEndpoint
        {
            get { return (this.RemoteEndpoint as IPEndPoint); }
        }
    }
}
