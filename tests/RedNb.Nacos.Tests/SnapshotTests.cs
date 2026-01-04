using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RedNb.Nacos.Common.Failover;
using RedNb.Nacos.Common.Options;

namespace RedNb.Nacos.Tests;

/// <summary>
/// 快照/容灾功能测试
/// </summary>
public class SnapshotTests : IDisposable
{
    private readonly string _testDir;
    private readonly Mock<ILogger<LocalFileConfigSnapshot>> _configLoggerMock;
    private readonly Mock<ILogger<LocalFileServiceSnapshot>> _serviceLoggerMock;
    private readonly IOptions<NacosOptions> _options;

    public SnapshotTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "nacos-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);

        _configLoggerMock = new Mock<ILogger<LocalFileConfigSnapshot>>();
        _serviceLoggerMock = new Mock<ILogger<LocalFileServiceSnapshot>>();

        _options = Options.Create(new NacosOptions
        {
            ServerAddresses = ["http://localhost:8848"],
            Config = new NacosConfigOptions
            {
                EnableSnapshot = true,
                SnapshotPath = _testDir
            }
        });
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public async Task ConfigSnapshot_SaveAndLoad_ShouldWorkCorrectly()
    {
        // Arrange
        var snapshot = new LocalFileConfigSnapshot(_options, _configLoggerMock.Object);
        var dataId = "test-config";
        var group = "DEFAULT_GROUP";
        var tenant = "test-tenant";
        var content = "key=value";

        // Act
        await snapshot.SaveSnapshotAsync(dataId, group, tenant, content);
        var loaded = await snapshot.GetSnapshotAsync(dataId, group, tenant);

        // Assert
        Assert.Equal(content, loaded);
    }

    [Fact]
    public async Task ConfigSnapshot_LoadNonExistent_ShouldReturnNull()
    {
        // Arrange
        var snapshot = new LocalFileConfigSnapshot(_options, _configLoggerMock.Object);

        // Act
        var loaded = await snapshot.GetSnapshotAsync("non-existent", "DEFAULT_GROUP", "test-tenant");

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task ConfigSnapshot_GetMd5_ShouldReturnCorrectHash()
    {
        // Arrange
        var snapshot = new LocalFileConfigSnapshot(_options, _configLoggerMock.Object);
        var dataId = "test-config-md5";
        var group = "DEFAULT_GROUP";
        var tenant = "test-tenant";
        var content = "key=value";

        // Act
        await snapshot.SaveSnapshotAsync(dataId, group, tenant, content);
        var md5 = await snapshot.GetMd5Async(dataId, group, tenant);

        // Assert
        Assert.NotNull(md5);
        Assert.NotEmpty(md5);
    }

    [Fact]
    public async Task ServiceSnapshot_SaveAndLoad_ShouldWorkCorrectly()
    {
        // Arrange - ServiceSnapshot also uses Config.EnableSnapshot
        var serviceOptions = Options.Create(new NacosOptions
        {
            ServerAddresses = ["http://localhost:8848"],
            Config = new NacosConfigOptions
            {
                EnableSnapshot = true,
                SnapshotPath = _testDir
            }
        });
        var snapshot = new LocalFileServiceSnapshot(serviceOptions, _serviceLoggerMock.Object);
        var serviceName = "test-service";
        var groupName = "DEFAULT_GROUP";
        var tenant = "";
        var serviceInfo = new ServiceInfo
        {
            Name = serviceName,
            GroupName = groupName,
            Hosts = new List<Instance>
            {
                new() { Ip = "192.168.1.1", Port = 8080, Weight = 1.0, Healthy = true }
            }
        };

        // Act
        await snapshot.SaveSnapshotAsync(serviceName, groupName, tenant, serviceInfo);
        var loaded = await snapshot.GetSnapshotAsync(serviceName, groupName, tenant);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(serviceName, loaded.Name);
        Assert.Single(loaded.Hosts);
        Assert.Equal("192.168.1.1", loaded.Hosts[0].Ip);
    }

    [Fact]
    public async Task ServiceSnapshot_LoadNonExistent_ShouldReturnNull()
    {
        // Arrange
        var snapshot = new LocalFileServiceSnapshot(_options, _serviceLoggerMock.Object);

        // Act
        var loaded = await snapshot.GetSnapshotAsync("non-existent", "DEFAULT_GROUP", "");

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task ServiceSnapshot_Delete_ShouldRemoveFile()
    {
        // Arrange
        var serviceOptions = Options.Create(new NacosOptions
        {
            ServerAddresses = ["http://localhost:8848"],
            Config = new NacosConfigOptions
            {
                EnableSnapshot = true,
                SnapshotPath = _testDir
            }
        });
        var snapshot = new LocalFileServiceSnapshot(serviceOptions, _serviceLoggerMock.Object);
        var serviceName = "test-service-delete";
        var groupName = "DEFAULT_GROUP";
        var tenant = "";
        var serviceInfo = new ServiceInfo { Name = serviceName, GroupName = groupName, Hosts = [] };

        await snapshot.SaveSnapshotAsync(serviceName, groupName, tenant, serviceInfo);

        // Act
        await snapshot.DeleteSnapshotAsync(serviceName, groupName, tenant);
        var loaded = await snapshot.GetSnapshotAsync(serviceName, groupName, tenant);

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task ConfigSnapshot_Disabled_ShouldNotSave()
    {
        // Arrange
        var disabledOptions = Options.Create(new NacosOptions
        {
            ServerAddresses = ["http://localhost:8848"],
            Config = new NacosConfigOptions { EnableSnapshot = false }
        });
        var snapshot = new LocalFileConfigSnapshot(disabledOptions, _configLoggerMock.Object);

        // Act
        await snapshot.SaveSnapshotAsync("test", "group", "tenant", "content");
        var loaded = await snapshot.GetSnapshotAsync("test", "group", "tenant");

        // Assert
        Assert.Null(loaded);
    }
}
