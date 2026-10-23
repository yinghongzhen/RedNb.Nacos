using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace RedNb.Nacos.Utils;

/// <summary>Per-transport TLS material; never changes process or machine trust settings.</summary>
internal static class NacosTransport
{
    public static HttpMessageHandler CreateHandler(NacosClientOptions options, bool grpc = false)
    {
        var owned = new X509Certificate2Collection();
        var sockets = new SocketsHttpHandler
        {
            AutomaticDecompression = grpc ? DecompressionMethods.None : DecompressionMethods.GZip | DecompressionMethods.Deflate,
            PooledConnectionLifetime = grpc ? Timeout.InfiniteTimeSpan : TimeSpan.FromMinutes(5),
            EnableMultipleHttp2Connections = false,
            ConnectTimeout = grpc ? TimeSpan.FromSeconds(3) : Timeout.InfiniteTimeSpan
        };
        if (grpc)
        {
            sockets.KeepAlivePingDelay = TimeSpan.FromSeconds(60);
            sockets.KeepAlivePingTimeout = TimeSpan.FromSeconds(30);
        }
        try
        {
            if (string.IsNullOrWhiteSpace(options.TlsCertPath) != string.IsNullOrWhiteSpace(options.TlsKeyPath))
                throw new ArgumentException("TlsCertPath and TlsKeyPath must be supplied together as PEM files.");
            if (!string.IsNullOrWhiteSpace(options.TlsCaPath))
            {
                var roots = new X509Certificate2Collection();
                owned.ImportFromPemFile(options.TlsCaPath);
                roots.AddRange(owned);
                if (roots.Count == 0 || roots.Cast<X509Certificate2>().Any(c => !c.Extensions.OfType<X509BasicConstraintsExtension>().Any(e => e.CertificateAuthority)))
                    throw new ArgumentException("TlsCaPath must contain PEM CA certificates.");
                sockets.SslOptions.RemoteCertificateValidationCallback = (_, certificate, peerChain, errors) =>
                {
                    if (certificate == null || (errors & (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable)) != 0) return false;
                    using var leaf = new X509Certificate2(certificate);
                    using var chain = new X509Chain();
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.AddRange(roots);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.DisableCertificateDownloads = true;
                    chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1"));
                    if (peerChain != null)
                        foreach (var element in peerChain.ChainElements) chain.ChainPolicy.ExtraStore.Add(element.Certificate);
                    return chain.Build(leaf);
                };
            }
            if (!string.IsNullOrWhiteSpace(options.TlsCertPath))
            {
                using var pem = X509Certificate2.CreateFromPemFile(options.TlsCertPath, options.TlsKeyPath);
                var certificate = ImportClientCertificate(pem);
                owned.Add(certificate);
                var chain = new X509Certificate2Collection();
                var before = owned.Count;
                owned.ImportFromPemFile(options.TlsCertPath);
                for (var i = before; i < owned.Count; i++) chain.Add(owned[i]);
                sockets.SslOptions.ClientCertificateContext = SslStreamCertificateContext.Create(certificate, chain, offline: true);
                sockets.SslOptions.ClientCertificates = new X509CertificateCollection { certificate };
            }
            return new OwnedHandler(sockets, owned);
        }
        catch
        {
            sockets.Dispose();
            foreach (var certificate in owned) certificate.Dispose();
            throw;
        }
    }

    private static X509Certificate2 ImportClientCertificate(X509Certificate2 pem)
    {
        // Re-import the key for Windows SChannel; do not retain the temporary PKCS#12 buffer.
        var bytes = pem.Export(X509ContentType.Pfx);
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

    private sealed class OwnedHandler(HttpMessageHandler inner, X509Certificate2Collection certificates) : DelegatingHandler(inner)
    {
        private int _disposed;
        protected override void Dispose(bool disposing)
        {
            if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                base.Dispose(true);
                foreach (var certificate in certificates) certificate.Dispose();
            }
            else base.Dispose(disposing);
        }
    }
}
