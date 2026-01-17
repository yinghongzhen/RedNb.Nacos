using FluentAssertions;
using RedNb.Nacos.Core.Lock;
using Xunit;

namespace RedNb.Nacos.Tests.Lock;

/// <summary>
/// Unit tests for LockInstance class.
/// </summary>
public class LockInstanceTests
{
    #region Factory Methods Tests

    [Fact]
    public void Create_WithKey_ShouldSetKeyAndDefaultValues()
    {
        // Arrange
        const string key = "test-lock-key";

        // Act
        var instance = LockInstance.Create(key);

        // Assert
        instance.Key.Should().Be(key);
        instance.ExpireTime.Should().Be(0);
        instance.LockType.Should().Be(LockConstants.DefaultLockType);
        instance.NamespaceId.Should().BeNull();
        instance.Owner.Should().BeNull();
        instance.Params.Should().BeNull();
        instance.Reentrant.Should().BeFalse();
    }

    [Fact]
    public void Create_WithKeyAndExpireTime_ShouldSetBothValues()
    {
        // Arrange
        const string key = "test-lock-key";
        const long expireTime = 30000;

        // Act
        var instance = LockInstance.Create(key, expireTime);

        // Assert
        instance.Key.Should().Be(key);
        instance.ExpireTime.Should().Be(expireTime);
        instance.LockType.Should().Be(LockConstants.DefaultLockType);
    }

    [Fact]
    public void Create_WithKeyExpireTimeAndLockType_ShouldSetAllValues()
    {
        // Arrange
        const string key = "test-lock-key";
        const long expireTime = 30000;
        const string lockType = "custom-lock";

        // Act
        var instance = LockInstance.Create(key, expireTime, lockType);

        // Assert
        instance.Key.Should().Be(key);
        instance.ExpireTime.Should().Be(expireTime);
        instance.LockType.Should().Be(lockType);
    }

    #endregion

    #region Fluent API Tests

    [Fact]
    public void WithExpireTime_Milliseconds_ShouldSetAndReturnSameInstance()
    {
        // Arrange
        var instance = LockInstance.Create("key");
        const long expireTime = 60000;

        // Act
        var result = instance.WithExpireTime(expireTime);

        // Assert
        result.Should().BeSameAs(instance);
        result.ExpireTime.Should().Be(expireTime);
    }

    [Fact]
    public void WithExpireTime_TimeSpan_ShouldConvertAndSetValue()
    {
        // Arrange
        var instance = LockInstance.Create("key");
        var expireTime = TimeSpan.FromSeconds(30);

        // Act
        var result = instance.WithExpireTime(expireTime);

        // Assert
        result.Should().BeSameAs(instance);
        result.ExpireTime.Should().Be(30000);
    }

    [Fact]
    public void WithExpireTime_TimeSpanMinutes_ShouldConvertCorrectly()
    {
        // Arrange
        var instance = LockInstance.Create("key");
        var expireTime = TimeSpan.FromMinutes(5);

        // Act
        var result = instance.WithExpireTime(expireTime);

        // Assert
        result.ExpireTime.Should().Be(300000);
    }

    [Fact]
    public void WithLockType_ShouldSetAndReturnSameInstance()
    {
        // Arrange
        var instance = LockInstance.Create("key");
        const string lockType = "redis-lock";

        // Act
        var result = instance.WithLockType(lockType);

        // Assert
        result.Should().BeSameAs(instance);
        result.LockType.Should().Be(lockType);
    }

    [Fact]
    public void WithNamespace_ShouldSetAndReturnSameInstance()
    {
        // Arrange
        var instance = LockInstance.Create("key");
        const string namespaceId = "custom-namespace";

        // Act
        var result = instance.WithNamespace(namespaceId);

        // Assert
        result.Should().BeSameAs(instance);
        result.NamespaceId.Should().Be(namespaceId);
    }

    [Fact]
    public void WithOwner_ShouldSetAndReturnSameInstance()
    {
        // Arrange
        var instance = LockInstance.Create("key");
        const string owner = "client-123";

        // Act
        var result = instance.WithOwner(owner);

        // Assert
        result.Should().BeSameAs(instance);
        result.Owner.Should().Be(owner);
    }

    [Fact]
    public void WithParam_SingleParam_ShouldAddToParamsDictionary()
    {
        // Arrange
        var instance = LockInstance.Create("key");

        // Act
        var result = instance.WithParam("timeout", 5000);

        // Assert
        result.Should().BeSameAs(instance);
        result.Params.Should().NotBeNull();
        result.Params.Should().ContainKey("timeout");
        result.Params!["timeout"].Should().Be(5000);
    }

    [Fact]
    public void WithParam_MultipleParams_ShouldAddAllToParamsDictionary()
    {
        // Arrange
        var instance = LockInstance.Create("key");

        // Act
        var result = instance
            .WithParam("timeout", 5000)
            .WithParam("retries", 3)
            .WithParam("enabled", true);

        // Assert
        result.Params.Should().HaveCount(3);
        result.Params!["timeout"].Should().Be(5000);
        result.Params["retries"].Should().Be(3);
        result.Params["enabled"].Should().Be(true);
    }

    [Fact]
    public void WithParam_SameKeyTwice_ShouldOverwriteValue()
    {
        // Arrange
        var instance = LockInstance.Create("key");

        // Act
        var result = instance
            .WithParam("timeout", 5000)
            .WithParam("timeout", 10000);

        // Assert
        result.Params.Should().HaveCount(1);
        result.Params!["timeout"].Should().Be(10000);
    }

    [Fact]
    public void WithReentrant_Default_ShouldSetToTrue()
    {
        // Arrange
        var instance = LockInstance.Create("key");

        // Act
        var result = instance.WithReentrant();

        // Assert
        result.Should().BeSameAs(instance);
        result.Reentrant.Should().BeTrue();
    }

    [Fact]
    public void WithReentrant_False_ShouldSetToFalse()
    {
        // Arrange
        var instance = LockInstance.Create("key");

        // Act
        var result = instance.WithReentrant(false);

        // Assert
        result.Reentrant.Should().BeFalse();
    }

    #endregion

    #region Fluent Chain Tests

    [Fact]
    public void FluentChain_FullConfiguration_ShouldSetAllProperties()
    {
        // Arrange & Act
        var instance = LockInstance.Create("my-lock")
            .WithExpireTime(TimeSpan.FromMinutes(5))
            .WithLockType("distributed")
            .WithNamespace("prod-namespace")
            .WithOwner("service-A")
            .WithParam("priority", "high")
            .WithParam("maxRetries", 5)
            .WithReentrant();

        // Assert
        instance.Key.Should().Be("my-lock");
        instance.ExpireTime.Should().Be(300000);
        instance.LockType.Should().Be("distributed");
        instance.NamespaceId.Should().Be("prod-namespace");
        instance.Owner.Should().Be("service-A");
        instance.Reentrant.Should().BeTrue();
        instance.Params.Should().HaveCount(2);
        instance.Params!["priority"].Should().Be("high");
        instance.Params["maxRetries"].Should().Be(5);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_BasicInstance_ShouldReturnFormattedString()
    {
        // Arrange
        var instance = LockInstance.Create("test-key", 30000);

        // Act
        var result = instance.ToString();

        // Assert
        result.Should().Contain("test-key");
        result.Should().Contain("30000");
        result.Should().Contain(LockConstants.DefaultLockType);
    }

    [Fact]
    public void ToString_WithOwner_ShouldIncludeOwner()
    {
        // Arrange
        var instance = LockInstance.Create("test-key")
            .WithOwner("my-client");

        // Act
        var result = instance.ToString();

        // Assert
        result.Should().Contain("my-client");
    }

    [Fact]
    public void ToString_Format_ShouldMatchExpectedPattern()
    {
        // Arrange
        var instance = LockInstance.Create("key", 1000, "nacos")
            .WithOwner("owner1");

        // Act
        var result = instance.ToString();

        // Assert
        result.Should().Be("LockInstance[key=key, expireTime=1000, lockType=nacos, owner=owner1]");
    }

    #endregion

    #region Property Direct Set Tests

    [Fact]
    public void Properties_DirectSet_ShouldWorkCorrectly()
    {
        // Arrange & Act
        var instance = new LockInstance
        {
            Key = "direct-key",
            ExpireTime = 15000,
            LockType = "custom",
            NamespaceId = "ns1",
            Owner = "owner1",
            AcquireTime = 1234567890,
            Reentrant = true,
            Params = new Dictionary<string, object> { { "test", "value" } }
        };

        // Assert
        instance.Key.Should().Be("direct-key");
        instance.ExpireTime.Should().Be(15000);
        instance.LockType.Should().Be("custom");
        instance.NamespaceId.Should().Be("ns1");
        instance.Owner.Should().Be("owner1");
        instance.AcquireTime.Should().Be(1234567890);
        instance.Reentrant.Should().BeTrue();
        instance.Params.Should().ContainKey("test");
    }

    [Fact]
    public void DefaultConstructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var instance = new LockInstance();

        // Assert
        instance.Key.Should().BeEmpty();
        instance.ExpireTime.Should().Be(0);
        instance.LockType.Should().Be(LockConstants.DefaultLockType);
        instance.NamespaceId.Should().BeNull();
        instance.Owner.Should().BeNull();
        instance.Params.Should().BeNull();
        instance.AcquireTime.Should().Be(0);
        instance.Reentrant.Should().BeFalse();
    }

    #endregion
}
