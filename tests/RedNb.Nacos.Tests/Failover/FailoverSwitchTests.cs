using FluentAssertions;
using RedNb.Nacos.Failover;
using Xunit;

namespace RedNb.Nacos.Tests.Failover;

/// <summary>
/// Unit tests for FailoverSwitch class.
/// </summary>
public class FailoverSwitchTests
{
    [Fact]
    public void CreateEnabled_ShouldReturnEnabledSwitch()
    {
        // Act
        var fSwitch = FailoverSwitch.CreateEnabled();

        // Assert
        fSwitch.Enabled.Should().BeTrue();
    }

    [Fact]
    public void CreateDisabled_ShouldReturnDisabledSwitch()
    {
        // Act
        var fSwitch = FailoverSwitch.CreateDisabled();

        // Assert
        fSwitch.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Enabled_SetProperty_ShouldUpdateValue()
    {
        // Arrange
        var fSwitch = FailoverSwitch.CreateDisabled();

        // Act
        fSwitch.Enabled = true;

        // Assert
        fSwitch.Enabled.Should().BeTrue();
    }

    [Fact]
    public void DefaultConstructor_ShouldBeDisabled()
    {
        // Arrange & Act
        var fSwitch = new FailoverSwitch();

        // Assert
        fSwitch.Enabled.Should().BeFalse();
    }
}
