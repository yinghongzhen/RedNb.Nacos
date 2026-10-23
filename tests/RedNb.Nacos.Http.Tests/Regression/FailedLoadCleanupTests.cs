using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using RedNb.Nacos.AspNetCore.Configuration;
using RedNb.Nacos.Config;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Regression;

public sealed class FailedLoadCleanupTests(ITestOutputHelper output)
{
    [Fact]
    public void FailedConfigurationBuildMustDisposeItsAlreadySubscribedClient()
    {
        var source = new NacosConfigurationSource { ReloadOnChange = true };
        source.ConfigItems.Add(new() { DataId = "first.json", Optional = false });
        source.ConfigItems.Add(new() { DataId = "missing.json", Optional = false });
        var fake = new Mock<IConfigService>(MockBehavior.Strict);
        var subscriptions = 0;
        var removals = 0;
        var disposals = 0;
        fake.Setup(x => x.GetConfigAsync("first.json", "DEFAULT_GROUP", It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{}");
        fake.Setup(x => x.GetConfigAsync("missing.json", "DEFAULT_GROUP", It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        fake.Setup(x => x.AddListenerAsync("first.json", "DEFAULT_GROUP", It.IsAny<IConfigChangeListener>(), It.IsAny<CancellationToken>()))
            .Callback(() => subscriptions++).Returns(Task.CompletedTask);
        fake.Setup(x => x.RemoveListener(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IConfigChangeListener>()))
            .Callback(() => removals++);
        fake.Setup(x => x.DisposeAsync()).Callback(() => disposals++).Returns(ValueTask.CompletedTask);
        var provider = CreateProvider(source, fake.Object);
        try
        {
            var builder = new ConfigurationBuilder().Add(new ExistingProviderSource(provider));
            var error = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Contains("missing.json", error.Message);
            Assert.Equal(1, subscriptions);
            output.WriteLine($"After failed Build: subscriptions={subscriptions}, removals={removals}, clientDisposals={disposals}");
            Assert.Equal(1, disposals);
        }
        finally
        {
            provider.Dispose();
        }
    }

    private sealed class ExistingProviderSource(IConfigurationProvider provider) : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder) => provider;
    }

    private static NacosConfigurationProvider CreateProvider(NacosConfigurationSource source, IConfigService fake)
    {
        var testNamespace = "issue-repro-" + Guid.NewGuid().ToString("N");
        source.Options.Namespace = testNamespace;
        source.Options.ServerAddresses = "127.0.0.1:1";
        var servers = string.Join(",", source.Options.GetServerAddressList().Order(StringComparer.Ordinal));
        var identity = $"{servers}\n{source.Options.ContextPath}\n{testNamespace}\n{source.Options.Username}";
        var expectedDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "nacos", "config", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))));
        var existedBefore = Directory.Exists(expectedDirectory);
        var provider = new NacosConfigurationProvider(source);
        var clientField = typeof(NacosConfigurationProvider).GetField("_configService", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var original = clientField.GetValue(provider)!;
        var cache = original.GetType().GetField("_localCache", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(original)!;
        var actualDirectory = (string)cache.GetType().GetField("_cacheDir", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cache)!;
        ((IAsyncDisposable)original).DisposeAsync().AsTask().GetAwaiter().GetResult();
        if (!existedBefore && source.Options.Namespace == testNamespace
            && Path.GetFullPath(actualDirectory) == Path.GetFullPath(expectedDirectory)
            && Directory.Exists(actualDirectory) && !Directory.EnumerateFileSystemEntries(actualDirectory).Any())
        {
            Directory.Delete(actualDirectory, recursive: false);
        }
        clientField.SetValue(provider, fake);
        return provider;
    }
}
