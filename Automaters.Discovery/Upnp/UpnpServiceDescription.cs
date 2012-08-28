using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using Automaters.Core;
using Automaters.Core.Extensions;
using System.Xml;
using Automaters.Core.Collections;

namespace Automaters.Discovery.Upnp
{
    public class UpnpServiceDescription : IXmlSerializable
    {

        public UpnpServiceDescription()
        {
            //this.Actions = new CustomActionCollection<UpnpAction>((action) => action.ServiceDescription = this, (action) => action.ServiceDescription = null);
            this.Actions = new List<UpnpAction>();
            this.Variables = new List<UpnpStateVariable>();
        }

        #region IXmlSerializable Implementation

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.LocalName != "scpd" && !reader.ReadToDescendant("scpd"))
                throw new InvalidDataException();

            var dict = new Dictionary<string, Action>()
            {
                {"specVersion", () => XmlHelper.ParseXml(reader, new Dictionary<string, Action>
                    {
                        {"major", () => this.VersionMajor = reader.ReadElementContentAsInt()},
                        {"minor", () => this.VersionMinor = reader.ReadElementContentAsInt()}
                    })
                },
                {"actionList", () => XmlHelper.ParseXmlCollection(reader, this.Actions, "action", () => new UpnpAction())},
                {"serviceStateTable", () => XmlHelper.ParseXmlCollection(reader, this.Variables, "stateVariable", () => new UpnpStateVariable())}
            };

            XmlHelper.ParseXml(reader, dict);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("scpd");
            writer.WriteAttributeString("xmlns", "urn:schemas-upnp-org:service-1-0");
            writer.WriteStartElement("specVersion");
            writer.WriteElementString("major", this.VersionMajor.ToString());
            writer.WriteElementString("minor", this.VersionMinor.ToString());
            writer.WriteEndElement();
            writer.WriteCollection(this.Actions, "actionList", true);
            writer.WriteCollection(this.Variables, "serviceStateTable", true);
            writer.WriteEndElement();

        }

        #endregion

        #region Properties

        public int VersionMajor
        {
            get;
            set;
        }

        public int VersionMinor
        {
            get;
            set;
        }

        public IList<UpnpAction> Actions
        {
            get;
            private set;
        }

        public IList<UpnpStateVariable> Variables
        {
            get;
            private set;
        }

        #endregion

    }
}
