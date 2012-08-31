using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using Automaters.Core;

namespace Automaters.Discovery.Upnp
{
    public class UpnpStateVariable : IXmlSerializable
    {

        #region Object Overrides

        public override string ToString()
        {
            return string.Format("{0} [{1}]", this.Name, this.DataType);
        }

        #endregion

        #region IXmlSerializable Implementation

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            if (reader.LocalName != "stateVariable" && !reader.ReadToDescendant("stateVariable"))
                throw new InvalidDataException();

            if (reader.HasAttributes)
                this.SendEvents = ((reader.GetAttribute("sendEvents") ?? "no") == "yes");
            
            var dict = new Dictionary<string, Action>()
            {
                {"name", () => this.Name = reader.ReadString()},
                {"dataType", () => this.DataType = reader.ReadString()}
            };

            XmlHelper.ParseXml(reader, dict);
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteStartElement("stateVariable");
            writer.WriteElementString("name", this.Name);
            writer.WriteElementString("dataType", this.DataType);
            writer.WriteEndElement();
        }

        #endregion

        #region Properties

        public bool SendEvents
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string DataType
        {
            get;
            set;
        }

        #endregion

    }
}
