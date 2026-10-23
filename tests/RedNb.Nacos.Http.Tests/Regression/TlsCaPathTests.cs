using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using RedNb.Nacos;
using RedNb.Nacos.Http.Transport;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Regression;

public sealed class TlsCaPathTests
{
    [Fact]
    public async Task HttpTransport_ShouldTrustTheCertificateAuthorityConfiguredByTlsCaPath()
    {
        using var caKey = RSA.Create(2048);
        var caRequest = new CertificateRequest("CN=RedNb local test CA", caKey,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        caRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        using var ca = caRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
        using var serverKey = RSA.Create(2048);
        var serverRequest = new CertificateRequest("CN=localhost", serverKey,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        serverRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        serverRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        serverRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, true));
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("localhost");
        names.AddIpAddress(IPAddress.Loopback);
        serverRequest.CertificateExtensions.Add(names.Build());
        using var issued = serverRequest.Create(ca, DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow.AddDays(1), RandomNumberGenerator.GetBytes(16));
        using var certificateWithKey = issued.CopyWithPrivateKey(serverKey);
        using var serverCertificate = TestCertificates.Import(certificateWithKey);

        var certificateDirectory = Path.Combine(Path.GetTempPath(), $"rednb-tls-repro-{Guid.NewGuid():N}");
        Directory.CreateDirectory(certificateDirectory);
        var caPath = Path.Combine(certificateDirectory, "ca.pem");
        try
        {
            await File.WriteAllTextAsync(caPath, ca.ExportCertificatePem());
            await using var server = new LoopbackTlsServer(serverCertificate);
            var endpoint = $"https://127.0.0.1:{server.Port}/nacos/probe";

            // Establish that the server certificate, CA and handshake are valid before testing the SDK.
            using (var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, certificate, _, errors) =>
                {
                    if (certificate is null || errors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
                        return false;
                    using var chain = new X509Chain();
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(ca);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    return chain.Build(certificate);
                }
            })
            using (var control = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) })
            {
                Assert.Equal("ok", await control.GetStringAsync(endpoint));
            }

            using var client = new NacosHttpClient(new NacosClientOptions
            {
                ServerAddresses = $"127.0.0.1:{server.Port}",
                EnableTls = true,
                TlsCaPath = caPath,
                DefaultTimeout = 10_000
            });
            string? response = null;
            var error = await Record.ExceptionAsync(async () => response = await client.GetAsync("probe"));
            Assert.Null(error);
            Assert.Equal("ok", response);
        }
        finally
        {
            IssueTestFiles.RemoveDirectory(certificateDirectory);
        }
    }

    private sealed class LoopbackTlsServer : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _stop = new();
        private readonly ConcurrentBag<Task> _requests = new();
        private readonly Task _acceptLoop;
        public int Port { get; }

        public LoopbackTlsServer(X509Certificate2 certificate)
        {
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _acceptLoop = Task.Run(async () =>
            {
                while (!_stop.IsCancellationRequested)
                {
                    TcpClient socket;
                    try { socket = await _listener.AcceptTcpClientAsync(_stop.Token); }
                    catch (OperationCanceledException) { break; }
                    catch (SocketException) when (_stop.IsCancellationRequested) { break; }
                    _requests.Add(HandleAsync(socket, certificate));
                }
            });
        }

        private async Task HandleAsync(TcpClient socket, X509Certificate2 certificate)
        {
            using (socket)
            using (var tls = new SslStream(socket.GetStream()))
            {
                try
                {
                    await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                    {
                        ServerCertificate = certificate,
                        ClientCertificateRequired = false,
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                    }, _stop.Token);
                    using var reader = new StreamReader(tls, Encoding.ASCII, false, 1024, leaveOpen: true);
                    while (await reader.ReadLineAsync(_stop.Token) is { Length: > 0 }) { }
                    await tls.WriteAsync(Encoding.ASCII.GetBytes(
                        "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok"), _stop.Token);
                }
                catch (AuthenticationException) { }
                catch (IOException) { }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
            }
        }

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            _listener.Stop();
            await _acceptLoop;
            await Task.WhenAll(_requests.ToArray());
            _stop.Dispose();
        }
    }
}
