using System;

namespace Upnp.Gena
{
    public interface IGenaSocket
    {
        event EventHandler<GenaMessageEventArgs> GenaMessageReceived;
    }

    public class GenaMessageEventArgs : EventArgs
    {
        public string ServiceId { get; set; }
        public GenaMessage Request { get; private set; }
        public GenaMessage Response { get; private set; }

        public GenaMessageEventArgs(string serviceId)
        {
            this.ServiceId = serviceId;
            this.Request = GenaMessage.CreateRequest();
            this.Response = GenaMessage.CreateResponse();
        }
    }
}
