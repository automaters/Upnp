using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using Automaters.Core;

namespace Automaters.Discovery.Upnp
{
    public class UpnpIcon : IXmlSerializable
    {

        #region IXmlSerializable Implementation

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.LocalName != "icon" && !reader.ReadToDescendant("icon"))
                throw new InvalidDataException();

            var dict = new Dictionary<string, Action>()
            {
                {"url", () => this.RelativeUrl = reader.ReadString()},
                {"mimetype", () => this.MimeType = reader.ReadString()},
                {"width", () => this.Width = reader.ReadElementContentAsInt()},
                {"height", () => this.Height = reader.ReadElementContentAsInt()},
                {"depth", () => this.Depth = reader.ReadElementContentAsInt()}
            };

            XmlHelper.ParseXml(reader, dict);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("icon");
            writer.WriteElementString("url", this.RelativeUrl);
            writer.WriteElementString("mimetype", this.MimeType);
            writer.WriteElementString("width", this.Width.ToString());
            writer.WriteElementString("height", this.Height.ToString());
            writer.WriteEndElement();
        }

        #endregion

        #region Properties

        public UpnpRoot Root
        {
            get { return this.Device.Root; }
        }

        public UpnpDevice Device
        {
            get;
            protected internal set;
        }

        public string RelativeUrl
        {
            get;
            set;
        }

        public Uri Url
        {
            get { return new Uri(this.Root.UrlBase, this.RelativeUrl); }
        }

        public string MimeType
        {
            get;
            set;
        }

        public virtual int Width
        {
            get;
            set;
        }
        
        public virtual int Height
        {
            get;
            set;
        }

        public virtual int Depth
        {
            get;
            set;
        }

        #endregion

    }
}
