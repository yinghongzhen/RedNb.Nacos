using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace RedNb.Nacos.Http.Tests.Regression;

internal static class TestCertificates
{
    public static X509Certificate2 Import(X509Certificate2 certificate)
    {
        var bytes = certificate.Export(X509ContentType.Pfx);
        try
        {
#if NET8_0
            return new X509Certificate2(bytes, (string?)null, X509KeyStorageFlags.Exportable);
#else
            return X509CertificateLoader.LoadPkcs12(bytes, null, X509KeyStorageFlags.Exportable);
#endif
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
}
