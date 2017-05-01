namespace NuSign
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = "Pkcs11Interop.3.2.0.nupkg";

            using (NuGetPackage pkg = new NuGetPackage(path))
                pkg.Sign();

            using (NuGetPackage pkg = new NuGetPackage(path))
                pkg.Verify();
        }
    }
}
