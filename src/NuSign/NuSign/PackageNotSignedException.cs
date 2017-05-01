using System;

namespace NuSign
{
    public class PackageNotSignedException : Exception
    {
        public PackageNotSignedException()
            : this("Package is not signed")
        {

        }

        public PackageNotSignedException(string message)
            : base(message)
        {

        }
    }
}
