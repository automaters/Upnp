using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Automaters.Discovery.Ssdp;

namespace Automaters.Discovery.Upnp
{
    public class UpnpServer : IDisposable
    {
        public UpnpRoot Root { get; private set; }

        private readonly SsdpServer _ssdp;
        private readonly ushort AdvertisementAge = 1800;

        public UpnpServer(UpnpRoot root)
        {
            this.Root = root;
            this._ssdp = new SsdpServer();
            
            BuildAdvertisements();
        }

        private void CreateAdvertisement(string notificationType, string usn)
        {
            var ad = this._ssdp.CreateAnnouncer();
            ad.NotificationType = notificationType;
            ad.USN = usn;
            ad.Location = this.Root.DeviceDescriptionUrl.ToString();
            ad.MaxAge = this.AdvertisementAge;
            ad.Start();
        }

        private void BuildAdvertisements()
        {
            CreateAdvertisement("upnp:rootdevice", string.Format ("uuid:{0}::upnp:rootdevice", this.Root.RootDevice.UDN));

            BuildAdvertisementsForDevice(this.Root.RootDevice);
        }

        private void BuildAdvertisementsForDevice(UpnpDevice device)
        {
            var notificationType = "uuid:" + device.UDN;
            CreateAdvertisement (notificationType, notificationType);

            var type = device.Type.ToString();
            CreateAdvertisement (type, string.Format ("uuid:{0}::{1}", device.UDN, type));

            foreach (var service in device.Services)
                BuildAdvertisementsForService (service);

            foreach (var child in device.Devices)
                BuildAdvertisementsForDevice(child);
        }

        private void BuildAdvertisementsForService(UpnpService service)
        {
            var type = service.Type.ToString ();
            CreateAdvertisement (type, string.Format ("uuid:{0}::{1}", service.Device.UDN, type));
        }

        public void StartListening(params IPEndPoint[] remoteEps)
        {
            this._ssdp.StartListening(remoteEps);
        }

        public void Dispose()
        {
            this._ssdp.Dispose();
        }
    }
}
