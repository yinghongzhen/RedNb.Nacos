using FluentAssertions;
using RedNb.Nacos.Core.Utils;
using Xunit;

namespace RedNb.Nacos.Tests;

public class NacosUtilsTests
{
    [Fact]
    public void GetMd5_ShouldReturnConsistentHash()
    {
        // Arrange
        var content = "test content";

        // Act
        var md5 = NacosUtils.GetMd5(content);

        // Assert
        md5.Should().NotBeNullOrEmpty();
        md5.Should().HaveLength(32);
    }

    [Fact]
    public void GetMd5_SameShouldReturnSameHash()
    {
        // Arrange
        var content = "test content";

        // Act
        var md5_1 = NacosUtils.GetMd5(content);
        var md5_2 = NacosUtils.GetMd5(content);

        // Assert
        md5_1.Should().Be(md5_2);
    }

    [Fact]
    public void GetMd5_DifferentContentShouldReturnDifferentHash()
    {
        // Arrange
        var content1 = "content 1";
        var content2 = "content 2";

        // Act
        var md5_1 = NacosUtils.GetMd5(content1);
        var md5_2 = NacosUtils.GetMd5(content2);

        // Assert
        md5_1.Should().NotBe(md5_2);
    }

    [Fact]
    public void GetMd5_EmptyStringShouldReturnEmpty()
    {
        // Arrange
        var content = string.Empty;

        // Act
        var md5 = NacosUtils.GetMd5(content);

        // Assert - Implementation returns empty string for empty/null input
        md5.Should().BeEmpty();
    }

    [Fact]
    public void IsIpv4_ValidIpv4_ShouldReturnTrue()
    {
        // Arrange
        var ip = "192.168.1.1";

        // Act
        var result = NacosUtils.IsIpv4(ip);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsIpv4_LocalhostIp_ShouldReturnTrue()
    {
        // Arrange
        var ip = "127.0.0.1";

        // Act
        var result = NacosUtils.IsIpv4(ip);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsIpv4_Ipv6_ShouldReturnFalse()
    {
        // Arrange
        var ip = "::1";

        // Act
        var result = NacosUtils.IsIpv4(ip);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpv4_InvalidString_ShouldReturnFalse()
    {
        // Arrange
        var ip = "not an ip";

        // Act
        var result = NacosUtils.IsIpv4(ip);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpv4_NullString_ShouldReturnFalse()
    {
        // Act
        var result = NacosUtils.IsIpv4(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsBlank_NullString_ShouldReturnTrue()
    {
        // Act
        var result = NacosUtils.IsBlank(null);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsBlank_EmptyString_ShouldReturnTrue()
    {
        // Act
        var result = NacosUtils.IsBlank(string.Empty);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsBlank_WhitespaceString_ShouldReturnTrue()
    {
        // Act
        var result = NacosUtils.IsBlank("   ");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsBlank_NonEmptyString_ShouldReturnFalse()
    {
        // Act
        var result = NacosUtils.IsBlank("test");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsNotBlank_NonEmptyString_ShouldReturnTrue()
    {
        // Act
        var result = NacosUtils.IsNotBlank("test");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsNotBlank_EmptyString_ShouldReturnFalse()
    {
        // Act
        var result = NacosUtils.IsNotBlank("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetCurrentTimeMillis_ShouldReturnPositiveValue()
    {
        // Act
        var timestamp = NacosUtils.GetCurrentTimeMillis();

        // Assert
        timestamp.Should().BePositive();
    }

    [Fact]
    public void GetCurrentTimeMillis_ShouldIncrease()
    {
        // Arrange
        var timestamp1 = NacosUtils.GetCurrentTimeMillis();
        Thread.Sleep(10);

        // Act
        var timestamp2 = NacosUtils.GetCurrentTimeMillis();

        // Assert
        timestamp2.Should().BeGreaterThanOrEqualTo(timestamp1);
    }
}
