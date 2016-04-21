namespace DevOps.LoadTest.AutoScaling
{
    using System;
    using System.Configuration;
    using System.Globalization;
    using System.Security.Cryptography.X509Certificates;

    public static class CertificateHelper
    {
        public static X509Certificate2 GetCertificateFromAppSettings(string appSettingsName)
        {
            var managementCertificate = ConfigurationManager.AppSettings[appSettingsName];
            var bytes = Convert.FromBase64String(managementCertificate);
            return new X509Certificate2(bytes);
        }

        public static X509Certificate2 GetCertificate(StoreName storeName, StoreLocation storeLocation, string certThumbprint)
        {
            return GetCertificate(storeName, storeLocation, X509FindType.FindByThumbprint, certThumbprint);
        }

        public static X509Certificate2 GetCertificate(StoreName storeName, StoreLocation storeLocation, X509FindType findType, string findValue)
        {
            X509Certificate2 certificate = null;
            X509Store store = null;
            X509Certificate2Collection certificates = null;
            try
            {
                store = new X509Store(storeName, storeLocation);
                store.Open(OpenFlags.ReadOnly);

                certificates = store.Certificates;
                if (findType == X509FindType.FindBySubjectName)
                {
                    certificate = FindBySubjectName(certificates, findValue);
                }
                else if (findType == X509FindType.FindByThumbprint)
                {
                    certificate = FindByThumbprint(certificates, findValue);
                }
                else if (findType == X509FindType.FindBySubjectDistinguishedName)
                {
                    certificate = FindBySubjectDistinguishedName(certificates, findValue);
                }
                else
                {
                    throw new Exception(string.Format(CultureInfo.CurrentCulture, "The value {0} of X509FindType is not supported. Please use FindBySubjectName or FindByThumbprint instead.", findType));
                }
            }
            finally
            {
                if (certificates != null)
                {
                    for (int i = 0; i < certificates.Count; ++i)
                    {
                        certificates[i].Reset();
                    }
                }
                if (store != null)
                {
                    store.Close();
                }
            }

            if (certificate != null)
            {
                return certificate;
            }

            throw new Exception(string.Format(CultureInfo.CurrentCulture, "No matching certificates were found for KeyId={0} and X509FindType={1}", findValue, findType));
        }

        static X509Certificate2 FindByThumbprint(X509Certificate2Collection certificates, string findValue)
        {
            foreach (X509Certificate2 certificate in certificates)
            {
                if (string.Compare(certificate.Thumbprint, findValue, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return new X509Certificate2(certificate);
                }
            }

            return null;
        }

        static X509Certificate2 FindBySubjectDistinguishedName(X509Certificate2Collection certificates, string findValue)
        {
            foreach (X509Certificate2 certificate in certificates)
            {
                if (string.Compare(certificate.SubjectName.Name, findValue, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return new X509Certificate2(certificate);
                }
            }

            return null;
        }

        static X509Certificate2 FindBySubjectName(X509Certificate2Collection certificates, string findValue)
        {
            foreach (X509Certificate2 certificate in certificates)
            {
                if (certificate.SubjectName.Name.ToLower().Contains(findValue.ToLower()))
                {
                    return new X509Certificate2(certificate);
                }
            }

            return null;
        }

    }
}