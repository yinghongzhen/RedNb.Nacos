using Xunit;

namespace RedNb.Nacos.Tests;

public class ConsoleUrlTests
{
    [Theory]
    [InlineData("localhost:8080", false, "http://localhost:8080")]
    [InlineData("localhost:8080", true, "https://localhost:8080")]
    [InlineData("http://localhost:8080", false, "http://localhost:8080")]
    [InlineData("https://localhost:8443", false, "https://localhost:8443")]
    [InlineData("http://localhost:8080", true, "http://localhost:8080")]
    [InlineData("https://localhost:8443/", true, "https://localhost:8443")]
    [InlineData(" HTTP://localhost:8080/ ", false, "HTTP://localhost:8080")]
    [InlineData("[::1]:8080", false, "http://[::1]:8080")]
    public void ConsoleUrlAddsSchemeOnlyWhenMissing(string address, bool tls, string expected)
    {
        var options = new NacosClientOptions { EnableTls = tls };
        Assert.Equal(expected, options.GetConsoleBaseUrl(address));
        Assert.Equal(expected + "/nacos", options.GetBaseUrl(address));
    }

    [Theory]
    [InlineData("http://localhost:8848", "http://localhost:8080")]
    [InlineData("https://localhost:8848", "https://localhost:8080")]
    public void DerivedConsoleAddressPreservesExplicitScheme(string address, string expected)
    {
        Assert.Equal(expected, Assert.Single(new NacosClientOptions { ServerAddresses = address }.GetConsoleAddressList()));
    }
}
