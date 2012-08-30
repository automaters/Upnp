using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Automaters.Discovery.Gena;
using Automaters.Discovery.Ssdp;

namespace Automaters.Discovery.Upnp
{
    public class UpnpServer : IDisposable
    {
        public UpnpRoot Root { get; private set; }

        private readonly SsdpServer _ssdp;
        private readonly GenaServer _gena;
        private readonly List<SsdpAnnouncer> _announcers = new List<SsdpAnnouncer>();

#if DEBUG
        private readonly ushort AdvertisementAge = 30;
#else
        private readonly ushort AdvertisementAge = 1800;
#endif

        public UpnpServer(UpnpRoot root, SsdpServer ssdp = null, GenaServer gena = null)
        {
            this.Root = root;
            this._ssdp = ssdp ?? new SsdpServer();
            this._gena = gena ?? new GenaServer();

            BuildAdvertisements();
        }

        private void CreateAdvertisement(string notificationType, string usn)
        {
            var ad = this._ssdp.CreateAnnouncer();
            ad.NotificationType = notificationType;
            ad.USN = usn;
            ad.Location = this.Root.DeviceDescriptionUrl.ToString();
            ad.MaxAge = this.AdvertisementAge;
            _announcers.Add(ad);
        }
  
        private void BuildAdvertisements()
        {
            CreateAdvertisement("upnp:rootdevice", string.Format ("{0}::upnp:rootdevice", this.Root.RootDevice.UDN));

            BuildAdvertisementsForDevice(this.Root.RootDevice);
        }
  
        private void BuildAdvertisementsForDevice(UpnpDevice device)
        {
            var notificationType = device.UDN;
            CreateAdvertisement (notificationType, notificationType);

            var type = device.Type.ToString();
            CreateAdvertisement (type, string.Format ("{0}::{1}", device.UDN, type));

            foreach (var service in device.Services)
                BuildAdvertisementsForService (service);

            foreach (var child in device.Devices)
                BuildAdvertisementsForDevice(child);
        }
  
        private void BuildAdvertisementsForService(UpnpService service)
        {
            var type = service.Type.ToString ();
            CreateAdvertisement (type, string.Format ("{0}::{1}", service.Device.UDN, type));
        }
  
        public void StopListening()
        {
            this._ssdp.StopListening();

            foreach (var announcer in _announcers)
                announcer.Shutdown();
        }

        public void StartListening(params IPEndPoint[] remoteEps)
        {
            this._ssdp.StartListening(remoteEps);

            foreach (var announcer in _announcers)
                announcer.Start();
        }

        public void Dispose()
        {
            this._ssdp.Dispose();
        }
    }
}
