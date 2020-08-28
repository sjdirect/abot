using System;

namespace Abot2.Poco
{
    public class HyperLink : IEquatable<HyperLink>
    {
        public string RawHrefValue { get; set; }

        public string RawHrefText { get; set; }

        public Uri HrefValue { get; set; }

        public override int GetHashCode()
        {
            return HrefValue != null ? HrefValue.AbsoluteUri.GetHashCode() : base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as HyperLink);
        }

        public bool Equals(HyperLink other)
        {
            return 
                this.HrefValue != null && other.HrefValue != null ? 
                    this.HrefValue.AbsoluteUri == other.HrefValue.AbsoluteUri : 
                    this.RawHrefValue == other.RawHrefValue;
        }
    }
}
