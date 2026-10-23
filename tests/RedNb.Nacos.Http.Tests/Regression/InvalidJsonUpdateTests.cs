using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using RedNb.Nacos.AspNetCore.Configuration;
using RedNb.Nacos.Config;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Regression;

public sealed class InvalidJsonUpdateTests(ITestOutputHelper output)
{
    [Fact]
    public void MalformedJsonMustNotDeleteValidConfigurationOrSucceedAsRequiredInitialData()
    {
        var source = new NacosConfigurationSource { ReloadOnChange = true };
        source.ConfigItems.Add(new() { DataId = "settings.json", Optional = false });
        var fake = new Mock<IConfigService>(MockBehavior.Strict);
        string content = "{\"Name\":\"working\"}";
        IConfigChangeListener? listener = null;
        fake.Setup(x => x.GetConfigAsync("settings.json", "DEFAULT_GROUP", It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => content);
        fake.Setup(x => x.AddListenerAsync("settings.json", "DEFAULT_GROUP", It.IsAny<IConfigChangeListener>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IConfigChangeListener, CancellationToken>((_, _, value, _) => listener = value)
            .Returns(Task.CompletedTask);
        fake.Setup(x => x.RemoveListener(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IConfigChangeListener>()));
        fake.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        using var provider = CreateProvider(source, fake.Object);
        provider.Load();
        Assert.True(provider.TryGet("Name", out var before));
        Assert.Equal("working", before);
        var reloadToken = provider.GetReloadToken();

        listener!.OnReceiveConfigInfo(new ConfigInfo { Content = "{invalid}" });

        var foundAfter = provider.TryGet("Name", out var after);
        content = "{invalid}";
        using var requiredInitial = CreateProvider(source, fake.Object);
        var initialError = Record.Exception(requiredInitial.Load);
        output.WriteLine($"After malformed push: found={foundAfter}, value={after ?? "<null>"}, tokenChanged={reloadToken.HasChanged}");
        output.WriteLine($"Required malformed initial Load exception: {initialError?.GetType().Name ?? "<none>"}");
        Assert.True(foundAfter, "Malformed JSON must not remove the last successfully parsed configuration.");
        Assert.Equal("working", after);
        Assert.False(reloadToken.HasChanged);
        Assert.NotNull(initialError);
    }

    internal static NacosConfigurationProvider CreateProvider(NacosConfigurationSource source, IConfigService fake)
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
