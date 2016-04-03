using System;
using Newtonsoft.Json;

namespace CKAN.Factorio.Version
{
    [Serializable]
    [JsonConverter(typeof(JsonSimpleStringConverter))]
    public abstract class AbstractVersion : IEquatable<AbstractVersion>, IComparable<AbstractVersion>, IComparable
    {

        protected readonly System.Version version;

        protected AbstractVersion(System.Version version)
        {
            this.version = (System.Version) version?.Clone();
        }

        public bool IsGreaterThan(AbstractVersion other)
        {
            return CompareTo(other) > 0;
        }

        public bool Equals(AbstractVersion other)
        {
            if ((object) other == null)
            {
                return false;
            }
            if (version == null)
            {
                return false;
            }
            return version.Equals(other.version);
        }

        public int CompareTo(AbstractVersion other)
        {
            bool otherIsNull = (object) other == null || (object) other.version == null;
            bool thisIsNull = (object) version == null;
            if (otherIsNull && thisIsNull)
            {
                return 0;
            }
            if (otherIsNull)
            {
                return 1;
            }
            if (thisIsNull)
            {
                return -1;
            }
            return version.CompareTo(other.version);
        }

        public bool Equals(string versionString)
        {
            return string.Equals(ToString(), versionString);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return -1;
            if (!(obj is AbstractVersion)) return 1;
            return CompareTo((AbstractVersion)obj);
        }

        public override string ToString()
        {
            return version?.ToString() ?? "";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((AbstractVersion)obj);
        }

        public override int GetHashCode()
        {
            return version?.GetHashCode() ?? 0;
        }

        // Why don't I get operator overloads for free?
        // Is there a class I can delcare allegiance to that gives me this?
        // Where's my ComparableOperators role?

        public static bool operator <(AbstractVersion v1, AbstractVersion v2)
        {
            return v1.CompareTo(v2) < 0;
        }

        public static bool operator <=(AbstractVersion v1, AbstractVersion v2)
        {
            return v1.CompareTo(v2) <= 0;
        }

        public static bool operator >(AbstractVersion v1, AbstractVersion v2)
        {
            return v1.CompareTo(v2) > 0;
        }

        public static bool operator >=(AbstractVersion v1, AbstractVersion v2)
        {
            return v1.CompareTo(v2) >= 0;
        }

        public static bool operator ==(AbstractVersion v1, AbstractVersion v2)
        {
            if (object.ReferenceEquals(null, v1))
            {
                return object.ReferenceEquals(null, v2);
            }
            return v1.Equals(v2);
        }

        public static bool operator !=(AbstractVersion v1, AbstractVersion v2)
        {
            if (object.ReferenceEquals(null, v1))
            {
                return !object.ReferenceEquals(null, v2);
            }
            return !v1.Equals(v2);
        }
    }
}
