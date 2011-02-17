using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Automaters.Core.Net;
using System.Net;

namespace Automaters.Discovery.Ssdp
{
    /// <summary>
    /// Class representing an SSDP request/response message
    /// </summary>
    public class SsdpMessage
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpMessage"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="source">The source.</param>
        public SsdpMessage(HttpMessage message, IPEndPoint source)
        {
            this.Message = message;
            this.Source = source;
            this.ParseMessageData();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Parses the message data.
        /// </summary>
        protected virtual void ParseMessageData()
        {
            // Parse out the type and UDN from the data
            this.Type = this.Message.Headers["NT"];
            this.UDN = this.USN;
            int index = this.UDN.IndexOf("::");
            if (index != -1)
            {
                if (string.IsNullOrEmpty(this.Type))
                    this.Type = this.UDN.Substring(index + 2);

                this.UDN = this.UDN.Substring(0, index);
            }

            // Parse out the max age from the cache control
            string cacheControl = this.Message.Headers["CACHE-CONTROL"].ToUpper();
            this.MaxAge = 0;
            int temp = 0;
            if (cacheControl.StartsWith("MAX-AGE") && int.TryParse(cacheControl.Substring(8), out temp))
                this.MaxAge = temp;


            string date = this.Message.Headers["DATE"];
            DateTime tempDt;
            if (string.IsNullOrEmpty(date) || !DateTime.TryParse(date, out tempDt))
                this.DateGenerated = DateTime.Now;
            else
                this.DateGenerated = tempDt;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public HttpMessage Message
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public IPEndPoint Source
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets a value indicating whether this is an alive message.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this is an alive message; otherwise, <c>false</c>.
        /// </value>
        public bool IsAlive
        {
            get { return (this.Message.Headers["NTS"].ToLower() != Protocol.SsdpByeByeNts.ToLower()); }
        }

        /// <summary>
        /// Gets the USN.
        /// </summary>
        public string USN
        {
            get { return this.Message.Headers["USN"]; }
        }

        /// <summary>
        /// Gets or sets the UDN.
        /// </summary>
        /// <value>
        /// The UDN.
        /// </value>
        public string UDN
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the max age.
        /// </summary>
        /// <value>
        /// The max age.
        /// </value>
        public int MaxAge
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the date generated.
        /// </summary>
        /// <value>
        /// The date generated.
        /// </value>
        public DateTime DateGenerated
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets the location.
        /// </summary>
        public string Location
        {
            get { return this.Message.Headers["LOCATION"]; }
        }

        /// <summary>
        /// Gets the server.
        /// </summary>
        public string Server
        {
            get { return this.Message.Headers["SERVER"]; }
        }

        /// <summary>
        /// Gets the type of the search.
        /// </summary>
        /// <value>
        /// The type of the search.
        /// </value>
        public string SearchType
        {
            get { return this.Message.Headers["ST"]; }
        }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public string Type
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets a value indicating whether this is a device message.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this is a device message; otherwise, <c>false</c>.
        /// </value>
        public bool IsDevice
        {
            get { return (this.Type.IndexOf(":device:") != -1); }
        }

        /// <summary>
        /// Gets a value indicating whether this is  service message.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this is a service message; otherwise, <c>false</c>.
        /// </value>
        public bool IsService
        {
            get { return (this.Type.IndexOf(":service:") != -1); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is advertisement.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is advertisement; otherwise, <c>false</c>.
        /// </value>
        public bool IsAdvertisement
        {
            get { return string.IsNullOrEmpty(this.SearchType); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is root.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is root; otherwise, <c>false</c>.
        /// </value>
        public bool IsRoot
        {
            get { return (this.Type == "upnp:rootdevice"); }
        }

        #endregion

    }
}
