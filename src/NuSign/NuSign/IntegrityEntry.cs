using System;
using System.Linq;

namespace NuSign
{
    public class IntegrityEntry : IEquatable<IntegrityEntry>
    {
        public string FilePath
        {
            get;
            set;
        }

        public string HashAlgorithm
        {
            get;
            set;
        }

        public byte[] HashValue
        {
            get;
            set;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IntegrityEntry);
        }

        public bool Equals(IntegrityEntry other)
        {
            // For the sake of simplicity let's pretend this object is immutable

            if (other == null)
                return false;

            return this.FilePath.Equals(other.FilePath) && this.HashAlgorithm.Equals(other.HashAlgorithm) && this.HashValue.SequenceEqual(other.HashValue);
        }

        public override int GetHashCode()
        {
            // For the sake of simplicity let's pretend this object is immutable

            int hash = 13;

            hash = (hash * 7) + this.FilePath.GetHashCode();
            hash = (hash * 7) + this.HashAlgorithm.GetHashCode();
            hash = (hash * 7) + this.HashValue.GetHashCode();

            return hash;
        }
    }
}
