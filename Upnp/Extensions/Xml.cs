﻿using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace Upnp.Extensions
{
    public static class Xml
    {

        public static void WriteCollection<T>(this XmlWriter writer, ICollection<T> collection, string elementName, bool omitIfEmpty = false)
            where T : IXmlSerializable
        {
            if (omitIfEmpty && collection == null || collection.Count == 0)
                return;

            writer.WriteStartElement(elementName);
            if (collection != null)
            {
                foreach (var item in collection)
                    item.WriteXml(writer);
            }
            writer.WriteEndElement();
        }

    }
}
