using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using Automaters.Core;
using System.IO;
using Automaters.Core.Collections;

namespace Automaters.Discovery.Upnp
{
    public class UpnpDevice : IXmlSerializable
    {

        public UpnpDevice()
        {
            this.Properties = new Dictionary<string, string>();
            this.IsEnabled = true;

            this.Devices = new CustomActionCollection<UpnpDevice>((device) =>
                {
                    device.Parent = this;
                    device.Root = this.Root;
                },
                (device) =>
                {
                    device.Parent = null;
                    device.Root = null;
                });
            this.Services = new CustomActionCollection<UpnpService>((service) => service.Device = this, (service) => service.Device = null);
            this.Icons = new CustomActionCollection<UpnpIcon>((icon) => icon.Device = this, (icon) => icon.Device = null);
        }

        #region Object Overrides 

        public override string ToString()
        {
            return string.Format("{0}/{1}", this.Type, this.UDN);
        }

        #endregion

        #region IXmlSerializable Implementation

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.LocalName != "device" && !reader.ReadToDescendant("device"))
                throw new InvalidDataException();
            
            // TODO: Serialize/Deserialize required Upnp properties separate from the default property
            var dict = new Dictionary<string, Action>()
            {
                {XmlHelper.DefaultParseElementName, () => 
                    {
                        if (!this.Properties.ContainsKey(reader.LocalName))
                            this.Properties.Add(reader.LocalName, reader.ReadString());
                    }
                },
                {"deviceList", () => XmlHelper.ParseXmlCollection(reader, this.Devices, "device", () => new UpnpDevice())},
                {"serviceList", () => XmlHelper.ParseXmlCollection(reader, this.Services, "service", () => new UpnpService())},
                {"iconList", () => XmlHelper.ParseXmlCollection(reader, this.Icons, "icon", () => new UpnpIcon())}
            };

            XmlHelper.ParseXml(reader, dict);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("device");

            if (!this.IsEnabled)
                writer.WriteAttributeString("enabled", "false");

            foreach (var pair in this.Properties)
                writer.WriteElementString(pair.Key, pair.Value);

            if (this.Devices.Count > 0)
            {
                writer.WriteStartElement("deviceList");
                foreach (var device in this.Devices)
                    device.WriteXml(writer);
                writer.WriteEndElement();
            }

            if (this.Services.Count > 0)
            {
                writer.WriteStartElement("serviceList");
                foreach (var service in this.Services)
                    service.WriteXml(writer);
                writer.WriteEndElement();
            }

            if (this.Icons.Count > 0)
            {
                writer.WriteStartElement("iconList");
                foreach (var icon in this.Icons)
                    icon.WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteEndElement(); //device
        }

        #endregion

        #region Properties

        public UpnpRoot Root
        {
            get;
            protected internal set;
        }

        public UpnpDevice Parent
        {
            get;
            protected set;
        }

        public UpnpDevice RootDevice
        {
            get { return this.Root.RootDevice; }
        }

        public bool IsEnabled
        {
            get;
            set;
        }

        public ICollection<UpnpDevice> Devices
        {
            get;
            private set;
        }

        public ICollection<UpnpService> Services
        {
            get;
            private set;
        }

        public ICollection<UpnpIcon> Icons
        {
            get;
            private set;
        }

        public UpnpType Type
        {
            get { return UpnpType.Parse(this.Properties["deviceType"]); }
            set { this.Properties["deviceType"] = value.ToString(); }
        }

        public string UDN
        {
            get { return this.Properties["UDN"]; }
            set { this.Properties["UDN"] = value; }
        }

        public Dictionary<string, string> Properties
        {
            get;
            private set;
        }

        public string FriendlyName
        {
            get { return this.GetProperty("friendlyName"); }
            set { this.Properties["friendlyName"] = value; }
        }

        public string Manufacturer
        {
            get { return this.GetProperty("manufacturer"); }
            set { this.Properties["manufacturer"] = value; }
        }

        public string ManufacturerUrl
        {
            get { return this.GetProperty("manufacturerURL"); }
            set { this.Properties["manufacturerURL"] = value; }
        }

        public string ModelDescription
        {
            get { return this.GetProperty("modelDescription"); }
            set { this.Properties["modelDescription"] = value; }
        }

        public string ModelName
        {
            get { return this.GetProperty("modelName"); }
            set { this.Properties["modelName"] = value; }
        }

        public string ModelNumber
        {
            get { return this.GetProperty("modelNumber"); }
            set { this.Properties["modelNumber"] = value; }
        }

        public string ModelUrl
        {
            get { return this.GetProperty("modelURL"); }
            set { this.Properties["modelURL"] = value; }
        }

        public string SerialNumber
        {
            get { return this.GetProperty("serialNumber"); }
            set { this.Properties["serialNumber"] = value; }
        }

        public string UPC
        {
            get { return this.GetProperty("UPC"); }
            set { this.Properties["UPC"] = value; }
        }

        private string GetProperty(string property)
        {
            if (this.Properties.ContainsKey(property))
                return this.Properties[property];

            return string.Empty;
        }
        #endregion

    }
}
