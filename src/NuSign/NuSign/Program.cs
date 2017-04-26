using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace NuSign
{
    class Program
    {
        static string _nupkgFileName = "Pkcs11Interop.3.2.0.nupkg";

        static string _fileListPath = "package/signatures/sha256sums.txt";

        static string _fileListSignaturePath = "package/signatures/sha256sums.p7s";

        /*
$ find . -type f | grep -v sha256sums.txt | xargs sha256sum > sha256sums.txt

c52795c4dc62aed3863c25cbc93df6e18f74a6173e831fbf8b669bd92c6cb572  ./lib/net45/Pkcs11Interop.dll
eafcd27e71798962657778dff1fd7cec4b15bb9b6e9c6ec2397841eeff0dc5bc  ./lib/net45/Pkcs11Interop.xml
4964700c893cdb4f76d2fdd63a7e1d7139bbebceb86af0238437f02830be7f30  ./lib/monoandroid2.3/Pkcs11Interop.Android.dll
270234e614b63ddff18e7ac464c78eff7ff0650343a4c873ea6314e8dfc05a3d  ./lib/monoandroid2.3/Pkcs11Interop.Android.xml
5727f3416c8ad9f8dd9027d9870fda5cc2a7dd25a28ddab12e8fe77d09bcd0a2  ./lib/net40/Pkcs11Interop.dll
eafcd27e71798962657778dff1fd7cec4b15bb9b6e9c6ec2397841eeff0dc5bc  ./lib/net40/Pkcs11Interop.xml
3df531c5814a640ee93be7276b3d0fca364830ee75038e753e99bde069e292f8  ./lib/xamarinios1.0/Pkcs11Interop.iOS.xml
cd27bd96e2b6a6bf9c396fc7265cab312689edd3f9adf56a684f96ab90e2a0c4  ./lib/xamarinios1.0/Pkcs11Interop.iOS.dll
854595a395e67c7d884cdc58bc3e0c18ab1b8ea4dd1f9ac864f31c4fe14c5d14  ./lib/net20/Pkcs11Interop.dll
eafcd27e71798962657778dff1fd7cec4b15bb9b6e9c6ec2397841eeff0dc5bc  ./lib/net20/Pkcs11Interop.xml
60595e91b0c5b62a6739cac8bda9c547e0c992f129a02b787e50d15b81346abb  ./lib/netstandard1.3/Pkcs11Interop.DotNetCore.xml
e89a6fc5496c18cae23bfa17b446767fe8e186dfefd431fa7739809ba06f0ebb  ./lib/netstandard1.3/Pkcs11Interop.DotNetCore.dll
efb5eddabe32a80d59cd268e3fc701919dda6a84d3263fad90816c87826d4a71  ./lib/sl5/Pkcs11Interop.Silverlight.dll
e3697bed3d9c1bd092fd3e1968a17631560921eb5d4a5fa26ddd6785085cd69a  ./lib/sl5/Pkcs11Interop.Silverlight.xml
3ddf9be5c28fe27dad143a5dc76eea25222ad1dd68934a047064e56ed2fa40c5  ./LICENSE.txt
2decf0d270ad07d65d71f175e04aa8ac821b7094407a7adf11d8d77a11c7ec5a  ./NOTICE.txt
64e1d70d9d2ae49a6b2d80d305eb66e576cf25d5c99ff94bc644f51660f77190  ./Pkcs11Interop.nuspec
646776abfd4dd46a92e190188ccf772d4cfabf72561aa02766acdd868cef3a25  ./[Content_Types].xml
217e4d08724279b86e1d477ba21d730aaf2beb229f3517630d902ac6a477dcab  ./_rels/.rels
1fc36b4ed45eaa1cc8b3b6fa04f4e5d0f87b5d6411ba63cd2e85a4efb55ccf43  ./package/services/metadata/core-properties/4f06113a123e4c98907eae31bec8d923.psmdcp
         */

        static void Main(string[] args)
        {
            if (!System.IO.File.Exists(_nupkgFileName))
                throw new Exception();

            SignPackage(_nupkgFileName);

            VerifyPackage(_nupkgFileName);
        }

        static void VerifyPackage(string path)
        {
            using (Ionic.Zip.ZipFile zipFile = new Ionic.Zip.ZipFile(path))
            {
                StringBuilder stringBuilder = new StringBuilder();

                if (!zipFile.ContainsEntry(_fileListPath) || !zipFile.ContainsEntry(_fileListSignaturePath))
                    throw new Exception("Package is not signed");

                byte[] fileList = null;
                byte[] fileListSignature = null;

                foreach (Ionic.Zip.ZipEntry zipEntry in zipFile.EntriesSorted)
                {
                    if (zipEntry.IsDirectory)
                        continue;

                    if (zipEntry.FileName == _fileListPath)
                    {
                        fileList = ReadZipEntryContent(zipEntry);
                        continue;
                    }
                        
                    if (zipEntry.FileName == _fileListSignaturePath)
                    {
                        fileListSignature = ReadZipEntryContent(zipEntry);
                        continue;
                    }

                    string fileName = zipEntry.FileName;
                    string fileHash = ComputeHash(zipEntry);

                    stringBuilder.AppendLine(fileHash + " " + fileName);
                }

                byte[] currentFileList = Encoding.UTF8.GetBytes(stringBuilder.ToString());

                if (!currentFileList.SequenceEqual(fileList))
                    throw new Exception("Package content has been altered");

                VerifyDetachedCmsSignature(fileList, fileListSignature);
            }
        }

        static void SignPackage(string path)
        {
            using (Ionic.Zip.ZipFile zipFile = new Ionic.Zip.ZipFile(path))
            {
                StringBuilder stringBuilder = new StringBuilder();

                if (zipFile.ContainsEntry(_fileListPath) || zipFile.ContainsEntry(_fileListSignaturePath))
                    throw new Exception("Package is already signed");

                foreach (Ionic.Zip.ZipEntry zipEntry in zipFile.EntriesSorted)
                {
                    if (zipEntry.IsDirectory)
                        continue;

                    string fileName = zipEntry.FileName;
                    string fileHash = ComputeHash(zipEntry);

                    stringBuilder.AppendLine(fileHash + " " + fileName);
                }

                byte[] fileList = Encoding.UTF8.GetBytes(stringBuilder.ToString());
                byte[] fileListSignature = SignData(fileList);

                zipFile.AddEntry(_fileListPath, fileList);
                zipFile.AddEntry(_fileListSignaturePath, fileListSignature);

                zipFile.Save();
            }
        }

        static string BytesToHexString(byte[] value)
        {
            if (value == null)
                return null;

            return BitConverter.ToString(value).Replace("-", "").ToLower();
        }

        static byte[] ComputeHash(Stream data)
        {
            System.Security.Cryptography.SHA256Managed sha256 = new System.Security.Cryptography.SHA256Managed();
            sha256.ComputeHash(data);
            return sha256.Hash;
        }

        static string ComputeHash(Ionic.Zip.ZipEntry entry)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                entry.Extract(ms);
                ms.Position = 0;
                return BytesToHexString(ComputeHash(ms));
            }
        }

        static byte[] ReadZipEntryContent(Ionic.Zip.ZipEntry entry)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                entry.Extract(ms);
                ms.Position = 0;
                return ms.ToArray();
            }
        }

        static byte[] SignData(byte[] data)
        {
            X509Certificate2 signingCertificate = GetSigningCertificate();
            return CreateDetachedCmsSignature(signingCertificate, data);
        }

        static X509Certificate2 GetSigningCertificate()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            try
            {
                X509Certificate2Collection certs = X509Certificate2UI.SelectFromCollection(store.Certificates, null, null, X509SelectionFlag.SingleSelection);
                if (certs != null && certs.Count > 0)
                    return certs[0];
            }
            finally
            {
                store.Close();
            }

            return null;
        }

        static X509Certificate2 GetSigningCertificate(string thumbprint)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            try
            {
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, true);
                if (certs != null && certs.Count > 0)
                    return certs[0];
            }
            finally
            {
                store.Close();
            }

            return null;
        }

        static byte[] CreateDetachedCmsSignature(X509Certificate2 signerCert, byte[] data)
        {
            CmsSigner cmsSigner = new CmsSigner(signerCert);
            ContentInfo contentInfo = new ContentInfo(data);
            SignedCms cms = new SignedCms(contentInfo, true);
            cms.ComputeSignature(cmsSigner, false);
            
            return cms.Encode();
        }

        static void VerifyDetachedCmsSignature(byte[] data, byte[] signature)
        {
            ContentInfo contentInfo = new ContentInfo(data);
            SignedCms cms = new SignedCms(contentInfo, true);
            cms.Decode(signature);
            cms.CheckSignature(true);
        }
    }
}
