using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Automaters.Core.Net;

namespace Automaters.Discovery.Gena
{
    public class GenaMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenaMessage"/> class.
        /// </summary>
        /// <param name="isRequest">if set to <c>true</c> [is request].</param>
        protected GenaMessage(bool isRequest)
        {
            this.Message = isRequest ? HttpMessage.CreateRequest() : HttpMessage.CreateResponse();
        }

        /// <summary>
        /// Creates the request.
        /// </summary>
        /// <returns></returns>
        public static GenaMessage CreateRequest()
        {
            return new GenaMessage(true);
        }

        /// <summary>
        /// Creates the response.
        /// </summary>
        /// <returns></returns>
        public static GenaMessage CreateResponse()
        {
            return new GenaMessage(false);
        }

        public HttpMessage Message { get; protected set; }

        public string EventUrl
        {
            get { return this.Message.DirectiveObj; }
            set { this.Message.DirectiveObj = value; }
        }

        public string SubscriptionId
        {
            get { 
                var sid = this.Message.Headers["sid"]; 
                if(sid == null)
                    return null;
                
                return sid.Substring("uuid:".Length);
                
            }
            set
            {
                if(value == null)
                {
                    this.Message.Headers.Remove("sid");
                    return;
                }

                if (value.StartsWith("uuid:"))
                    this.Message.Headers["sid"] = value;
                else
                    this.Message.Headers["sid"] = string.Format("uuid:{0}", value);
            }
        }

        public string NotificationType
        {
            get { return this.Message.Headers["nt"]; }
            set { this.Message.Headers["nt"] = value; }
        }

        public Uri[] Callbacks
        {
            get
            {
                var callbacks = this.Message.Headers["callback"];
                if(string.IsNullOrEmpty(callbacks))
                    return null;

                return callbacks.Split(',').Select(cb => cb.Trim().Trim('<', '>')).Select(cb => new Uri(cb)).ToArray();
            }
            set
            {
                if(value == null)
                {
                    this.Message.Headers.Remove("callback");
                    return;
                }

                var sb = new StringBuilder();

                foreach (var callback in value)
                {
                    sb.AppendFormat("<{0}>, ", callback);
                }

                if (sb.Length > 0)
                    sb.Length -= 2;

                this.Message.Headers["callback"] = sb.ToString();
            }

        }
        
        public TimeSpan? Timeout
        {
            get
            {
                var value = this.Message.Headers["timeout"];

                if (value == null)
                    return null;

                string lower = value.ToLower();
                if (lower == "infinite" || lower == "second-infinite")
                    return TimeSpan.MaxValue;

                if (lower.StartsWith("second-"))
                {
                    value = value.Substring(7);
                    int seconds;
                    if (int.TryParse(value, out seconds))
                        return TimeSpan.FromSeconds(seconds);
                }
                
                return null;
            }
            set
            {
                if(value == null)
                {
                    this.Message.Headers.Remove("timeout");
                    return;
                }

                var timeout = value.Value;

                string timeoutString;
                if (timeout == TimeSpan.MaxValue) //infinite
                {
                    timeoutString = "Infinite";
                }
                else
                {
                    timeoutString = "Second-" + timeout.TotalSeconds;
                }

                this.Message.Headers["timeout"] = timeoutString;
            }
        }

        public string UserAgent
        {
            get { return this.Message.Headers["server"]; }
            set
            {
                if(string.IsNullOrEmpty(value ))
                {
                    this.Message.Headers.Remove("server");
                    return;
                }

                this.Message.Headers["server"] = value;
            }
        }

        public bool IsSubscribe
        {
            get { return this.Message.Directive.ToLower() == "subscribe" && string.IsNullOrEmpty(this.Message.Headers["sid"]); }
            
        }

        public bool IsRenewal
        {
            get { return this.Message.Directive.ToLower() == "subscribe" && !string.IsNullOrEmpty(this.Message.Headers["sid"]); }
        }

        public bool IsUnsubscribe
        {
            get { return this.Message.Directive.ToLower() == "unsubscribe"; }

        }

        public DateTime? Date
        {
            get
            {
                if (string.IsNullOrEmpty(this.Message.Headers["date"]))
                    return null;

                DateTime date;

                if (DateTime.TryParse(this.Message.Headers["date"], out date))
                    return date;

                return null;
            }
            set 
            { 
                if(value == null)
                {
                    this.Message.Headers.Remove("date");
                    return;
                }

                this.Message.Headers["date"] = value.Value.ToString("r");
            }
        }
    }
}
