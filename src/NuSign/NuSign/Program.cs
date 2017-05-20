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

        static int Main(string[] args)
        {
            bool argHelp = false;
            string argSign = null;
            string argVerify = null;
            string argCert = null;
            bool argSkipCertValidation = false;
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
                $"  {AssemblyName} -verify Example.nupkg -skipCertValidation",
                "",
                "Available options:",
                "",
                { "h|help", "Show help", h => argHelp = (h != null) },
                { "s|sign=", "Sign specified package", s => argSign = s },
                { "v|verify=", "Verify signature of specified package", v => argVerify = v },
                { "c|cert=", "Thumbprint of the signing certificate located in CurrentUser\\My certificate store", c => argCert = c },
                { "k|skipCertValidation", "Skip validation of signing certificate", k => argSkipCertValidation = (k != null) },
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
                return 1;
            }

            if (args.Length == 0 || argHelp || (argExtras != null && argExtras.Count > 0))
            {
                options.WriteOptionDescriptions(Console.Out);
                return 1;
            }

            if (!string.IsNullOrEmpty(argSign))
            {
                string fileName = Path.GetFileName(argSign);

                Console.WriteLine($"Signing package \"{fileName}\"...");

                X509Certificate2 signingCert = null;

                try
                {
                    using (NuGetPackage package = new NuGetPackage(argSign))
                        signingCert = package.Sign(argCert);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to sign package: {ex.Message}");
                    return 1;
                }

                Console.WriteLine($"Package \"{fileName}\" successfully signed.");
                PrintCertInfo(signingCert);
            }

            if (!string.IsNullOrEmpty(argVerify))
            {
                string fileName = Path.GetFileName(argVerify);

                if (argSkipCertValidation)
                    Console.WriteLine($"Verifying the signature of package \"{fileName}\" without the validation of signing certificate...");
                else
                    Console.WriteLine($"Verifying the signature of package \"{fileName}\"...");

                X509Certificate2 signingCert = null;

                try
                {
                    using (NuGetPackage package = new NuGetPackage(argVerify))
                        signingCert = package.Verify(argSkipCertValidation);
                }
                catch (InvalidSignatureException ex)
                {
                    Console.WriteLine(ex.InnerException == null ? $"{ex.Message}" : $"{ex.Message}: {ex.InnerException.Message}");
                    return 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to verify package: {ex.Message}");
                    return 1;
                }

                Console.WriteLine($"Signature of \"{fileName}\" package is VALID.");
                PrintCertInfo(signingCert);
            }

            return 0;
        }

        static void PrintCertInfo(X509Certificate2 cert)
        {
            Console.WriteLine();
            Console.WriteLine("Package was signed with the following certificate:");
            Console.WriteLine($"  Issuer:         {cert.Issuer}");
            Console.WriteLine($"  Subject:        {cert.Subject}");
            Console.WriteLine($"  Serial number:  {cert.SerialNumber}");
            Console.WriteLine($"  Invalid before: {cert.NotBefore.ToString("R")}");
            Console.WriteLine($"  Invalid after:  {cert.NotAfter.ToString("R")}");
            Console.WriteLine();
        }
    }
}
