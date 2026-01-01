namespace RedNb.Nacos.Tests;

/// <summary>
/// NacosOptions 配置测试
/// </summary>
public class NacosOptionsTests
{
    [Fact]
    public void NacosOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new NacosOptions { ServerAddresses = new List<string>() };

        // Assert
        Assert.Empty(options.ServerAddresses);
        Assert.Equal("public", options.Namespace); // 默认命名空间是 "public"
        Assert.Null(options.UserName);
        Assert.Null(options.Password);
        Assert.NotNull(options.Naming);
        Assert.NotNull(options.Config);
        Assert.NotNull(options.Auth);
    }

    [Fact]
    public void NacosNamingOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new NacosNamingOptions();

        // Assert
        Assert.Equal(NacosConstants.DefaultGroup, options.GroupName);
        Assert.Equal(NacosConstants.DefaultCluster, options.ClusterName);
        Assert.True(options.RegisterEnabled);
        Assert.Equal(100, options.Weight); // 默认权重是 100
        Assert.True(options.Ephemeral);
        Assert.True(options.InstanceEnabled);
        Assert.False(options.Secure);
        Assert.Equal(5000, options.HeartbeatIntervalMs);
        Assert.NotNull(options.Metadata);
    }

    [Fact]
    public void NacosConfigOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new NacosConfigOptions();

        // Assert
        Assert.True(options.EnableSnapshot);
        Assert.Equal(30000, options.LongPollingTimeout);
        Assert.NotNull(options.Listeners);
        Assert.Empty(options.Listeners);
    }

    [Fact]
    public void NacosAuthOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new NacosAuthOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(1800, options.TokenRefreshIntervalSeconds);
        Assert.Equal(3, options.TokenRetryCount);
    }
}
