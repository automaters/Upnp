using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;

namespace Upnp.Gena
{
    public class GenaServer
    {
        private readonly Dictionary<string, GenaSubscriptionList> _subscriptionLists = new Dictionary<string, GenaSubscriptionList>();
        private readonly Dictionary<string, GenaPropertySet> _propSets = new Dictionary<string, GenaPropertySet>();

        public IGenaSocket Server { get; private set; }

        public event EventHandler<GenaMessageEventArgs> GenaMessageReceived;
        public event EventHandler<GenaMessageEventArgs> GenaSubscribe;
        public event EventHandler<GenaMessageEventArgs> GenaUnsubscribe;
        public event EventHandler<GenaMessageEventArgs> GenaRenewal;

        #region constructors

        //TODO: this needs to supply a socket
        public GenaServer() 
            : this(null)
        {
        }

        public GenaServer(IGenaSocket server)
        {
            if (server == null) 
                throw new ArgumentNullException("server");

            this.Server = server;
            this.Server.GenaMessageReceived += OnGenaMessageReceived;
        }
        #endregion

        public void RegisterProperty(IGenaProperty property)
        {
            var key = property.ServiceId;
            if(!_propSets.ContainsKey(key))
                _propSets.Add(key, new GenaPropertySet());

            _propSets[key].Add(property);
            property.PropertyChanged += PropertyOnPropertyChanged;
        }

        private void PropertyOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var prop = sender as IGenaProperty;
            var serviceId = prop.ServiceId;
            var props = _propSets[serviceId];

            GenaSubscriptionList list;
            if (!_subscriptionLists.TryGetValue(serviceId, out list))
                return;

            list.Notify(props);
        }

        private void OnGenaUnsubscribeInternal(GenaMessageEventArgs e)
        {
            GenaSubscriptionList list;

            if (!_subscriptionLists.TryGetValue(e.ServiceId, out list))
                throw new GenaException(HttpStatusCode.BadRequest);

            list.Unsubscribe(e.Request, e.Response);

            OnGenaUnsubscribe(e);
        }

        private void OnGenaSubscribeInternal(GenaMessageEventArgs e)
        {
            OnGenaSubscribe(e);

            GenaSubscriptionList list;

            if (!_subscriptionLists.TryGetValue(e.ServiceId, out list))
            {
                list = new GenaSubscriptionList();
                _subscriptionLists.Add(e.ServiceId, list);
            }

            list.Subscribe(e.Request, e.Response);
        }

        private void OnGenaRenewalInternal(GenaMessageEventArgs e)
        {
            GenaSubscriptionList list;

            if (!_subscriptionLists.TryGetValue(e.ServiceId, out list))
            {
                list = new GenaSubscriptionList();
                _subscriptionLists.Add(e.ServiceId, list);
            }

            list.Renew(e.Request, e.Response);

            OnGenaRenewal(e);
        }

        private void OnGenaMessageReceived(object sender, GenaMessageEventArgs e)
        {
            OnGenaMessageReceived(e);

            if(e.Request.IsSubscribe)
            {
                OnGenaSubscribeInternal(e);
            }
            else if(e.Request.IsRenewal)
            {
                OnGenaRenewalInternal(e);
            }
            else if(e.Request.IsUnsubscribe)
            {
                OnGenaUnsubscribeInternal(e);
            }
        }

        protected virtual void OnGenaMessageReceived(GenaMessageEventArgs msg)
        {
            var handler = this.GenaMessageReceived;
            if (handler != null)
                handler(this, msg);
        }

        protected virtual void OnGenaSubscribe(GenaMessageEventArgs e)
        {
            var handler = this.GenaSubscribe;
            if (handler != null)
                handler(this, e);
        }

        protected virtual void OnGenaRenewal(GenaMessageEventArgs genaMessage)
        {
            var handler = this.GenaRenewal;
            if (handler != null)
                handler(this, genaMessage);
        }

        protected virtual void OnGenaUnsubscribe(GenaMessageEventArgs msg)
        {
            var handler = this.GenaUnsubscribe;
            if (handler != null)
                handler(this, msg);
        }
    }
}
