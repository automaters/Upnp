using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Automaters.Core.Extensions;
using System.Net;
using System.Runtime.Serialization;

namespace Automaters.Core.Net
{
    /// <summary>
    /// Class to represent an HttpMessage
    /// </summary>
    public class HttpMessage
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpMessage"/> class.
        /// </summary>
        /// <param name="isRequest">if set to <c>true</c> [is request].</param>
        protected HttpMessage(bool isRequest)
        {
            this.IsRequest = isRequest;
            this.Headers = new WebHeaderCollection();
            this.HttpVersion = "HTTP/1.1";

            if (this.IsResponse)
            {
                this.ResponseCode = (int)HttpStatusCode.OK;
                this.ResponseCodeDesc = "OK";
            }
        }

        /// <summary>
        /// Creates the request.
        /// </summary>
        /// <returns></returns>
        public static HttpMessage CreateRequest()
        {
            return new HttpMessage(true);
        }

        /// <summary>
        /// Creates the response.
        /// </summary>
        /// <returns></returns>
        public static HttpMessage CreateResponse()
        {
            return new HttpMessage(false);
        }

        /// <summary>
        /// Parses the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public static HttpMessage Parse(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return Parse(reader);
            }
        }

        /// <summary>
        /// Parses the specified reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        public static HttpMessage Parse(TextReader reader)
        {
            HttpMessage message = new HttpMessage(true);
            message.FromStream(reader);
            return message;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Writes the stream.
        /// </summary>
        /// <param name="writer">The writer.</param>
        protected virtual void WriteStream(TextWriter writer)
        {
            this.WriteHead(writer);
            this.WriteHeaders(writer);
            this.WriteBody(writer);
        }

        /// <summary>
        /// Writes the head.
        /// </summary>
        /// <param name="writer">The writer.</param>
        protected virtual void WriteHead(TextWriter writer)
        {
            if (this.IsRequest)
                writer.WriteLine("{0} {1} {2}", this.Directive, this.DirectiveObj, this.HttpVersion);
            else
                writer.WriteLine("{0} {1} {2}", this.HttpVersion, this.ResponseCode, this.ResponseCodeDesc);
        }

        /// <summary>
        /// Writes the headers.
        /// </summary>
        /// <param name="writer">The writer.</param>
        protected virtual void WriteHeaders(TextWriter writer)
        {
            // We'll be using the body length as the content length so remove it if it exists
            this.Headers.Remove("content-length");

            foreach (string key in this.Headers.Keys)
                writer.WriteLine("{0}: {1}", key, this.Headers[key]);

            writer.WriteLine("Content-Length: " + this.Body.Length);
            writer.WriteLine();
        }

        /// <summary>
        /// Writes the body.
        /// </summary>
        /// <param name="writer">The writer.</param>
        protected virtual void WriteBody(TextWriter writer)
        {
            writer.Write(this.Body);
        }

        /// <summary>
        /// Creates the message from the stream.
        /// </summary>
        /// <param name="reader">The reader.</param>
        protected virtual void FromStream(TextReader reader)
        {
            this.ReadHead(reader);
            this.ReadHeaders(reader);
            this.ReadBody(reader);
        }

        /// <summary>
        /// Reads the head.
        /// </summary>
        /// <param name="reader">The reader.</param>
        protected virtual void ReadHead(TextReader reader)
        {
            string line = reader.ReadLine();
            if (line == null)
                throw new ArgumentException("Invalid HttpMessage data.");

            string[] parts = line.Split(' ');
            if (parts.Length < 3)
                throw new ArgumentException("Invalid HttpMessage data.");

            this.IsResponse = parts[0].StartsWith("HTTP");
            if (this.IsResponse)
            {
                this.HttpVersion = parts[0];
                this.ResponseCode = int.Parse(parts[1]);
                this.ResponseCodeDesc = parts[2];
            }
            else
            {
                this.Directive = parts[0];
                this.DirectiveObj = parts[1];
                this.HttpVersion = parts[2];
            }
        }

        /// <summary>
        /// Reads the headers.
        /// </summary>
        /// <param name="reader">The reader.</param>
        protected virtual void ReadHeaders(TextReader reader)
        {
            string line = string.Empty;
            while ((line = reader.ReadLine()) != null)
            {
                if (line == string.Empty)
                    break;

                int index = line.IndexOf(":");
                if (index < 0) continue;

                //check for a space immediately after the colon
                int offset = index + 1;
                if (line.Length > offset && line[offset] == ' ')
                    offset++;

                string key = line.Substring(0, index);
                this.Headers[key] = line.Substring(offset);
            }
        }

        /// <summary>
        /// Reads the body.
        /// </summary>
        /// <param name="reader">The reader.</param>
        protected virtual void ReadBody(TextReader reader)
        {
            this.Body = reader.ReadToEnd();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Writes to a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void ToStream(Stream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                ToStream(writer);
            }
        }

        /// <summary>
        /// Writes to the stream.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public void ToStream(TextWriter writer)
        {
            this.WriteStream(writer);
        }

        /// <summary>
        /// Copies to another message.
        /// </summary>
        /// <param name="copy">The copy.</param>
        public void CopyTo(HttpMessage copy)
        {
            copy.Headers.Clear();
            foreach (string key in this.Headers)
                copy.Headers[key] = this.Headers[key];

            copy.Body = this.Body;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether this instance is request.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is request; otherwise, <c>false</c>.
        /// </value>
        public bool IsRequest
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is response.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is response; otherwise, <c>false</c>.
        /// </value>
        public bool IsResponse
        {
            get { return !this.IsRequest; }
            set { this.IsRequest = !value; }
        }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        public WebHeaderCollection Headers
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        public virtual string Body
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the length of the content.
        /// </summary>
        /// <value>
        /// The length of the content.
        /// </value>
        public virtual int ContentLength
        {
            get
            {
                if (string.IsNullOrEmpty(this.Body))
                    return 0;

                return this.Body.Length;
            }
        }

        /// <summary>
        /// Gets or sets the directive.
        /// </summary>
        /// <value>
        /// The directive.
        /// </value>
        public virtual string Directive
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the directive object.
        /// </summary>
        /// <value>
        /// The directive object.
        /// </value>
        public virtual string DirectiveObj
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the HTTP version.
        /// </summary>
        /// <value>
        /// The HTTP version.
        /// </value>
        public virtual string HttpVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the response code.
        /// </summary>
        /// <value>
        /// The response code.
        /// </value>
        public virtual int ResponseCode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the response code description.
        /// </summary>
        /// <value>
        /// The response code description.
        /// </value>
        public virtual string ResponseCodeDesc
        {
            get;
            set;
        }

        #endregion

    }
}
