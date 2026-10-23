using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.AspNetCore.HealthChecks;
using RedNb.Nacos.Http.Transport;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Regression;

public class TlsTransportTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrivateCaAndMutualTlsApplyToLoginRequestsConsoleAndHealth(bool mutual)
    {
        await using var server = await TlsServer.StartAsync(mutual);
        var options = server.Options(mutual);
        options.Username = "local-test"; options.Password = "local-test";
        using var client = new NacosHttpClient(options);
        Assert.Equal("ok", await client.GetAsync("probe", cancellationToken: TestContext.Current.CancellationToken));
        using var console = new NacosConsoleHttpClient(options);
        Assert.Equal("ok", await console.GetAsync("v3/console/health/readiness", cancellationToken: TestContext.Current.CancellationToken));
        using var check = new NacosHealthCheck(options, new NacosHealthCheckOptions { Mode = NacosHealthCheckMode.ServerAndConsole, Strategy = NacosHealthCheckStrategy.All });
        Assert.Equal(HealthStatus.Healthy, (await check.CheckHealthAsync(new(), TestContext.Current.CancellationToken)).Status);
        var transport = typeof(NacosClientOptions).Assembly.GetType("RedNb.Nacos.Utils.NacosTransport")!;
        using var handler = (HttpMessageHandler)transport.GetMethod("CreateHandler")!.Invoke(null, [options, true])!;
        using var http2 = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, options.GetBaseUrl(options.ServerAddresses) + "/probe")
        { Version = HttpVersion.Version20, VersionPolicy = HttpVersionPolicy.RequestVersionExact };
        using var response = await http2.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpVersion.Version20, response.Version);
        Assert.True(server.Logins >= 2);
        if (mutual) Assert.True(server.ClientCertificates > 0);
    }

    [Theory]
    [InlineData("untrusted")]
    [InlineData("wrong-ca")]
    [InlineData("hostname")]
    [InlineData("expired")]
    [InlineData("missing-client")]
    [InlineData("wrong-client")]
    [InlineData("mismatched-key")]
    [InlineData("missing-key")]
    public async Task InvalidTrustIdentityOrClientCredentialsAreRejected(string problem)
    {
        await using var server = await TlsServer.StartAsync(problem is "missing-client" or "wrong-client", problem);
        var options = server.Options(problem is "mismatched-key" or "missing-key" or "wrong-client");
        if (problem == "untrusted") options.TlsCaPath = null;
        if (problem == "wrong-ca") options.TlsCaPath = server.OtherCaPath;
        if (problem == "mismatched-key") options.TlsKeyPath = server.WrongKeyPath;
        if (problem == "missing-key") options.TlsKeyPath = null;
        var error = await Record.ExceptionAsync(async () =>
        {
            using var client = new NacosHttpClient(options);
            await client.GetAsync("probe", cancellationToken: TestContext.Current.CancellationToken);
        });
        Assert.NotNull(error);
    }

    private sealed class TlsServer : IAsyncDisposable
    {
        private readonly List<IDisposable> _certificates = new();
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "rednb-tls-" + Guid.NewGuid().ToString("N"));
        private WebApplication? _app;
        private string _address = "";
        public int Logins, ClientCertificates;
        public string OtherCaPath => Path.Combine(_directory, "other-ca.pem");
        public string WrongKeyPath => Path.Combine(_directory, "wrong-key.pem");

        public NacosClientOptions Options(bool client) => new()
        {
            ServerAddresses = new Uri(_address).Authority, ConsoleAddresses = _address, EnableTls = true,
            TlsCaPath = Path.Combine(_directory, "ca.pem"), DefaultTimeout = 2000,
            TlsCertPath = client ? Path.Combine(_directory, "client.pem") : null,
            TlsKeyPath = client ? Path.Combine(_directory, "client-key.pem") : null
        };

        public static async Task<TlsServer> StartAsync(bool mutual, string problem = "")
        {
            var server = new TlsServer();
            try { await server.StartCoreAsync(mutual, problem); return server; }
            catch { await server.DisposeAsync(); throw; }
        }

        private async Task StartCoreAsync(bool mutual, string problem)
        {
            Directory.CreateDirectory(_directory);
            X509Certificate2 MakeCa(string name)
            {
                var key = RSA.Create(2048); _certificates.Add(key);
                var request = new CertificateRequest("CN=" + name, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
                var ca = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(2));
                _certificates.Add(ca); return ca;
            }
            var ca = MakeCa("Local test CA"); var other = MakeCa("Unrelated CA");
            await File.WriteAllTextAsync(Path.Combine(_directory, "ca.pem"), ca.ExportCertificatePem() + Environment.NewLine + other.ExportCertificatePem());
            await File.WriteAllTextAsync(OtherCaPath, other.ExportCertificatePem());
            X509Certificate2 Issue(bool client)
            {
                using var key = RSA.Create(2048);
                var request = new CertificateRequest(client ? "CN=local client" : "CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
                request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new(client ? "1.3.6.1.5.5.7.3.2" : "1.3.6.1.5.5.7.3.1") }, true));
                if (!client)
                {
                    var san = new SubjectAlternativeNameBuilder();
                    if (problem == "hostname") san.AddDnsName("wrong.invalid");
                    else { san.AddDnsName("localhost"); san.AddIpAddress(IPAddress.Loopback); }
                    request.CertificateExtensions.Add(san.Build());
                }
                using var issued = request.Create(client && problem == "wrong-client" ? other : ca, DateTimeOffset.UtcNow.AddDays(-3),
                    DateTimeOffset.UtcNow.AddDays(problem == "expired" && !client ? -1 : 1), RandomNumberGenerator.GetBytes(16));
                using var keyed = issued.CopyWithPrivateKey(key);
                if (client)
                {
                    File.WriteAllText(Path.Combine(_directory, "client.pem"), keyed.ExportCertificatePem());
                    File.WriteAllText(Path.Combine(_directory, "client-key.pem"), key.ExportPkcs8PrivateKeyPem());
                }
                var certificate = TestCertificates.Import(keyed); _certificates.Add(certificate); return certificate;
            }
            var certificate = Issue(false); Issue(true);
            using (var wrong = RSA.Create(2048)) File.WriteAllText(WrongKeyPath, wrong.ExportPkcs8PrivateKeyPem());
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, 0, endpoint =>
            {
                endpoint.Protocols = HttpProtocols.Http1AndHttp2;
                endpoint.UseHttps(certificate, https =>
                {
                    if (mutual)
                    {
                        https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                        https.ClientCertificateValidation = (client, _, _) =>
                        {
                            using var chain = new X509Chain();
                            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                            chain.ChainPolicy.CustomTrustStore.Add(ca);
                            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                            chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.2"));
                            var valid = chain.Build(client);
                            if (valid) Interlocked.Increment(ref ClientCertificates);
                            return valid;
                        };
                    }
                });
            }));
            _app = builder.Build();
            _app.MapPost("/nacos/v3/auth/user/login", () => { Interlocked.Increment(ref Logins); return new { accessToken = "local-token", tokenTtl = 18000 }; });
            _app.MapGet("/nacos/probe", () => "ok");
            _app.MapGet("/v3/console/health/readiness", () => "ok");
            _app.MapGet("/nacos/v3/admin/core/state/readiness", () => "ok");
            await _app.StartAsync(TestContext.Current.CancellationToken);
            _address = _app.Urls.Single();
        }

        public async ValueTask DisposeAsync()
        {
            if (_app != null) await _app.DisposeAsync();
            foreach (var certificate in _certificates) certificate.Dispose();
            IssueTestFiles.RemoveDirectory(_directory);
        }
    }
}
