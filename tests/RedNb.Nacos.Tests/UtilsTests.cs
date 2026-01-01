namespace RedNb.Nacos.Tests;

/// <summary>
/// 工具类测试
/// </summary>
public class UtilsTests
{
    [Fact]
    public void Md5Utils_ComputeMd5_ShouldReturnCorrectHash()
    {
        // Arrange
        var input = "test";

        // Act
        var hash = Md5Utils.ComputeMd5(input);

        // Assert
        Assert.NotEmpty(hash);
        Assert.Equal(32, hash.Length);
        Assert.Equal("098f6bcd4621d373cade4e832627b4f6", hash);
    }

    [Fact]
    public void Md5Utils_ComputeMd5_EmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var input = "";

        // Act
        var hash = Md5Utils.ComputeMd5(input);

        // Assert - 空字符串返回空字符串（根据实现逻辑）
        Assert.Empty(hash);
    }

    [Fact]
    public void NetworkUtils_GetLocalIp_ShouldReturnNonLoopbackAddress()
    {
        // Act
        var ip = NetworkUtils.GetLocalIp();

        // Assert
        Assert.NotEmpty(ip);
        Assert.NotEqual("127.0.0.1", ip);
    }
}
