using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Upnp.Upnp
{
    public class UpnpDeviceMerger
    {
        public void Merge(UpnpDevice to, UpnpDevice from)
        {
            if(from == null)
                return;

            //always merge from right into left
            var fromChildren = from.EnumerateDevices().ToArray();

            foreach(var toChild in to.EnumerateDevices())
            {
                var fromChild = FindMatchFor(toChild, fromChildren);
                if (fromChild == null)
                    continue;

                MergeChildDevice(toChild, fromChild);

                foreach (var toService in toChild.Services)
                {
                    var fromService = FindMatchFor(toService, fromChild.Services);
                    if (fromService == null)
                        continue;

                    MergeChildDeviceServices(toService, fromService);
                }
            }
        }

        protected virtual void MergeChildDeviceServices(UpnpService to, UpnpService from)
        {
        }

        protected virtual void MergeChildDevice(UpnpDevice to, UpnpDevice from)
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
