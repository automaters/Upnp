using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Upnp.Extensions;
using Upnp.Gena;
using Upnp.Ssdp;

namespace Upnp.Upnp
{
    public class UpnpServer : IDisposable
    {
        public UpnpRoot Root { get; private set; }

        protected readonly SsdpServer SsdpServer;
        protected readonly GenaServer GenaServer;
        private readonly List<SsdpAnnouncer> _announcers = new List<SsdpAnnouncer>();
        
        public UpnpServer(UpnpRoot root, SsdpServer ssdp = null, GenaServer gena = null)
        {
            this.Root = root;
            this.SsdpServer = ssdp ?? new SsdpServer();
            this.GenaServer = gena ?? new GenaServer();

            _announcers.AddRange(BuildAdvertisements());
        }

        protected SsdpAnnouncer CreateAdvertisement(string notificationType, string usn)
        {
            var ad = this.SsdpServer.CreateAnnouncer();
            ad.NotificationType = notificationType;
            ad.USN = usn;
            ad.Location = this.Root.DeviceDescriptionUrl.ToString();
            return ad;
        }

        protected virtual IEnumerable<SsdpAnnouncer> BuildAdvertisements()
        {
            return BuildAdvertisementsForDevice(this.Root.RootDevice)
                    .Add(() => CreateAdvertisement("upnp:rootdevice", string.Format ("{0}::upnp:rootdevice", this.Root.RootDevice.UDN)));
        }

        private IEnumerable<SsdpAnnouncer> BuildAdvertisementsForDevice(UpnpDevice device)
        {
            var notificationType = device.UDN;
            var type = device.Type.ToString();

            var serviceAnnouncers = from service in device.Services
                                    select BuildAdvertisementForService(service);

            var deviceAnnouncers = from child in device.Devices
                                   from announcer in BuildAdvertisementsForDevice(child)
                                   select announcer;

            return deviceAnnouncers.Concat(serviceAnnouncers).Add(() => CreateAdvertisement(notificationType, notificationType), () => CreateAdvertisement (type, string.Format ("{0}::{1}", device.UDN, type)));
        }

        private SsdpAnnouncer BuildAdvertisementForService(UpnpService service)
        {
            return CreateAdvertisement (service.Type.ToString (), string.Format ("{0}::{1}", service.Device.UDN, service.Type));
        }
  
        public void StopListening()
        {
            this.SsdpServer.StopListening();

            foreach (var announcer in _announcers)
                announcer.Shutdown();
        }

        public void StartListening(params IPEndPoint[] remoteEps)
        {
            this.SsdpServer.StartListening(remoteEps);

            foreach (var announcer in _announcers)
                announcer.Start();
        }

        public void Dispose()
        {
            this.SsdpServer.Dispose();
        }
    }
}
