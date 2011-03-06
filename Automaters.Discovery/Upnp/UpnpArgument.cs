using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using Automaters.Core;

namespace Automaters.Discovery.Upnp
{
    public class UpnpArgument : IXmlSerializable
    {

        #region Object Overrides

        public override string ToString()
        {
            return string.Format("{0} [{1}]", this.Name, this.Direction);
        }

        #endregion

        #region IXmlSerializable Implementation

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            if (reader.LocalName != "argument" && !reader.ReadToDescendant("argument"))
                throw new InvalidDataException();

            var dict = new Dictionary<string, Action>()
            {
                {"name", () => this.Name = reader.ReadString()},
                {"direction", () => this.Direction = reader.ReadString()},
                {"relatedStateVariable", () => this.RelatedStateVariable = reader.ReadString()}
            };

            XmlHelper.ParseXml(reader, dict);
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteStartElement("argument");
            writer.WriteElementString("name", this.Name);
            writer.WriteElementString("direction", this.Direction);
            writer.WriteElementString("relatedStateVariable", this.RelatedStateVariable);
            writer.WriteEndElement();
        }

        #endregion

        #region Properties

        public UpnpAction Action
        {
            get;
            protected internal set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string Direction
        {
            get;
            set;
        }

        public string RelatedStateVariable
        {
            get;
            set;
        }

        public string Value
        {
            get;
            set;
        }

        #endregion

    }
}
