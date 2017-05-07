using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace NuSign
{
    class Program
    {
        static string AssemblyName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);

        static void Main(string[] args)
        {
            bool argHelp = false;
            string argSign = null;
            string argVerify = null;
            string argCert = null;
            bool argCertValidation = false;
            List<string> argExtras = null;

            var options = new OptionSet {
                $"{AssemblyName} - NuGet package signing prototype",
                "",
                "Example usage:",
                "",
                $"  {AssemblyName} -help",
                $"  {AssemblyName} -sign Example.nupkg",
                $"  {AssemblyName} -sign Example.nupkg -cert d5de31ea974f5ea8581d633eeffa8f3ea0d479bb",
                $"  {AssemblyName} -verify Example.nupkg",
                $"  {AssemblyName} -verify Example.nupkg -performCertValidation",
                "",
                "Available options:",
                "",
                { "h|help", "Show help", h => argHelp = (h != null) },
                { "s|sign=", "Sign specified package", s => argSign = s },
                { "v|verify=", "Verify signature of specified package", v => argVerify = v },
                { "c|cert=", "Thumbprint of the signing certificate located in CurrentUser\\My certificate store", c => argCert = c },
                { "p|performCertValidation", "Perform also validation of signer's certificate", p => argCertValidation = (p != null) },
                ""
            };
            
            try
            {
                argExtras = options.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine($"Try '{AssemblyName} -help' for more information.");
                return;
            }

            if (argHelp || (argExtras != null && argExtras.Count > 0))
            {
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (!string.IsNullOrEmpty(argSign))
            {
                string fileName = Path.GetFileName(argSign);

                Console.WriteLine($"Signing package \"{fileName}\"...");

                try
                {
                    using (NuGetPackage package = new NuGetPackage(argSign))
                        package.Sign(argCert);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to sign package: {ex.Message}");
                    return;
                }

                Console.WriteLine($"Package \"{fileName}\" successfully signed.");
            }

            Console.WriteLine();

            if (!string.IsNullOrEmpty(argVerify))
            {
                string fileName = Path.GetFileName(argVerify);

                if (argCertValidation)
                    Console.WriteLine($"Verifying the signature of package \"{fileName}\"...");
                else
                    Console.WriteLine($"Verifying the signature of package \"{fileName}\" without the validation of signer's certificate...");

                X509Certificate2 signerCert = null;

                try
                {
                    using (NuGetPackage package = new NuGetPackage(argVerify))
                        signerCert = package.Verify(argCertValidation);
                }
                catch (InvalidSignatureException ex)
                {
                    Console.WriteLine(ex.InnerException == null ? $"{ex.Message}" : $"{ex.Message}: {ex.InnerException.Message}");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to verify package: {ex.Message}");
                    return;
                }

                Console.WriteLine($"Signature of \"{fileName}\" package is VALID.");
                Console.WriteLine();
                Console.WriteLine("Package was signed with the following certificate:");
                Console.WriteLine($"  Issuer:         {signerCert.Issuer}");
                Console.WriteLine($"  Subject:        {signerCert.Subject}");
                Console.WriteLine($"  Serial number:  {signerCert.SerialNumber}");
                Console.WriteLine($"  Invalid before: {signerCert.NotBefore.ToString("R")}");
                Console.WriteLine($"  Invalid after:  {signerCert.NotAfter.ToString("R")}");
            }

            Console.WriteLine();
        }
    }
}
