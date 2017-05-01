using System;

namespace NuSign
{
    public class PackageAlreadySignedException : Exception
    {
        public PackageAlreadySignedException()
            : this("Package is already signed")
        {

        }

        public PackageAlreadySignedException(string message)
            : base(message)
        {

        }
    }
}
