using System;

namespace NuSign
{
    public class InvalidSignatureException : Exception
    {
        public InvalidSignatureException()
            : base("Package signature is INVALID")
        {

        }

        public InvalidSignatureException(string message)
            : base(message)
        {

        }

        public InvalidSignatureException(Exception innerException)
            : base("Package signature is INVALID", innerException)
        {

        }

        public InvalidSignatureException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}