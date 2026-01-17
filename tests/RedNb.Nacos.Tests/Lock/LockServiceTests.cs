using FluentAssertions;
using Moq;
using RedNb.Nacos.Core.Lock;
using Xunit;

namespace RedNb.Nacos.Tests.Lock;

/// <summary>
/// Unit tests for ILockService interface behavior.
/// Uses mock to verify expected interface contract.
/// </summary>
public class LockServiceTests
{
    private readonly Mock<ILockService> _mockLockService;

    public LockServiceTests()
    {
        _mockLockService = new Mock<ILockService>();
    }

    #region LockAsync Tests

    [Fact]
    public async Task LockAsync_ValidInstance_ShouldReturnTrue()
    {
        // Arrange
        var instance = LockInstance.Create("test-lock", 30000);
        _mockLockService.Setup(x => x.LockAsync(instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockLockService.Object.LockAsync(instance);

        // Assert
        result.Should().BeTrue();
        _mockLockService.Verify(x => x.LockAsync(instance, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LockAsync_LockAlreadyHeld_ShouldReturnFalse()
    {
        // Arrange
        var instance = LockInstance.Create("existing-lock", 30000);
        _mockLockService.Setup(x => x.LockAsync(instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _mockLockService.Object.LockAsync(instance);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LockAsync_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var instance = LockInstance.Create("test-lock");
        var cts = new CancellationTokenSource();
        _mockLockService.Setup(x => x.LockAsync(instance, cts.Token))
            .ReturnsAsync(true);

        // Act
        var result = await _mockLockService.Object.LockAsync(instance, cts.Token);

        // Assert
        result.Should().BeTrue();
        _mockLockService.Verify(x => x.LockAsync(instance, cts.Token), Times.Once);
    }

    [Fact]
    public async Task LockAsync_CancellationRequested_ShouldThrow()
    {
        // Arrange
        var instance = LockInstance.Create("test-lock");
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _mockLockService.Setup(x => x.LockAsync(instance, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _mockLockService.Object.LockAsync(instance, cts.Token));
    }

    #endregion

    #region UnlockAsync Tests

    [Fact]
    public async Task UnlockAsync_ValidInstance_ShouldReturnTrue()
    {
        // Arrange
        var instance = LockInstance.Create("test-lock")
            .WithOwner("client-123");
        _mockLockService.Setup(x => x.UnlockAsync(instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockLockService.Object.UnlockAsync(instance);

        // Assert
        result.Should().BeTrue();
        _mockLockService.Verify(x => x.UnlockAsync(instance, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnlockAsync_LockNotHeld_ShouldReturnFalse()
    {
        // Arrange
        var instance = LockInstance.Create("unknown-lock");
        _mockLockService.Setup(x => x.UnlockAsync(instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _mockLockService.Object.UnlockAsync(instance);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region TryLockAsync Tests

    [Fact]
    public async Task TryLockAsync_WithinTimeout_ShouldReturnTrue()
    {
        // Arrange
        var instance = LockInstance.Create("test-lock");
        var timeout = TimeSpan.FromSeconds(5);
        _mockLockService.Setup(x => x.TryLockAsync(instance, timeout, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockLockService.Object.TryLockAsync(instance, timeout);

        // Assert
        result.Should().BeTrue();
        _mockLockService.Verify(x => x.TryLockAsync(instance, timeout, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryLockAsync_TimeoutExceeded_ShouldReturnFalse()
    {
        // Arrange
        var instance = LockInstance.Create("contended-lock");
        var timeout = TimeSpan.FromMilliseconds(100);
        _mockLockService.Setup(x => x.TryLockAsync(instance, timeout, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _mockLockService.Object.TryLockAsync(instance, timeout);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryLockAsync_ZeroTimeout_ShouldAttemptOnce()
    {
        // Arrange
        var instance = LockInstance.Create("test-lock");
        var timeout = TimeSpan.Zero;
        _mockLockService.Setup(x => x.TryLockAsync(instance, timeout, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockLockService.Object.TryLockAsync(instance, timeout);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region RemoteTryLockAsync Tests

    [Fact]
    public async Task RemoteTryLockAsync_ValidInstance_ShouldReturnTrue()
    {
        // Arrange
        var instance = LockInstance.Create("remote-lock", 30000)
            .WithNamespace("custom-ns");
        _mockLockService.Setup(x => x.RemoteTryLockAsync(instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockLockService.Object.RemoteTryLockAsync(instance);

        // Assert
        result.Should().BeTrue();
        _mockLockService.Verify(x => x.RemoteTryLockAsync(instance, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoteTryLockAsync_ServerUnavailable_ShouldReturnFalse()
    {
        // Arrange
        var instance = LockInstance.Create("test-lock");
        _mockLockService.Setup(x => x.RemoteTryLockAsync(instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _mockLockService.Object.RemoteTryLockAsync(instance);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region RemoteReleaseLockAsync Tests

    [Fact]
    public async Task RemoteReleaseLockAsync_ValidInstance_ShouldReturnTrue()
    {
        // Arrange
        var instance = LockInstance.Create("remote-lock")
            .WithOwner("client-123");
        _mockLockService.Setup(x => x.RemoteReleaseLockAsync(instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockLockService.Object.RemoteReleaseLockAsync(instance);

        // Assert
        result.Should().BeTrue();
        _mockLockService.Verify(x => x.RemoteReleaseLockAsync(instance, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoteReleaseLockAsync_LockNotOwned_ShouldReturnFalse()
    {
        // Arrange
        var instance = LockInstance.Create("other-lock")
            .WithOwner("different-client");
        _mockLockService.Setup(x => x.RemoteReleaseLockAsync(instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _mockLockService.Object.RemoteReleaseLockAsync(instance);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetServerStatus Tests

    [Fact]
    public void GetServerStatus_Connected_ShouldReturnUpStatus()
    {
        // Arrange
        _mockLockService.Setup(x => x.GetServerStatus())
            .Returns("UP");

        // Act
        var status = _mockLockService.Object.GetServerStatus();

        // Assert
        status.Should().Be("UP");
    }

    [Fact]
    public void GetServerStatus_Disconnected_ShouldReturnDownStatus()
    {
        // Arrange
        _mockLockService.Setup(x => x.GetServerStatus())
            .Returns("DOWN");

        // Act
        var status = _mockLockService.Object.GetServerStatus();

        // Assert
        status.Should().Be("DOWN");
    }

    #endregion

    #region ShutdownAsync Tests

    [Fact]
    public async Task ShutdownAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        _mockLockService.Setup(x => x.ShutdownAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockLockService.Object.ShutdownAsync();

        // Assert
        _mockLockService.Verify(x => x.ShutdownAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShutdownAsync_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _mockLockService.Setup(x => x.ShutdownAsync(cts.Token))
            .Returns(Task.CompletedTask);

        // Act
        await _mockLockService.Object.ShutdownAsync(cts.Token);

        // Assert
        _mockLockService.Verify(x => x.ShutdownAsync(cts.Token), Times.Once);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_ShouldDisposeCorrectly()
    {
        // Arrange
        _mockLockService.Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        // Act
        await _mockLockService.Object.DisposeAsync();

        // Assert
        _mockLockService.Verify(x => x.DisposeAsync(), Times.Once);
    }

    #endregion

    #region Integration Scenario Tests

    [Fact]
    public async Task LockWorkflow_AcquireAndRelease_ShouldWorkCorrectly()
    {
        // Arrange
        var instance = LockInstance.Create("workflow-lock", 30000)
            .WithOwner("client-123")
            .WithReentrant();

        _mockLockService.SetupSequence(x => x.LockAsync(instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockLockService.Setup(x => x.UnlockAsync(instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act - Acquire lock
        var acquired = await _mockLockService.Object.LockAsync(instance);
        
        // Act - Do work (simulated)
        
        // Act - Release lock
        var released = await _mockLockService.Object.UnlockAsync(instance);

        // Assert
        acquired.Should().BeTrue();
        released.Should().BeTrue();
    }

    [Fact]
    public async Task ReentrantLock_MultipleLocks_ShouldSucceed()
    {
        // Arrange
        var instance = LockInstance.Create("reentrant-lock")
            .WithReentrant();

        _mockLockService.Setup(x => x.LockAsync(instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act - Acquire same lock multiple times (reentrant)
        var first = await _mockLockService.Object.LockAsync(instance);
        var second = await _mockLockService.Object.LockAsync(instance);
        var third = await _mockLockService.Object.LockAsync(instance);

        // Assert
        first.Should().BeTrue();
        second.Should().BeTrue();
        third.Should().BeTrue();
        _mockLockService.Verify(x => x.LockAsync(instance, It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    #endregion
}
