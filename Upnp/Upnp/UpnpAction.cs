using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using Upnp.Collections;
using Upnp.Extensions;
using Upnp.Xml;

namespace Upnp.Upnp
{
    public class UpnpAction : IXmlSerializable
    {

        public UpnpAction()
        {
            this.Arguments = new CustomActionCollection<UpnpArgument>((arg) => arg.Action = this, (arg) => arg.Action = null);
        }

        #region Object Overrides

        public override string ToString()
        {
            return string.Format("{0}({1})", this.Name, this.Arguments.Count);
        }

        #endregion

        #region IXmlSerializable Implementation

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            if (reader.LocalName != "action" && !reader.ReadToDescendant("action"))
                throw new InvalidDataException();

            var dict = new Dictionary<string, Action>()
            {
                {"name", () => this.Name = reader.ReadString()},
                {"argumentList", () => XmlHelper.ParseXmlCollection(reader, this.Arguments, "argument", () => new UpnpArgument())}
            };

            XmlHelper.ParseXml(reader, dict);
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteStartElement("action");
            writer.WriteElementString("name", this.Name);
            writer.WriteCollection(this.Arguments, "argumentList", true);
            writer.WriteEndElement();
        }

        #endregion

        #region Properties

        public string Name
        {
            get;
            protected internal set;
        }

        public ICollection<UpnpArgument> Arguments
        {
            get;
            private set;
        }

        #endregion

    }
}
