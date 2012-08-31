using System;

namespace Upnp.Upnp
{
    public class UniqueDeviceName
    {
        public Guid Uuid { get; private set; }

        public override string ToString()
        {
            return "uuid:" + this.Uuid;
        }

        public static implicit operator UniqueDeviceName(string udn)
        {
            return Parse(udn);
        }

        public static implicit operator String(UniqueDeviceName udn)
        {
            return udn.ToString();
        }

        public static UniqueDeviceName Parse(string udn)
        {
            return new UniqueDeviceName {Uuid = udn.StartsWith("uuid:") ? new Guid(udn.Substring(5)) : new Guid(udn)};
        }

        public static bool operator ==(UniqueDeviceName udn, string udn2)
        {
            if((object)udn == null && udn2 == null)
                return true;

            if((object)udn == null)
                return false;

            return udn.ToString() == udn2 || udn.Uuid.ToString() == udn2;
        }

        public static bool operator !=(UniqueDeviceName udn, string udn2)
        {
            return !(udn == udn2);
        }

        public override int GetHashCode()
        {
            return this.Uuid.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this == (obj as UniqueDeviceName);
        }
    }
}
