using System;
using System.Security.Cryptography.X509Certificates;

namespace NuSign
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = "Pkcs11Interop.3.2.0.nupkg";

            // nusign.exe -sign [-cert thumbprint] Pkcs11Interop.3.2.0.nupkg

            // nusign.exe -verify Pkcs11Interop.3.2.0.nupkg

            using (NuGetPackage pkg = new NuGetPackage(path))
                pkg.Sign();

            X509Certificate2 signerCert = null;
            using (NuGetPackage pkg = new NuGetPackage(path))
                signerCert = pkg.Verify();

            Console.WriteLine(signerCert.Subject);
        }
    }
}
