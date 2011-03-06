using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Automaters.Core.Extensions;
using System.Xml.Serialization;

namespace Automaters.Core
{
    public static class XmlHelper
    {

        public const string DefaultParseElementName = "@default";

        public static void ParseXml(XmlReader reader, Dictionary<string, Action> actions, string endElement = null)
        {
            // If no end element was specified then just read until the end tag of our current element
            if (string.IsNullOrEmpty(endElement))
            {
                // Make sure we can actually figure out the end element
                if (!reader.IsStartElement())
                    throw new ArgumentException();

                endElement = reader.LocalName;
            }
            
            // Nothing to read so just return
            if (reader.LocalName == endElement && reader.IsEmptyElement)
                return;

            // Check to see if there's a default action to perform if no specific action is found
            Action defaultAction = null;
            actions.TryGetValue(DefaultParseElementName, out defaultAction);

            // Read until the end (or we find our end tag)
            while (reader.Read())
            {
                // If this is not a start element then we can skip it
                if (!reader.IsStartElement())
                {
                    // If we found our end element then exit
                    if (reader.LocalName == endElement)
                        break;

                    continue;
                }

                // Try to find the action we should use for this element
                Action action = null;
                if (actions.TryGetValue(reader.LocalName, out action))
                    action();
                else if (defaultAction != null)
                    defaultAction();
            }
        }

        public static void ParseXmlCollection<T>(XmlReader reader, ICollection<T> collection, string elementName, Func<T> creator)
            where T : IXmlSerializable
        {
            Dictionary<string, Action> dict = new Dictionary<string, Action>()
            {
                {elementName, () =>
                    {
                        T value = creator();
                        collection.Add(value);
                        value.ReadXml(reader);
                    }
                }
            };

            ParseXml(reader, dict);
        }

    }
}
