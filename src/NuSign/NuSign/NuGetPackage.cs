using Ionic.Zip;
using System;
using System.IO;
using System.Linq;

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

        public void Sign()
        {
            if (this.IsSigned)
                throw new PackageAlreadySignedException();

            IntegrityList integrityList = this.ComputeIntegrityList();

            byte[] integrityListContent = integrityList.ToByteArray();
            byte[] integrityListSignature = CryptoUtils.CreateDetachedCmsSignature(integrityListContent);

            _zipFile.AddEntry(_integrityListPath, integrityListContent);
            _zipFile.AddEntry(_integrityListSignaturePath, integrityListSignature);

            _zipFile.Save();
        }

        public void Verify()
        {
            if (!this.IsSigned)
                throw new PackageNotSignedException();

            IntegrityList computedIntegrityList = this.ComputeIntegrityList();
            IntegrityList embeddedIntegrityList = this.ReadIntegrityList();

            if (!computedIntegrityList.SequenceEqual(embeddedIntegrityList))
                throw new Exception("Package content has been altered");

            byte[] integrityListContent = GetIntegrityListContent();
            byte[] integrityListSignatureContent = GetIntegrityListSignatureContent();

            CryptoUtils.VerifyDetachedCmsSignature(integrityListContent, integrityListSignatureContent);
        }

        #endregion

        #region Private methods

        private IntegrityList ComputeIntegrityList()
        {
            // TODO - Optimize Verify() by outputing byte[] integrityListContent and byte[] integrityListSignatureContent here

            IntegrityList integrityList = new IntegrityList();

            foreach (ZipEntry zipEntry in _zipFile.EntriesSorted)
            {
                if (zipEntry.IsDirectory || IsIntegrityList(zipEntry) || IsIntegrityListSignature(zipEntry))
                    continue;

                IntegrityEntry integrityEntry = new IntegrityEntry()
                {
                    FilePath = zipEntry.FileName,
                    HashAlgorithm = @"http://www.w3.org/2000/09/xmldsig#sha256",
                    HashValue = ComputeHash(zipEntry)
                };

                integrityList.Add(integrityEntry);
            }

            return integrityList;
        }

        private IntegrityList ReadIntegrityList()
        {
            byte[] integrityListContent = GetIntegrityListContent();
            return IntegrityList.FromByteArray(integrityListContent);
        }

        private byte[] GetIntegrityListContent()
        {
            foreach (ZipEntry zipEntry in _zipFile.EntriesSorted)
            {
                if (zipEntry.IsDirectory)
                    continue;

                if (IsIntegrityList(zipEntry))
                    return ReadZipEntryContent(zipEntry);
            }

            return null;
        }

        private byte[] GetIntegrityListSignatureContent()
        {
            foreach (ZipEntry zipEntry in _zipFile.EntriesSorted)
            {
                if (zipEntry.IsDirectory)
                    continue;

                if (IsIntegrityListSignature(zipEntry))
                    return ReadZipEntryContent(zipEntry);
            }

            return null;
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
