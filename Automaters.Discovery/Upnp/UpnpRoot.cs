using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using Automaters.Core;
using System.Net;
using System.Linq.Expressions;

namespace Automaters.Discovery.Upnp
{
    public class UpnpRoot : IXmlSerializable
    {

        #region Object Overrides

        public override string ToString()
        {
            return (this.DeviceDescriptionUrl ?? this.UrlBase).ToString();
        }

        #endregion

        #region IXmlSerializable Implementation

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (!reader.ReadToDescendant("root"))
                throw new InvalidDataException();

            if (reader.HasAttributes)
                this.ConfigurationId = int.Parse(reader.GetAttribute("configId") ?? "-1");

            var dict = new Dictionary<string, Action>()
            {
                {"specVersion", () => 
                    {
                        XmlHelper.ParseXml(reader, new Dictionary<string, Action> {
                            {"major", () => this.UpnpMajorVersion = reader.ReadElementContentAsInt()},
                            {"minor", () => this.UpnpMinorVersion = reader.ReadElementContentAsInt()}
                        });
                    }
                },
                {"URLBase", () => this.UrlBase = new Uri(reader.ReadString())},
                {"device", () => {
                        this.RootDevice = new UpnpDevice();
                        this.RootDevice.ReadXml(reader);
                    }
                },
            };

            XmlHelper.ParseXml(reader, dict);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("root");
            writer.WriteAttributeString("xmlns", "urn:schemas-upnp-org:device-1-0");
            if (this.ConfigurationId != -1)
                writer.WriteAttributeString("configId", this.ConfigurationId.ToString());

            writer.WriteStartElement("specVersion");
            writer.WriteElementString("major", this.UpnpMajorVersion.ToString());
            writer.WriteElementString("minor", this.UpnpMinorVersion.ToString());
            writer.WriteEndElement();
            writer.WriteElementString("URLBase", this.UrlBase.ToString());

            if (this.RootDevice != null)
                this.RootDevice.WriteXml(writer);

            writer.WriteEndElement();
        }

        #endregion

        #region Properties

        public int UpnpMajorVersion
        {
            get;
            set;
        }

        public int UpnpMinorVersion
        {
            get;
            set;
        }

        public Uri DeviceDescriptionUrl
        {
            get;
            set;
        }

        public Uri UrlBase
        {
            get;
            set;
        }

        public int ConfigurationId
        {
            get;
            set;
        }

        private UpnpDevice _rootDevice;
        public UpnpDevice RootDevice
        {
            get { return this._rootDevice; }
            set
            {
                if (this._rootDevice != null)
                    this._rootDevice.Root = null;

                this._rootDevice = value;
                if (value != null)
                    value.Root = this;
            }
        }


        #endregion

        public IEnumerable<UpnpDevice> FindByDeviceType(UpnpType type)
        {
            var root = this.RootDevice;
            if(root == null)
                yield break;

            foreach(var device in root.FindByDeviceType(type))
            {
                yield return device;
            }
        }
    }
}
