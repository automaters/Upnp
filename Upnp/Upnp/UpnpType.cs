using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Automaters.Discovery.Upnp
{
    public class UpnpType : IEquatable<UpnpType>
    {

        #region Constructors

        public UpnpType()
            : this(string.Empty, string.Empty, string.Empty, new Version())
        {
        }

        public UpnpType(string domain, string kind, string type, Version version)
        {
            this.Domain = domain;
            this.Kind = kind;
            this.Type = type;
            this.Version = version;
        }

        public static UpnpType Parse(string urn)
        {
            string[] parts = urn.Split(':');
            if (parts.Length != 5)
                throw new ArgumentException();

            return new UpnpType(parts[1], parts[2], parts[3], Version.Parse(parts[4] + ".0"));
        }

        #endregion

        #region Object Overrides

        public override bool Equals(object obj)
        {
            UpnpType type = obj as UpnpType;
            if (type == null)
                return false;

            return this.Domain == type.Domain &&
                this.Kind == type.Kind &&
                this.Type == type.Type &&
                this.Version == type.Version;
        }

        public override int GetHashCode()
        {
            return this.Domain.GetHashCode() ^ this.Kind.GetHashCode() ^ this.Type.GetHashCode() ^ this.Version.GetHashCode();
        }

        public bool Equals(UpnpType other)
        {
            if((object)other == null)
                return false;

            return (other.Domain == this.Domain && 
                    other.Kind == this.Kind && 
                    other.Type == this.Type && 
                    other.Version == this.Version);
        }

        public static bool operator ==(UpnpType a, UpnpType b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(UpnpType a, UpnpType b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return string.Format("urn:{0}:{1}:{2}:{3}", this.Domain, this.Kind, this.Type, this.VersionString);
        }

        #endregion

        #region Properties

        public string Domain
        {
            get;
            set;
        }

        public string Kind
        {
            get;
            set;
        }

        public string Type
        {
            get;
            set;
        }

        public Version Version
        {
            get;
            set;
        }

        public string VersionString
        {
            get { return (this.Version.Minor == 0 ? this.Version.Major.ToString() : this.Version.ToString(2)); }
            set { this.Version = Version.Parse(value + ".0"); }
        }

        #endregion

    }
}
