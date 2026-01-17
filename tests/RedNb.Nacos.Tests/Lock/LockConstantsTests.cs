using FluentAssertions;
using RedNb.Nacos.Core.Lock;
using Xunit;

namespace RedNb.Nacos.Tests.Lock;

/// <summary>
/// Unit tests for LockConstants class.
/// </summary>
public class LockConstantsTests
{
    [Fact]
    public void DefaultLockType_ShouldBeNacos()
    {
        LockConstants.DefaultLockType.Should().Be("nacos");
    }

    [Fact]
    public void LockTypeRedis_ShouldBeRedis()
    {
        LockConstants.LockTypeRedis.Should().Be("redis");
    }

    [Fact]
    public void LockTypeZookeeper_ShouldBeZookeeper()
    {
        LockConstants.LockTypeZookeeper.Should().Be("zookeeper");
    }

    [Fact]
    public void LockTypeEtcd_ShouldBeEtcd()
    {
        LockConstants.LockTypeEtcd.Should().Be("etcd");
    }

    [Fact]
    public void DefaultExpireTime_ShouldBe30Seconds()
    {
        LockConstants.DefaultExpireTime.Should().Be(30000);
    }

    [Fact]
    public void DefaultAcquireTimeout_ShouldBe10Seconds()
    {
        LockConstants.DefaultAcquireTimeout.Should().Be(10000);
    }

    [Fact]
    public void RetryInterval_ShouldBe100Milliseconds()
    {
        LockConstants.RetryInterval.Should().Be(100);
    }

    [Fact]
    public void HeartbeatInterval_ShouldBe10Seconds()
    {
        LockConstants.HeartbeatInterval.Should().Be(10000);
    }

    [Fact]
    public void LockApiPath_ShouldBeCorrect()
    {
        LockConstants.LockApiPath.Should().Be("/nacos/v3/lock");
    }

    [Fact]
    public void LockAcquirePath_ShouldBeCorrect()
    {
        LockConstants.LockAcquirePath.Should().Be("/nacos/v3/lock/acquire");
    }

    [Fact]
    public void LockReleasePath_ShouldBeCorrect()
    {
        LockConstants.LockReleasePath.Should().Be("/nacos/v3/lock/release");
    }

    [Fact]
    public void LockQueryPath_ShouldBeCorrect()
    {
        LockConstants.LockQueryPath.Should().Be("/nacos/v3/lock/query");
    }

    [Fact]
    public void GrpcLockAcquireType_ShouldBeCorrect()
    {
        LockConstants.GrpcLockAcquireType.Should().Be("LockAcquireRequest");
    }

    [Fact]
    public void GrpcLockReleaseType_ShouldBeCorrect()
    {
        LockConstants.GrpcLockReleaseType.Should().Be("LockReleaseRequest");
    }

    [Fact]
    public void GrpcLockQueryType_ShouldBeCorrect()
    {
        LockConstants.GrpcLockQueryType.Should().Be("LockQueryRequest");
    }
}
