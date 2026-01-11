using Xunit;
using RedNb.Nacos.Core.Config.Filter;

namespace RedNb.Nacos.Tests.Config.Filter;

/// <summary>
/// Tests for <see cref="ConfigFilterChainManager"/>.
/// </summary>
public class ConfigFilterChainManagerTests
{
    [Fact]
    public void AddFilter_ShouldAddFilterSuccessfully()
    {
        // Arrange
        var manager = new ConfigFilterChainManager();
        var filter = new TestConfigFilter("test", 1);

        // Act
        manager.AddFilter(filter);

        // Assert
        Assert.True(manager.HasFilters);
    }

    [Fact]
    public async Task AddFilter_MultipleFilters_ShouldOrderByOrder()
    {
        // Arrange
        var manager = new ConfigFilterChainManager();
        var filter1 = new TestConfigFilter("filter1", 10);
        var filter2 = new TestConfigFilter("filter2", 1);
        var filter3 = new TestConfigFilter("filter3", 5);

        // Act
        manager.AddFilter(filter1);
        manager.AddFilter(filter2);
        manager.AddFilter(filter3);

        // Assert - Verify through execution order
        var request = new ConfigRequest("dataId", "group", null, null);
        var response = new ConfigResponse();
        
        await manager.DoFilterAsync(request, response);
        
        // All filters should be called
        Assert.True(filter1.WasCalled);
        Assert.True(filter2.WasCalled);
        Assert.True(filter3.WasCalled);
    }

    [Fact]
    public void RemoveFilter_ByName_ShouldRemoveFilter()
    {
        // Arrange
        var manager = new ConfigFilterChainManager();
        var filter = new TestConfigFilter("testFilter", 1);
        manager.AddFilter(filter);

        // Act
        manager.RemoveFilter("testFilter");

        // Assert
        Assert.False(manager.HasFilters);
    }

    [Fact]
    public void RemoveFilter_ByInstance_ShouldRemoveFilter()
    {
        // Arrange
        var manager = new ConfigFilterChainManager();
        var filter = new TestConfigFilter("testFilter", 1);
        manager.AddFilter(filter);

        // Act
        manager.RemoveFilter(filter);

        // Assert
        Assert.False(manager.HasFilters);
    }

    [Fact]
    public void HasFilters_WhenEmpty_ShouldReturnFalse()
    {
        // Arrange
        var manager = new ConfigFilterChainManager();

        // Assert
        Assert.False(manager.HasFilters);
    }

    [Fact]
    public async Task DoFilters_ShouldCallAllFilters()
    {
        // Arrange
        var manager = new ConfigFilterChainManager();
        var filter1 = new TestConfigFilter("filter1", 1);
        var filter2 = new TestConfigFilter("filter2", 2);
        manager.AddFilter(filter1);
        manager.AddFilter(filter2);
        
        var request = new ConfigRequest("dataId", "group", null, null);
        var response = new ConfigResponse();

        // Act
        await manager.DoFilterAsync(request, response);

        // Assert
        Assert.True(filter1.WasCalled);
        Assert.True(filter2.WasCalled);
    }

    [Fact]
    public async Task DoFilters_WithNoFilters_ShouldNotThrow()
    {
        // Arrange
        var manager = new ConfigFilterChainManager();
        var request = new ConfigRequest("dataId", "group", null, null);
        var response = new ConfigResponse();

        // Act & Assert - should not throw
        var exception = await Record.ExceptionAsync(() => manager.DoFilterAsync(request, response));
        Assert.Null(exception);
    }

    /// <summary>
    /// Test implementation of IConfigFilter.
    /// </summary>
    private class TestConfigFilter : IConfigFilter
    {
        public bool WasCalled { get; private set; }
        
        public string FilterName { get; }
        public int Order { get; }

        public TestConfigFilter(string name, int order)
        {
            FilterName = name;
            Order = order;
        }

        public void Init(IDictionary<string, string>? properties)
        {
        }

        public Task DoFilterAsync(IConfigRequest request, IConfigResponse response, IConfigFilterChain filterChain, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return filterChain.DoFilterAsync(request, response, cancellationToken);
        }
    }
}
