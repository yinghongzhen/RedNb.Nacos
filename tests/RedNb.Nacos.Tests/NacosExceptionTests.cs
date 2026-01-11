using FluentAssertions;
using RedNb.Nacos.Core;
using Xunit;

namespace RedNb.Nacos.Tests;

public class NacosExceptionTests
{
    [Fact]
    public void NacosException_WithMessageOnly_ShouldRetainMessage()
    {
        // Act
        var exception = new NacosException("Test error message");

        // Assert
        exception.Message.Should().Be("Test error message");
        exception.ErrorCode.Should().Be(0);
    }

    [Fact]
    public void NacosException_WithErrorCodeAndMessage_ShouldRetainBoth()
    {
        // Act
        var exception = new NacosException(NacosException.ClientInvalidParam, "Invalid parameter");

        // Assert
        exception.ErrorCode.Should().Be(NacosException.ClientInvalidParam);
        exception.Message.Should().Be("Invalid parameter");
    }

    [Fact]
    public void NacosException_WithInnerException_ShouldRetainInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner error");

        // Act
        var exception = new NacosException(NacosException.ServerError, "Server error", inner);

        // Assert
        exception.InnerException.Should().BeSameAs(inner);
        exception.ErrorCode.Should().Be(NacosException.ServerError);
    }

    [Fact]
    public void NacosException_ErrorCodeConstants_ShouldHaveExpectedValues()
    {
        // Assert
        NacosException.ClientInvalidParam.Should().Be(-400);
        NacosException.ClientOverThreshold.Should().Be(-503);
        NacosException.ClientDisconnect.Should().Be(-401); // Actual value in implementation
        NacosException.ServerError.Should().Be(500);
        NacosException.BadGateway.Should().Be(502);
        NacosException.Conflict.Should().Be(409);
    }

    [Fact]
    public void NacosException_IsClientError_ShouldIdentifyClientErrors()
    {
        // Arrange
        var clientException = new NacosException(NacosException.ClientInvalidParam, "Client error");
        var serverException = new NacosException(NacosException.ServerError, "Server error");

        // Act & Assert
        clientException.ErrorCode.Should().BeLessThan(0);
        serverException.ErrorCode.Should().BeGreaterThan(0);
    }
}
