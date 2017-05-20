using Ionic.Zip;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace NuSign
{
    public class NuGetPackage : IDisposable
    {
        const string _integrityListPath = "package/signatures/IntegrityList.xml";

        const string _integrityListSignaturePath = "package/signatures/IntegrityList.p7s";

        private bool _disposed = false;

        private ZipFile _zipFile = null;

        public bool IsSigned
        {
            get
            {
                return (_zipFile.ContainsEntry(_integrityListPath) && _zipFile.ContainsEntry(_integrityListSignaturePath));
            }
        }

        #region Constructors

        public NuGetPackage(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException($"File {path} does not exist");

            _zipFile = new ZipFile(path);
        }

        #endregion

        #region Public methods

        public X509Certificate2 Sign(string certThumbPrint)
        {
            if (this.IsSigned)
                throw new Exception("Package is already signed");

            IntegrityList integrityList = this.ComputeIntegrityList(out _, out _);

            byte[] integrityListContent = integrityList.ToByteArray();
            X509Certificate2 signingCert = CryptoUtils.CreateDetachedCmsSignature(integrityListContent, certThumbPrint, out byte[] integrityListSignature);

            _zipFile.AddEntry(_integrityListPath, integrityListContent);
            _zipFile.AddEntry(_integrityListSignaturePath, integrityListSignature);
            _zipFile.Save();

            return signingCert;
        }

        public X509Certificate2 Verify(bool skipCertValidation)
        {
            if (!this.IsSigned)
                throw new Exception("Package is not signed");

            IntegrityList computedIntegrityList = this.ComputeIntegrityList(out byte[] integrityListContent, out byte[] integrityListSignatureContent);
            IntegrityList embeddedIntegrityList = IntegrityList.FromByteArray(integrityListContent);

            SignerInfo signerInfo = CryptoUtils.VerifyDetachedCmsSignature(integrityListContent, integrityListSignatureContent, skipCertValidation);

            if (!computedIntegrityList.SequenceEqual(embeddedIntegrityList))
                throw new InvalidSignatureException("Package content has been altered");
            
            return signerInfo.Certificate;
        }

        #endregion

        #region Private methods

        private IntegrityList ComputeIntegrityList(out byte[] integrityListContent, out byte[] integrityListSignatureContent)
        {
            byte[] integrityListContentLocal = null;
            byte[] integrityListSignatureContentLocal = null;

            IntegrityList integrityList = new IntegrityList();

            foreach (ZipEntry zipEntry in _zipFile.EntriesSorted)
            {
                if (zipEntry.IsDirectory)
                {
                    continue;
                }

                if (IsIntegrityList(zipEntry))
                {
                    integrityListContentLocal = ReadZipEntryContent(zipEntry);
                    continue;
                }

                if (IsIntegrityListSignature(zipEntry))
                {
                    integrityListSignatureContentLocal = ReadZipEntryContent(zipEntry);
                    continue;
                }

                IntegrityEntry integrityEntry = new IntegrityEntry()
                {
                    FilePath = zipEntry.FileName,
                    HashAlgorithm = @"http://www.w3.org/2000/09/xmldsig#sha256",
                    HashValue = ComputeHash(zipEntry)
                };

                integrityList.Add(integrityEntry);
            }

            integrityListContent = integrityListContentLocal;
            integrityListSignatureContent = integrityListSignatureContentLocal;
            return integrityList;
        }

        #endregion

        #region Private static methods

        private static byte[] ComputeHash(ZipEntry zipEntry)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                zipEntry.Extract(ms);
                ms.Position = 0;
                return CryptoUtils.ComputeHash(ms);
            }
        }

        private static bool IsIntegrityList(ZipEntry zipEntry)
        {
            return (0 == string.Compare(zipEntry.FileName, _integrityListPath, StringComparison.Ordinal));
        }

        private static bool IsIntegrityListSignature(ZipEntry zipEntry)
        {
            return (0 == string.Compare(zipEntry.FileName, _integrityListSignaturePath, StringComparison.Ordinal));
        }

        private static byte[] ReadZipEntryContent(ZipEntry zipEntry)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                zipEntry.Extract(ms);
                return ms.ToArray();
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    if (_zipFile != null)
                    {
                        _zipFile.Dispose();
                        _zipFile = null;
                    }
                }

                _disposed = true;
            }
        }

        ~NuGetPackage()
        {
            Dispose(false);
        }

        #endregion
    }
}
