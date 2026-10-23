using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Moq;
using RedNb.Nacos.AspNetCore.Configuration;
using RedNb.Nacos.Config;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Regression;

public class ConfigurationAtomicityTests
{
    [Fact]
    public void RequiredInitialMalformedJsonThrowsAndDisposesExactlyOnce()
    {
        var source = new NacosConfigurationSource { ReloadOnChange = false };
        source.ConfigItems.Add(new() { DataId = "required" });
        var fake = Client(_ => "{invalid}");
        var provider = InvalidJsonUpdateTests.CreateProvider(source, fake.Object);
        Assert.ThrowsAny<JsonException>(() => provider.Load());
        provider.Dispose();
        fake.Verify(c => c.DisposeAsync(), Times.Once);
    }

    [Fact]
    public void FailedExplicitReloadPreservesAllSourcesAndAllowsNextValidReload()
    {
        var source = new NacosConfigurationSource { ReloadOnChange = false };
        source.ConfigItems.Add(new() { DataId = "a" }); source.ConfigItems.Add(new() { DataId = "b" });
        var values = new Dictionary<string, string?> { ["a"] = "{\"A\":\"old\"}", ["b"] = "{\"B\":\"old\"}" };
        var fake = Client(id => values[id]);
        using var provider = InvalidJsonUpdateTests.CreateProvider(source, fake.Object);
        using var root = new ConfigurationRoot([provider]);
        var token = root.GetReloadToken();
        values["a"] = "{\"A\":\"new\"}"; values["b"] = "{invalid}";
        Assert.ThrowsAny<JsonException>(root.Reload);
        Assert.Equal("old", root["A"]); Assert.Equal("old", root["B"]); Assert.False(token.HasChanged);
        fake.Verify(c => c.DisposeAsync(), Times.Never);
        values["b"] = "{\"B\":\"new\"}"; root.Reload();
        Assert.Equal("new", root["A"]); Assert.Equal("new", root["B"]);
    }

    [Theory]
    [InlineData(null, "base")]
    [InlineData("", "base")]
    [InlineData("{invalid}", "overlay")]
    public void OptionalReloadDistinguishesAbsenceFromInvalidContent(string? next, string expected)
    {
        var source = new NacosConfigurationSource { ReloadOnChange = false };
        source.ConfigItems.Add(new() { DataId = "base" }); source.ConfigItems.Add(new() { DataId = "overlay", Optional = true });
        string? value = "{\"Value\":\"overlay\"}";
        var fake = Client(id => id == "base" ? "{\"Value\":\"base\"}" : value);
        using var provider = InvalidJsonUpdateTests.CreateProvider(source, fake.Object);
        using var root = new ConfigurationRoot([provider]);
        value = next; root.Reload();
        Assert.Equal(expected, root["Value"]);
    }

    [Fact]
    public void MissingOptionalItemStillSubscribesAndReceivesLaterCreation()
    {
        var source = new NacosConfigurationSource { ReloadOnChange = true };
        source.ConfigItems.Add(new() { DataId = "later", Optional = true });
        var fake = Client(_ => null);
        IConfigChangeListener? listener = null;
        fake.Setup(c => c.AddListenerAsync("later", "DEFAULT_GROUP", It.IsAny<IConfigChangeListener>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IConfigChangeListener, CancellationToken>((_, _, l, _) => listener = l).Returns(Task.CompletedTask);
        using var provider = InvalidJsonUpdateTests.CreateProvider(source, fake.Object);
        using var root = new ConfigurationRoot([provider]);
        Assert.NotNull(listener);
        listener.OnReceiveConfigInfo(new ConfigInfo { Content = "{\"Created\":true}" });
        Assert.Equal("True", root["Created"], ignoreCase: true);
        listener.OnReceiveConfigInfo(new ConfigInfo { Content = null });
        Assert.Null(root["Created"]);
    }

    private static Mock<IConfigService> Client(Func<string, string?> read)
    {
        var fake = new Mock<IConfigService>(MockBehavior.Strict);
        fake.Setup(c => c.GetConfigAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, string _, long _, CancellationToken _) => read(id));
        fake.Setup(c => c.RemoveListener(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IConfigChangeListener>()));
        fake.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return fake;
    }
}
