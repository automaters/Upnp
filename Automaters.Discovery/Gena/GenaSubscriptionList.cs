using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Automaters.Core.Timers;
using Automaters.Discovery.Upnp;

namespace Automaters.Discovery.Gena
{
    class GenaSubscriptionList
    {
        private readonly Dictionary<string, GenaSubscription> _subscriptions = new Dictionary<string, GenaSubscription>();
        private readonly TimeoutDispatcher _dispatcher = new TimeoutDispatcher();

        public void Notify(GenaPropertySet props)
        {
            GenaSubscription[] temp;
            lock (_subscriptions)
            {
                temp = _subscriptions.Values.ToArray();
            }

            Parallel.ForEach(temp, subscription => subscription.Notify(props));
        }

        public void Subscribe(GenaMessage request, GenaMessage response)
        {
            VerifyHeaders(request);
            
            //subscribe
            var sub = new GenaSubscription();

            lock (_subscriptions)
            {
                this._subscriptions.Add(sub.SubscriptionId, sub);
            }

            sub.Callbacks = request.Callbacks;

            _dispatcher.Add(() => CheckSubscription(sub), sub.RenewInterval);


            //update timeout value if one is present
            RespondToSubscribe(request, response, sub);

            /**
             * SUBSCRIBE:
             * 
             * SUBSCRIBE publisher path HTTP/1.1
             *  HOST: publisher host:publisher port
             *  USER-AGENT: OS/version UPnP/1.1 product/version
             *  CALLBACK: <delivery URL>
             *  NT: upnp:event
             *  TIMEOUT: Second-requested subscription duration
             *
             * SUBSCRIBE RESPONSE:
             * 
             *  HTTP/1.1 200 OK
             *  DATE: when response was generated
             *  SERVER: OS/version UPnP/1.1 product/version
             *  SID: uuid:subscription-UUID
             *  CONTENT-LENGTH: 0
             *  TIMEOUT: Second-actual subscription duration
             * 
             * RENEWAL:
             *  SUBSCRIBE publisher path HTTP/1.1
             *  HOST: publisher host:publisher port
             *  SID: uuid:subscription UUID
             *  TIMEOUT: Second-requested subscription duration
             *  
             * RENEWAL RESPONSE is the same as SUBSCRIBE RESPONSE 
             * 
            */
        }

        public void Unsubscribe(GenaMessage request, GenaMessage response)
        {
            VerifyHeaders(request);

            var sid = request.SubscriptionId;

            lock (_subscriptions)
            {
                if (!_subscriptions.ContainsKey(sid))
                    throw new GenaException(HttpStatusCode.PreconditionFailed);
                
                _subscriptions.Remove(sid);
            }
        }

        public void Renew(GenaMessage request, GenaMessage response)
        {
            GenaSubscription sub;

            var sid = request.SubscriptionId;

            //renew
            lock (_subscriptions)
            {
                if (!_subscriptions.TryGetValue(sid, out sub))
                    throw new GenaException(HttpStatusCode.PreconditionFailed);
            }

            sub.Renew();

            RespondToSubscribe(request, response, sub);
        }

        private static void VerifyHeaders(GenaMessage message)
        {
            if (message == null)
                throw new GenaException(HttpStatusCode.BadRequest);

            var sid = message.SubscriptionId;
            var nt = message.NotificationType;
            var callbacks = message.Callbacks;

            //If a SID header field and one of NT or CALLBACK header fields are present.
            if (!string.IsNullOrEmpty(sid) && (!string.IsNullOrEmpty(nt) || callbacks != null))
                throw new GenaException(HttpStatusCode.BadRequest);
        }

        private static void RespondToSubscribe(GenaMessage request, GenaMessage response, GenaSubscription sub)
        {
            var timeout = request.Timeout;
            if (timeout.HasValue)
                sub.Timeout = timeout.Value;

            response.SubscriptionId = sub.SubscriptionId;
            response.Timeout = sub.Timeout;
            response.Date = DateTime.Now;
            response.UserAgent = String.Format("{0}/{1} UPnP/1.1 UPnPLib/1.1", Environment.OSVersion.Platform, Environment.OSVersion.Version); ;
        }

        private void CheckSubscription(GenaSubscription sub)
        {
            var time = sub.NextRenewal.Subtract(DateTime.Now);

            if (time.TotalMilliseconds <= 0)
            {
                //expired so remove the sub
                lock (_subscriptions)
                {
                    this._subscriptions.Remove(sub.SubscriptionId);
                }
            }
            else
            {
                //check again once our time is up
                _dispatcher.Add(() => CheckSubscription(sub), time);
            }
        }
    }
}
