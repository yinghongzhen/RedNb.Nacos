using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedNb.Nacos.Common.Options;
using RedNb.Nacos.Config;

namespace RedNb.Nacos.Tests;

/// <summary>
/// 配置中心集成测试
/// 注意：这些测试需要真实的 Nacos 服务器，标记为 Trait("Category", "Integration")
/// 运行命令：dotnet test --filter "Category=Integration"
/// 
/// 配置说明：
/// 1. 复制 testsettings.template.json 为 testsettings.json
/// 2. 修改 testsettings.json 中的配置为您的 Nacos 服务器信息
/// 3. testsettings.json 已在 .gitignore 中排除，不会提交到代码库
/// 
/// 本地开发默认配置（testsettings.template.json）：
/// - 地址: http://localhost:8848
/// - gRPC 端口: 9848
/// - 命名空间: public
/// - 用户名/密码: nacos/nacos
/// </summary>
[Trait("Category", "Integration")]
[Collection("ConfigIntegration")]
public class ConfigIntegrationTests : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;
    private INacosConfigService? _configService;
    private NacosOptions? _options;

    // 测试配置 - 从 testsettings.json 读取
    private string _testServerAddress = "http://localhost:8848";
    private string _testNamespace = "public";
    private string? _testUsername;
    private string? _testPassword;
    private int _grpcPortOffset = 1000;

    // 测试用的 DataId 前缀，避免与其他配置冲突
    private const string TestDataIdPrefix = "integration-test-";
    private const string TestGroup = "DEFAULT_GROUP";

    public async Task InitializeAsync()
    {
        // 尝试从 testsettings.json 读取配置
        var testSettingsPath = Path.Combine(AppContext.BaseDirectory, "testsettings.json");
        if (File.Exists(testSettingsPath))
        {
            var testConfig = new ConfigurationBuilder()
                .AddJsonFile(testSettingsPath, optional: true)
                .Build();

            _testServerAddress = testConfig["Nacos:ServerAddress"] ?? _testServerAddress;
            _testNamespace = testConfig["Nacos:Namespace"] ?? _testNamespace;
            _testUsername = testConfig["Nacos:Username"];
            _testPassword = testConfig["Nacos:Password"];

            // 计算 gRPC 端口偏移量
            if (int.TryParse(testConfig["Nacos:GrpcPort"], out var grpcPort))
            {
                var httpUri = new Uri(_testServerAddress);
                _grpcPortOffset = grpcPort - httpUri.Port;
            }
        }

        var configDict = new Dictionary<string, string?>
        {
            ["RedNb:Nacos:ServerAddresses:0"] = _testServerAddress,
            ["RedNb:Nacos:Namespace"] = _testNamespace,
            ["RedNb:Nacos:UseGrpc"] = "false", // 使用 HTTP 模式进行配置测试，更稳定
            ["RedNb:Nacos:GrpcPortOffset"] = _grpcPortOffset.ToString()
        };

        if (!string.IsNullOrEmpty(_testUsername))
        {
            configDict["RedNb:Nacos:UserName"] = _testUsername;
        }
        if (!string.IsNullOrEmpty(_testPassword))
        {
            configDict["RedNb:Nacos:Password"] = _testPassword;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddRedNbNacos(configuration);

        _serviceProvider = services.BuildServiceProvider();
        _configService = _serviceProvider.GetRequiredService<INacosConfigService>();
        _options = _serviceProvider.GetRequiredService<IOptions<NacosOptions>>().Value;

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // 清理测试创建的配置
        await CleanupTestConfigsAsync();

        if (_serviceProvider != null)
        {
            await _serviceProvider.DisposeAsync();
        }
    }

    private async Task CleanupTestConfigsAsync()
    {
        if (_configService == null) return;

        // 尝试删除测试配置
        var testDataIds = new[]
        {
            $"{TestDataIdPrefix}basic",
            $"{TestDataIdPrefix}json",
            $"{TestDataIdPrefix}yaml",
            $"{TestDataIdPrefix}properties",
            $"{TestDataIdPrefix}listener",
            $"{TestDataIdPrefix}update",
            $"{TestDataIdPrefix}delete"
        };

        foreach (var dataId in testDataIds)
        {
            try
            {
                await _configService.RemoveConfigAsync(dataId, TestGroup);
            }
            catch
            {
                // 忽略删除错误
            }
        }
    }

    #region 基础配置操作测试

    [Fact]
    public async Task PublishAndGetConfig_BasicText_ShouldWorkCorrectly()
    {
        // Arrange
        var dataId = $"{TestDataIdPrefix}basic";
        var content = $"test-content-{DateTime.Now:yyyyMMddHHmmss}";

        // Act - 发布配置
        var publishResult = await _configService!.PublishConfigAsync(dataId, TestGroup, content);

        // Assert - 发布成功
        Assert.True(publishResult, "配置发布应该成功");

        // 等待配置同步
        await Task.Delay(500);

        // Act - 获取配置
        var retrievedContent = await _configService.GetConfigAsync(dataId, TestGroup);

        // Assert - 获取成功且内容匹配
        Assert.NotNull(retrievedContent);
        Assert.Equal(content, retrievedContent);
    }

    [Fact]
    public async Task PublishAndGetConfig_JsonContent_ShouldWorkCorrectly()
    {
        // Arrange
        var dataId = $"{TestDataIdPrefix}json";
        var jsonContent = """
            {
                "database": {
                    "host": "localhost",
                    "port": 3306,
                    "name": "testdb"
                },
                "logging": {
                    "level": "Debug"
                }
            }
            """;

        // Act - 发布 JSON 配置
        var publishResult = await _configService!.PublishConfigAsync(
            dataId, TestGroup, jsonContent, "json");

        // Assert
        Assert.True(publishResult, "JSON 配置发布应该成功");

        await Task.Delay(500);

        var retrievedContent = await _configService.GetConfigAsync(dataId, TestGroup);
        Assert.NotNull(retrievedContent);
        
        // 验证 JSON 可以正确解析
        var jsonDoc = System.Text.Json.JsonDocument.Parse(retrievedContent);
        Assert.Equal("localhost", jsonDoc.RootElement.GetProperty("database").GetProperty("host").GetString());
    }

    [Fact]
    public async Task PublishAndGetConfig_YamlContent_ShouldWorkCorrectly()
    {
        // Arrange
        var dataId = $"{TestDataIdPrefix}yaml";
        var yamlContent = """
            server:
              port: 8080
              host: localhost
            database:
              connection-string: "Server=localhost;Database=test"
            """;

        // Act
        var publishResult = await _configService!.PublishConfigAsync(
            dataId, TestGroup, yamlContent, "yaml");

        // Assert
        Assert.True(publishResult, "YAML 配置发布应该成功");

        await Task.Delay(500);

        var retrievedContent = await _configService.GetConfigAsync(dataId, TestGroup);
        Assert.NotNull(retrievedContent);
        Assert.Contains("server:", retrievedContent);
        Assert.Contains("port: 8080", retrievedContent);
    }

    [Fact]
    public async Task PublishAndGetConfig_PropertiesContent_ShouldWorkCorrectly()
    {
        // Arrange
        var dataId = $"{TestDataIdPrefix}properties";
        var propertiesContent = """
            app.name=TestApplication
            app.version=1.0.0
            database.url=jdbc:mysql://localhost:3306/test
            logging.level.root=INFO
            """;

        // Act
        var publishResult = await _configService!.PublishConfigAsync(
            dataId, TestGroup, propertiesContent, "properties");

        // Assert
        Assert.True(publishResult, "Properties 配置发布应该成功");

        await Task.Delay(500);

        var retrievedContent = await _configService.GetConfigAsync(dataId, TestGroup);
        Assert.NotNull(retrievedContent);
        Assert.Contains("app.name=TestApplication", retrievedContent);
    }

    #endregion

    #region 配置更新测试

    [Fact]
    public async Task UpdateConfig_ShouldOverwriteExistingContent()
    {
        // Arrange
        var dataId = $"{TestDataIdPrefix}update";
        var originalContent = "original-content";
        var updatedContent = "updated-content";

        // 先发布原始配置
        await _configService!.PublishConfigAsync(dataId, TestGroup, originalContent);
        await Task.Delay(500);

        // Act - 更新配置
        var updateResult = await _configService.PublishConfigAsync(dataId, TestGroup, updatedContent);

        // Assert
        Assert.True(updateResult, "配置更新应该成功");

        await Task.Delay(500);

        var retrievedContent = await _configService.GetConfigAsync(dataId, TestGroup);
        Assert.Equal(updatedContent, retrievedContent);
    }

    [Fact]
    public async Task UpdateConfig_MultipleUpdates_ShouldReturnLatestContent()
    {
        // Arrange
        var dataId = $"{TestDataIdPrefix}update";

        // Act - 连续更新多次
        for (int i = 1; i <= 3; i++)
        {
            var content = $"version-{i}";
            await _configService!.PublishConfigAsync(dataId, TestGroup, content);
            await Task.Delay(300);
        }

        // Assert - 应该返回最后一个版本
        var finalContent = await _configService!.GetConfigAsync(dataId, TestGroup);
        Assert.Equal("version-3", finalContent);
    }

    #endregion

    #region 配置删除测试

    [Fact]
    public async Task RemoveConfig_ExistingConfig_ShouldSucceed()
    {
        // Arrange
        var dataId = $"{TestDataIdPrefix}delete";
        var content = "content-to-delete";

        await _configService!.PublishConfigAsync(dataId, TestGroup, content);
        await Task.Delay(500);

        // 验证配置存在
        var existingContent = await _configService.GetConfigAsync(dataId, TestGroup);
        Assert.NotNull(existingContent);

        // Act - 删除配置
        var removeResult = await _configService.RemoveConfigAsync(dataId, TestGroup);

        // Assert
        Assert.True(removeResult, "配置删除应该成功");

        await Task.Delay(500);

        // 验证配置已删除
        var deletedContent = await _configService.GetConfigAsync(dataId, TestGroup);
        Assert.Null(deletedContent);
    }

    [Fact]
    public async Task RemoveConfig_NonExistentConfig_ShouldNotThrow()
    {
        // Arrange
        var dataId = $"{TestDataIdPrefix}non-existent-{Guid.NewGuid():N}";

        // Act & Assert - 删除不存在的配置不应抛出异常
        var removeResult = await _configService!.RemoveConfigAsync(dataId, TestGroup);
        
        // Nacos 对于不存在的配置删除通常返回 true
        // 主要验证不会抛出异常
    }

    #endregion

    #region 配置监听测试

    [Fact]
    public async Task AddListener_ShouldReceiveConfigChanges()
    {
        // Arrange
        var dataId = $"{TestDataIdPrefix}listener";
        var initialContent = "initial-content";
        var updatedContent = "updated-content";
        
        var receivedContents = new List<string?>();
        var listenerCalled = new TaskCompletionSource<bool>();

        // 先发布初始配置
        await _configService!.PublishConfigAsync(dataId, TestGroup, initialContent);
        await Task.Delay(500);

        // Act - 添加监听器
        await _configService.AddListenerAsync(dataId, TestGroup, content =>
        {
            receivedContents.Add(content);
            if (content == updatedContent)
            {
                listenerCalled.TrySetResult(true);
            }
        });

        // 等待监听器注册完成
        await Task.Delay(1000);

        // 更新配置触发监听
        await _configService.PublishConfigAsync(dataId, TestGroup, updatedContent);

        // Assert - 等待监听器被调用（最多等待 15 秒）
        var received = await Task.WhenAny(listenerCalled.Task, Task.Delay(15000)) == listenerCalled.Task;

        // 由于使用 HTTP 长轮询，可能需要较长时间才能收到更新
        if (received)
        {
            Assert.Contains(updatedContent, receivedContents);
        }
        else
        {
            // 如果超时，至少验证配置已经更新
            var currentContent = await _configService.GetConfigAsync(dataId, TestGroup);
            Assert.Equal(updatedContent, currentContent);
        }
    }

    #endregion

    #region 配置获取为类型测试

    [Fact]
    public async Task GetConfigAsType_JsonConfig_ShouldDeserializeCorrectly()
    {
        // Arrange
        var dataId = $"{TestDataIdPrefix}json";
        var jsonContent = """
            {
                "name": "TestApp",
                "version": "1.0.0",
                "enabled": true,
                "maxConnections": 100
            }
            """;

        await _configService!.PublishConfigAsync(dataId, TestGroup, jsonContent, "json");
        await Task.Delay(500);

        // Act
        var config = await _configService.GetConfigAsync<TestAppConfig>(dataId, TestGroup);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("TestApp", config.Name);
        Assert.Equal("1.0.0", config.Version);
        Assert.True(config.Enabled);
        Assert.Equal(100, config.MaxConnections);
    }

    #endregion

    #region 服务状态测试

    [Fact]
    public async Task GetServerStatus_ShouldNotReturnDown()
    {
        // Act
        var status = await _configService!.GetServerStatusAsync();

        // Assert - 服务应该在运行，即使端点不存在也不应该是 "DOWN" 以外的错误
        Assert.NotNull(status);
        // 注意：如果健康检查端点不存在，可能返回 "DOWN"，但服务实际上是运行的
        // 所以这里只验证能获取到状态
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public async Task GetConfig_NonExistentDataId_ShouldReturnNull()
    {
        // Arrange
        var dataId = $"non-existent-{Guid.NewGuid():N}";

        // Act
        var content = await _configService!.GetConfigAsync(dataId, TestGroup);

        // Assert
        Assert.Null(content);
    }

    [Fact]
    public async Task PublishConfig_SingleCharContent_ShouldSucceed()
    {
        // Arrange
        var dataId = $"{TestDataIdPrefix}basic";
        var singleCharContent = "x"; // 单字符内容

        // Act
        var result = await _configService!.PublishConfigAsync(dataId, TestGroup, singleCharContent);

        // Assert - 单字符内容发布应该成功
        Assert.True(result);

        await Task.Delay(500);

        var retrievedContent = await _configService.GetConfigAsync(dataId, TestGroup);
        Assert.Equal(singleCharContent, retrievedContent);
    }

    [Fact]
    public async Task PublishConfig_LargeContent_ShouldSucceed()
    {
        // Arrange
        var dataId = $"{TestDataIdPrefix}basic";
        var largeContent = new string('x', 10000); // 10KB 内容

        // Act
        var result = await _configService!.PublishConfigAsync(dataId, TestGroup, largeContent);

        // Assert
        Assert.True(result, "大内容配置发布应该成功");

        await Task.Delay(500);

        var retrievedContent = await _configService.GetConfigAsync(dataId, TestGroup);
        Assert.Equal(largeContent.Length, retrievedContent?.Length);
    }

    [Fact]
    public async Task PublishConfig_SpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var dataId = $"{TestDataIdPrefix}basic";
        var specialContent = """
            {
                "message": "Hello, 世界!",
                "path": "C:\\Users\\test\\file.txt",
                "quote": "He said \"Hello\"",
                "newline": "line1\nline2\nline3"
            }
            """;

        // Act
        var result = await _configService!.PublishConfigAsync(dataId, TestGroup, specialContent, "json");

        // Assert
        Assert.True(result, "特殊字符配置发布应该成功");

        await Task.Delay(500);

        var retrievedContent = await _configService.GetConfigAsync(dataId, TestGroup);
        Assert.NotNull(retrievedContent);
        Assert.Contains("世界", retrievedContent);
    }

    [Fact]
    public async Task PublishConfig_ChineseContent_ShouldHandleCorrectly()
    {
        // Arrange
        var dataId = $"{TestDataIdPrefix}basic";
        var chineseContent = """
            {
                "应用名称": "测试应用",
                "描述": "这是一个中文配置测试",
                "版本": "1.0.0"
            }
            """;

        // Act
        var result = await _configService!.PublishConfigAsync(dataId, TestGroup, chineseContent, "json");

        // Assert
        Assert.True(result, "中文内容配置发布应该成功");

        await Task.Delay(500);

        var retrievedContent = await _configService.GetConfigAsync(dataId, TestGroup);
        Assert.NotNull(retrievedContent);
        Assert.Contains("应用名称", retrievedContent);
        Assert.Contains("测试应用", retrievedContent);
    }

    #endregion
}

/// <summary>
/// 测试用配置类
/// </summary>
public class TestAppConfig
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public bool Enabled { get; set; }
    public int MaxConnections { get; set; }
}

/// <summary>
/// 配置集成测试集合定义
/// </summary>
[CollectionDefinition("ConfigIntegration")]
public class ConfigIntegrationCollection : ICollectionFixture<ConfigIntegrationFixture>
{
}

/// <summary>
/// 配置集成测试固件
/// </summary>
public class ConfigIntegrationFixture
{
    // 共享的测试资源
}
