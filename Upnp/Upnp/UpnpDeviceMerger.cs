using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Upnp.Upnp
{
    public class UpnpDevicePairInspector
    {
        public void Compare(UpnpDevice to, UpnpDevice from)
        {
            if(from == null)
                return;

            var fromChildren = from.EnumerateDevices().ToList();
            var toChildren = to.EnumerateDevices().ToList();

            foreach (var toChild in toChildren)
            {
                var fromChild = FindMatchFor(toChild, fromChildren);
                if (fromChild == null)
                {
                    //fromChild doesnt exist so was removed
                    DeviceRemoved(toChild);
                    continue;
                }

                // remove the child so we can see if there are any new ones at the end.
                fromChildren.Remove(fromChild);
                CompareDevices(toChild, fromChild);
            }

            //these devices were not found in the 'to' device so they are new
            if (fromChildren.Count > 0)
                ProcessAddedDevices(to, fromChildren);
        }

        protected virtual void CompareDevices(UpnpDevice to, UpnpDevice from)
        {
            var fromServices = from.Services.ToList();
            var toServices = to.Services.ToList();

            foreach (var toService in toServices)
            {
                var fromService = FindMatchFor(toService, fromServices);
                if (fromService == null)
                {
                    ServiceRemoved(to, toService);
                    continue;
                }

                fromServices.Remove(fromService);
                CompareService(toService, fromService);
            }

            if(fromServices.Count > 0)
                ServicesAdded(to, fromServices);
        }

        protected virtual void CompareService(UpnpService to, UpnpService from)
        {
        }

        private void ProcessAddedDevices(UpnpDevice to, List<UpnpDevice> addedDevices)
        {
            var toChildren = to.EnumerateDevices().ToList();

            foreach (var device in addedDevices)
            {
                //cant update root device
                if(device.Parent == null)
                    continue;

                //find the original 'to' parent
                var toChild = FindMatchFor(device.Parent, toChildren);
                if(toChild == null)
                    continue;


                DeviceAdded(toChild, device);
            }
        }

        protected virtual void DeviceAdded(UpnpDevice parent, UpnpDevice device)
        {
        }

        protected virtual void ServicesAdded(UpnpDevice device, List<UpnpService> services)
        {
        }

        protected virtual void ServiceRemoved(UpnpDevice device, UpnpService service)
        {
        }

        protected virtual void DeviceRemoved(UpnpDevice device)
        {
        }

        protected virtual UpnpDevice FindMatchFor(UpnpDevice device, IEnumerable<UpnpDevice> fromChildren)
        {
            return fromChildren.FirstOrDefault(child => device.Type == child.Type);
        }

        protected virtual UpnpService FindMatchFor(UpnpService service, IEnumerable<UpnpService> services)
        {
            return services.FirstOrDefault(child => service.Id == child.Id);
        }
    }
}
