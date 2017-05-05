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
                "",
                "Available options:",
                "",
                { "h|help", "Show help", h => argHelp = (h != null) },
                { "s|sign=", "Sign specified package", s => argSign = s },
                { "v|verify=", "Verify signature of specified package", v => argVerify = v },
                { "c|cert=", "Thumbprint of the signing certificate located in CurrentUser\\My certificate store", c => argCert = c },
                ""
            };
            
            try
            {
                argExtras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
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
                using (NuGetPackage package = new NuGetPackage(argSign))
                    package.Sign(argCert);
            }

            if (!string.IsNullOrEmpty(argVerify))
            {
                X509Certificate2 signerCert = null;
                using (NuGetPackage package = new NuGetPackage(argVerify))
                    signerCert = package.Verify();

                Console.WriteLine(signerCert.Subject); // TODO - show in more friendly way
            }
        }
    }
}
