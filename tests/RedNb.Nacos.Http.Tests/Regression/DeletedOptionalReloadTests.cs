using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using RedNb.Nacos.AspNetCore.Configuration;
using RedNb.Nacos.Config;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Regression;

public sealed class DeletedOptionalReloadTests(ITestOutputHelper output)
{
    [Fact]
    public void ExplicitReloadMustRemoveKeysOfDeletedOptionalConfiguration()
    {
        var source = new NacosConfigurationSource { ReloadOnChange = false };
        source.ConfigItems.Add(new() { DataId = "optional.json", Optional = true });
        var fake = new Mock<IConfigService>(MockBehavior.Strict);
        string? content = "{\"Value\":\"old\"}";
        fake.Setup(x => x.GetConfigAsync("optional.json", "DEFAULT_GROUP", It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => content);
        fake.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        using var provider = CreateProvider(source, fake.Object);
        using var root = new ConfigurationRoot(new[] { provider });
        Assert.Equal("old", root["Value"]);

        content = null;
        root.Reload();

        output.WriteLine($"After deleting optional configuration and explicit Reload: Value={root["Value"] ?? "<null>"}");
        Assert.Null(root["Value"]);
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
