using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Automaters.Discovery.Gena
{
    public class GenaSubscription
    {
        public IPEndPoint RemoteEndpoint { get; private set; }

        public Uri[] Callbacks { get; set; }

        public string SubscriptionId { get; private set; }

        private readonly object _eventKeyLock = new object();
        private uint _eventKey;

        /// <summary>
        /// Gets the DateTime that the subscription was last renewed
        /// </summary>
        public DateTime LastRenewal { get; private set; }

        /// <summary>
        /// Gets the DateTime that the subscription should be renewed by
        /// </summary>
        public DateTime NextRenewal
        {
            get
            {
                if (LastRenewal == DateTime.MinValue)
                    return DateTime.MaxValue;
                return LastRenewal.Add(RenewInterval);
            }
        }

        /// <summary>
        /// Gets or sets the timeout value by TimeSpan
        /// </summary>
        public TimeSpan RenewInterval { get; set; }

        public TimeSpan Timeout { get; set; }

        public GenaSubscription()
        {
            this.LastRenewal = DateTime.Now;
            this.RenewInterval = TimeSpan.MaxValue;
            this.SubscriptionId = "uuid:" + Guid.NewGuid();
            lock (_eventKeyLock)
            {
                this._eventKey = 0;    
            }
        }

        public void Renew()
        {
            LastRenewal = DateTime.Now;
        }

        public void Notify(GenaPropertySet props)
        {
            //TODO: need to probably wait for a small amount of time to reduce the number of network updates
            uint key;
            lock (_eventKeyLock)
            {
                key = this._eventKey;
                this._eventKey++;    
            }

            var callbacks = Callbacks;
            Task.Factory.StartNew(() =>
            {
                //TODO: we probably should support UDP also
                foreach (var uri in callbacks.Where(uri => uri.Scheme == "http"))
                {
                    try
                    {
                        
                        var request = (HttpWebRequest) WebRequest.Create(uri);
                        request.Method = "NOTIFY";
                        request.ContentType = "text/xml; charset=\"utf-8\"";

                        request.Headers.Add("NT", "upnp:event");
                        request.Headers.Add("NTS", "upnp:propchange");
                        request.Headers.Add("SID", string.Format("uuid:{0}", this.SubscriptionId));
                        request.Headers.Add("SEQ", key.ToString());

                        using (var stream = request.GetRequestStream())
                        {
                            var writer = new XmlTextWriter(stream, Encoding.UTF8);
                            props.WriteXml(writer);
                            writer.Close();
                        }

                        //send it
                        var response = (HttpWebResponse)request.GetResponse();

                        return;
                    }
                    catch (WebException)
                    {
                    }
                }
            });
        }
    }
}
