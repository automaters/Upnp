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
        protected readonly List<SsdpAnnouncer> Announcers = new List<SsdpAnnouncer>();
        
        public UpnpServer(UpnpRoot root, SsdpServer ssdp = null, GenaServer gena = null)
        {
            this.Root = root;
            this.Root.ChildDeviceAdded += OnChildDeviceAdded;
            this.SsdpServer = ssdp ?? new SsdpServer();
            this.GenaServer = gena ?? new GenaServer();

            BuildAdvertisements();
        }

        private void OnChildDeviceAdded(object sender, EventArgs<UpnpDevice> eventArgs)
        {
            BuildAdvertisementsForDevice(eventArgs.Value);
        }

        protected SsdpAnnouncer CreateAdvertisement(string notificationType, string usn)
        {
            var ad = this.SsdpServer.CreateAnnouncer();
            ad.NotificationType = notificationType;
            ad.USN = usn;
            ad.Location = this.Root.DeviceDescriptionUrl.ToString();
            Announcers.Add(ad);
            
            if(this.SsdpServer.IsListening)
                ad.Start();

            return ad;
        }

        protected void BuildAdvertisements()
        {
            CreateAdvertisement("upnp:rootdevice", string.Format("{0}::upnp:rootdevice", this.Root.RootDevice.UDN));

            BuildAdvertisementsForDevice(this.Root.RootDevice);
        }

        private void BuildAdvertisementsForDevice(UpnpDevice device)
        {
            var notificationType = device.UDN;
            var type = device.Type.ToString();

            var ad1 = CreateAdvertisement(notificationType, notificationType);
            var ad2 = CreateAdvertisement(type, string.Format("{0}::{1}", device.UDN, type));

            SetupOnRemovedHandler(device, ad1, ad2);

            foreach (var service in device.Services)
                BuildAdvertisementForService(service);

            foreach (var child in device.Devices)
                BuildAdvertisementsForDevice(child);
        }

        private void SetupOnRemovedHandler(UpnpDevice device, SsdpAnnouncer ad1, SsdpAnnouncer ad2)
        {
            EventHandler<EventArgs<UpnpDevice>> onRemoved = null;
            onRemoved = (sender, args) =>
            {
                ad1.Shutdown();
                ad2.Shutdown();

                this.Announcers.Remove(ad1);
                this.Announcers.Remove(ad2);

                device.Removed -= onRemoved;
            };

            device.Removed += onRemoved;
        }

        private void BuildAdvertisementForService(UpnpService service)
        {
            var ad = CreateAdvertisement (service.Type.ToString (), string.Format ("{0}::{1}", service.Device.UDN, service.Type));

            EventHandler<EventArgs<UpnpService>> onRemoved = null;
            onRemoved = (sender, args) =>
            {
                ad.Shutdown();

                this.Announcers.Remove(ad);

                service.Removed -= onRemoved;
            };

            service.Removed += onRemoved;
        }
  
        public void StopListening()
        {
            this.SsdpServer.StopListening();

            foreach (var announcer in Announcers)
                announcer.Shutdown();
        }

        public void StartListening(params IPEndPoint[] remoteEps)
        {
            this.SsdpServer.StartListening(remoteEps);

            foreach (var announcer in Announcers)
                announcer.Start();
        }

        public void Dispose()
        {
            this.Root.ChildDeviceAdded -= OnChildDeviceAdded;
            this.SsdpServer.Dispose();
        }
    }
}
