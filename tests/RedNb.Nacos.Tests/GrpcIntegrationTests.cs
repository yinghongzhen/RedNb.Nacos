using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedNb.Nacos.Auth;
using RedNb.Nacos.Common.Options;
using RedNb.Nacos.Config;
using RedNb.Nacos.Naming;
using RedNb.Nacos.Naming.Models;
using RedNb.Nacos.Remote.Grpc;

namespace RedNb.Nacos.Tests;

/// <summary>
/// gRPC 集成测试
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
[Collection("GrpcIntegration")]
public class GrpcIntegrationTests : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;
    private INacosGrpcClient? _grpcClient;
    private INacosConfigService? _configService;
    private INacosNamingService? _namingService;
    private NacosOptions? _options;

    // 测试配置 - 从 testsettings.json 读取
    private string _testServerAddress = "http://localhost:8848";
    private string _testNamespace = "public";
    private string? _testUsername;
    private string? _testPassword;
    private int _grpcPortOffset = 1000;

    private const string TestDataId = "grpc-test-config";
    private const string TestGroup = "DEFAULT_GROUP";
    private const string TestServiceName = "grpc-test-service";

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
            ["RedNb:Nacos:UseGrpc"] = "true",
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
        _grpcClient = _serviceProvider.GetRequiredService<INacosGrpcClient>();
        _configService = _serviceProvider.GetRequiredService<INacosConfigService>();
        _namingService = _serviceProvider.GetRequiredService<INacosNamingService>();
        _options = _serviceProvider.GetRequiredService<IOptions<NacosOptions>>().Value;

        // 预先建立连接，确保连接稳定
        if (_options.UseGrpc)
        {
            var connected = await _grpcClient.ConnectAsync();
            if (!connected)
            {
                throw new InvalidOperationException("无法连接到 Nacos gRPC 服务");
            }
            // 等待连接稳定
            await Task.Delay(1000);
        }
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider != null)
        {
            await _serviceProvider.DisposeAsync();
        }
    }

    #region gRPC 连接测试

    [Fact]
    public void GrpcClient_ShouldBeRegistered()
    {
        // Assert
        Assert.NotNull(_grpcClient);
        Assert.IsType<NacosGrpcClient>(_grpcClient);
    }

    [Fact]
    public void Options_ShouldHaveGrpcEnabled()
    {
        // Assert
        Assert.NotNull(_options);
        Assert.True(_options.UseGrpc);
        Assert.Equal(1000, _options.GrpcPortOffset);
    }

    [Fact]
    public async Task GrpcClient_ConnectAsync_ShouldConnectToServer()
    {
        // Act
        var connected = await _grpcClient!.ConnectAsync();

        // Assert
        Assert.True(connected, "应该能够连接到 Nacos gRPC 服务");
        Assert.True(_grpcClient.IsConnected);
    }

    [Fact]
    public async Task GrpcClient_ReconnectAsync_ShouldReconnect()
    {
        // Arrange
        await _grpcClient!.ConnectAsync();

        // Act
        var reconnected = await _grpcClient.ReconnectAsync();

        // Assert
        Assert.True(reconnected, "应该能够重新连接");
        Assert.True(_grpcClient.IsConnected);
    }

    #endregion

    #region 配置管理测试 (gRPC)

    [Fact]
    public async Task ConfigService_PublishAndGetConfig_ViaGrpc()
    {
        // Arrange
        var testContent = $"{{\"test\": \"grpc-{DateTime.Now:yyyyMMddHHmmss}\"}}";

        // Act - 发布配置
        var publishResult = await _configService!.PublishConfigAsync(
            TestDataId, TestGroup, testContent, "json");

        // Assert - 发布成功
        Assert.True(publishResult, "配置发布应该成功");

        // Act - 获取配置
        await Task.Delay(500); // 等待配置同步
        var content = await _configService.GetConfigAsync(TestDataId, TestGroup);

        // Assert - 获取成功
        Assert.NotNull(content);
        Assert.Equal(testContent, content);

        // Cleanup
        await _configService.RemoveConfigAsync(TestDataId, TestGroup);
    }

    [Fact]
    public async Task ConfigService_AddListener_ShouldReceivePush()
    {
        // Arrange
        var receivedContent = string.Empty;
        var listenerCalled = new TaskCompletionSource<bool>();

        await _configService!.AddListenerAsync(TestDataId, TestGroup, content =>
        {
            receivedContent = content;
            listenerCalled.TrySetResult(true);
        });

        // Act - 发布新配置
        var newContent = $"{{\"pushed\": \"grpc-push-{DateTime.Now:yyyyMMddHHmmss}\"}}";
        await _configService.PublishConfigAsync(TestDataId, TestGroup, newContent, "json");

        // Assert - 等待推送（最多等待 10 秒）
        var received = await Task.WhenAny(listenerCalled.Task, Task.Delay(10000)) == listenerCalled.Task;

        // Cleanup
        await _configService.RemoveConfigAsync(TestDataId, TestGroup);

        // 如果使用 gRPC，应该能收到推送
        if (_options!.UseGrpc)
        {
            Assert.True(received, "应该在 10 秒内收到配置变更推送");
            Assert.Equal(newContent, receivedContent);
        }
    }

    [Fact]
    public async Task ConfigService_RemoveConfig_ViaGrpc()
    {
        // Arrange
        var testContent = "test-content-to-delete";
        await _configService!.PublishConfigAsync(TestDataId, TestGroup, testContent);
        await Task.Delay(500);

        // Act
        var removeResult = await _configService.RemoveConfigAsync(TestDataId, TestGroup);

        // Assert
        Assert.True(removeResult, "配置删除应该成功");

        // Verify
        await Task.Delay(500);
        var content = await _configService.GetConfigAsync(TestDataId, TestGroup);
        Assert.Null(content);
    }

    #endregion

    #region 服务发现测试 (gRPC)

    [Fact]
    public async Task NamingService_RegisterAndDeregister_ViaGrpc()
    {
        // Arrange
        var instance = new Instance
        {
            Ip = "192.168.1.100",
            Port = 8080,
            Weight = 1.0,
            Healthy = true,
            Enabled = true,
            Ephemeral = true,
            ClusterName = "DEFAULT",
            Metadata = new Dictionary<string, string>
            {
                ["version"] = "1.0.0",
                ["protocol"] = "grpc-test"
            }
        };

        // Act - 注册
        await _namingService!.RegisterInstanceAsync(TestServiceName, TestGroup, instance);
        await Task.Delay(1000); // 等待注册生效

        // Assert - 获取实例
        var instances = await _namingService.GetAllInstancesAsync(TestServiceName, TestGroup);
        var registeredInstance = instances.FirstOrDefault(i => i.Ip == instance.Ip && i.Port == instance.Port);

        Assert.NotNull(registeredInstance);
        Assert.Equal(instance.Ip, registeredInstance.Ip);
        Assert.Equal(instance.Port, registeredInstance.Port);

        // Act - 注销
        await _namingService.DeregisterInstanceAsync(TestServiceName, TestGroup, instance);
        await Task.Delay(1000);

        // Assert - 验证注销
        instances = await _namingService.GetAllInstancesAsync(TestServiceName, TestGroup);
        registeredInstance = instances.FirstOrDefault(i => i.Ip == instance.Ip && i.Port == instance.Port);

        Assert.Null(registeredInstance);
    }

    [Fact]
    public async Task NamingService_BatchRegister_ViaGrpc()
    {
        // Arrange
        var instances = new List<Instance>
        {
            new() { Ip = "192.168.1.101", Port = 8081, Weight = 1.0, Ephemeral = true },
            new() { Ip = "192.168.1.102", Port = 8082, Weight = 1.0, Ephemeral = true },
            new() { Ip = "192.168.1.103", Port = 8083, Weight = 1.0, Ephemeral = true }
        };

        try
        {
            // Act - 批量注册
            await _namingService!.BatchRegisterInstanceAsync(TestServiceName, TestGroup, instances);
            await Task.Delay(1000);

            // Assert
            var registeredInstances = await _namingService.GetAllInstancesAsync(TestServiceName, TestGroup);
            Assert.True(registeredInstances.Count >= 3, "应该至少有 3 个实例");

            foreach (var instance in instances)
            {
                var found = registeredInstances.Any(i => i.Ip == instance.Ip && i.Port == instance.Port);
                Assert.True(found, $"实例 {instance.Ip}:{instance.Port} 应该存在");
            }
        }
        finally
        {
            // Cleanup - 批量注销
            await _namingService!.BatchDeregisterInstanceAsync(TestServiceName, TestGroup, instances);
        }
    }

    [Fact]
    public async Task NamingService_Subscribe_ShouldReceivePush()
    {
        // Arrange
        var pushReceived = new TaskCompletionSource<ServiceInfo>();

        await _namingService!.SubscribeAsync(TestServiceName, TestGroup, serviceInfo =>
        {
            pushReceived.TrySetResult(serviceInfo);
        });

        // Act - 注册一个新实例
        var instance = new Instance
        {
            Ip = "192.168.1.200",
            Port = 9090,
            Weight = 1.0,
            Ephemeral = true
        };

        await _namingService.RegisterInstanceAsync(TestServiceName, TestGroup, instance);

        // Assert - 等待推送
        var received = await Task.WhenAny(pushReceived.Task, Task.Delay(10000)) == pushReceived.Task;

        // Cleanup
        await _namingService.DeregisterInstanceAsync(TestServiceName, TestGroup, instance);

        if (_options!.UseGrpc)
        {
            Assert.True(received, "应该在 10 秒内收到服务变更推送");
        }
    }

    [Fact]
    public async Task NamingService_GetServices_ViaGrpc()
    {
        // Arrange - 先注册一个服务
        var instance = new Instance
        {
            Ip = "192.168.1.150",
            Port = 8888,
            Ephemeral = true
        };
        await _namingService!.RegisterInstanceAsync(TestServiceName, TestGroup, instance);
        await Task.Delay(1000);

        try
        {
            // Act
            var services = await _namingService.GetServicesAsync(TestGroup);

            // Assert
            Assert.NotNull(services);
            Assert.Contains(TestServiceName, services);
        }
        finally
        {
            // Cleanup
            await _namingService.DeregisterInstanceAsync(TestServiceName, TestGroup, instance);
        }
    }

    [Fact]
    public async Task NamingService_SelectOneHealthyInstance_ViaGrpc()
    {
        // Arrange
        var instances = new List<Instance>
        {
            new() { Ip = "192.168.1.111", Port = 8001, Weight = 1.0, Healthy = true, Ephemeral = true },
            new() { Ip = "192.168.1.112", Port = 8002, Weight = 2.0, Healthy = true, Ephemeral = true }
        };

        await _namingService!.BatchRegisterInstanceAsync(TestServiceName, TestGroup, instances);
        await Task.Delay(1000);

        try
        {
            // Act
            var selectedInstance = await _namingService.SelectOneHealthyInstanceAsync(TestServiceName, TestGroup);

            // Assert
            Assert.NotNull(selectedInstance);
            Assert.True(selectedInstance.Healthy);
            Assert.Contains(instances, i => i.Ip == selectedInstance.Ip && i.Port == selectedInstance.Port);
        }
        finally
        {
            // Cleanup
            await _namingService.BatchDeregisterInstanceAsync(TestServiceName, TestGroup, instances);
        }
    }

    #endregion

    #region 健康检查测试

    [Fact]
    public async Task ConfigService_GetServerStatus_ShouldReturnStatus()
    {
        // Act
        var status = await _configService!.GetServerStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.NotEqual("DOWN", status);
    }

    [Fact]
    public async Task NamingService_GetServerStatus_ShouldReturnStatus()
    {
        // Act
        var status = await _namingService!.GetServerStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.NotEqual("DOWN", status);
    }

    #endregion
}

/// <summary>
/// 测试集合定义，确保测试按顺序执行，避免连接冲突
/// </summary>
[CollectionDefinition("GrpcIntegration")]
public class GrpcIntegrationCollection : ICollectionFixture<GrpcIntegrationFixture>
{
}

/// <summary>
/// gRPC 集成测试固件
/// </summary>
public class GrpcIntegrationFixture
{
    // 可以在这里添加共享的测试资源
}
