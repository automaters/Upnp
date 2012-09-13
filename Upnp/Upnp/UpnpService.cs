using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using Upnp.Xml;

namespace Upnp.Upnp
{
    public class UpnpService : IXmlSerializable
    {
        private UpnpDevice _device;

        #region Object Overrides

        public override string ToString()
        {
            return string.Format("{0}/{1}/{2}", this.Type, this.Device.Type, this.Device.UDN);
        }

        #endregion

        #region IXmlSerializable Implementation

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.LocalName != "service" && !reader.ReadToDescendant("service"))
                throw new InvalidDataException();

            var dict = new Dictionary<string, Action>()
            {
                {"serviceType", () => this.Type = UpnpType.Parse(reader.ReadString())},
                {"serviceId", () => this.Id = reader.ReadString()},
                {"SCPDURL", () => this.RelativeScpdUrl = reader.ReadString()},
                {"controlURL", () => this.RelativeControlUrl = reader.ReadString()},
                {"eventSubURL", () => this.RelativeEventUrl = reader.ReadString()}
            };

            XmlHelper.ParseXml(reader, dict);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("service");
            writer.WriteElementString("serviceType", this.Type.ToString());
            writer.WriteElementString("serviceId", this.Id);
            writer.WriteElementString("SCPDURL", this.RelativeScpdUrl);
            writer.WriteElementString("controlURL", this.RelativeControlUrl);
            writer.WriteElementString("eventSubURL", this.RelativeEventUrl);
            writer.WriteEndElement();
        }

        #endregion

        #region Events

        public event EventHandler<EventArgs<UpnpService>> Removed;
        public event EventHandler<EventArgs<UpnpService>> Added;

        protected void OnAdded()
        {
            var handler = Added;
            if (handler != null)
                handler(this, new EventArgs<UpnpService>(this));
        }

        protected void OnRemoved()
        {
            var handler = this.Removed;
            if (handler != null)
                handler(this, new EventArgs<UpnpService>(this));
        }
        #endregion

        #region Properties

        public UpnpRoot Root
        {
            get { return this.Device.Root; }
        }

        public UpnpDevice Device
        {
            get { return _device; }
            protected internal set
            {
                if(_device == value)
                    return;

                _device = value;

                if(_device == null)
                {
                    OnRemoved();
                }
                else
                {
                    OnAdded();
                }
            }
        }

        public UpnpType Type
        {
            get;
            set;
        }

        public string Id
        {
            get;
            set;
        }

        public string RelativeScpdUrl
        {
            get;
            set;
        }

        public Uri ScpdUrl
        {
            get { return new Uri(this.Root.UrlBase, this.RelativeScpdUrl); }
        }

        public string RelativeControlUrl
        {
            get;
            set;
        }

        public Uri ControlUrl
        {
            get { return new Uri(this.Root.UrlBase, this.RelativeControlUrl); }
        }

        public string RelativeEventUrl
        {
            get;
            set;
        }

        public Uri EventUrl
        {
            get { return new Uri(this.Root.UrlBase, this.RelativeEventUrl); }
        }

        #endregion

    }
}
