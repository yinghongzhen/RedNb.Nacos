using RedNb.Nacos.Remote.Grpc.Models;

namespace RedNb.Nacos.Tests;

/// <summary>
/// gRPC 模型测试
/// </summary>
public class GrpcModelsTests
{
    [Fact]
    public void ConfigQueryRequest_GetRequestType_ShouldReturnCorrectType()
    {
        // Arrange
        var request = new ConfigQueryRequest
        {
            DataId = "test-data-id",
            Group = "test-group",
            Tenant = "test-tenant"
        };

        // Act
        var type = request.GetRequestType();

        // Assert
        Assert.Equal("ConfigQueryRequest", type);
    }

    [Fact]
    public void ConfigPublishRequest_GetRequestType_ShouldReturnCorrectType()
    {
        // Arrange
        var request = new ConfigPublishRequest
        {
            DataId = "test-data-id",
            Group = "test-group",
            Tenant = "test-tenant",
            Content = "test-content"
        };

        // Act
        var type = request.GetRequestType();

        // Assert
        Assert.Equal("ConfigPublishRequest", type);
    }

    [Fact]
    public void InstanceRequest_GetRequestType_ShouldReturnCorrectType()
    {
        // Arrange
        var request = new InstanceRequest
        {
            ServiceName = "test-service",
            GroupName = "test-group",
            Namespace = "test-namespace",
            Type = "registerInstance"
        };

        // Act
        var type = request.GetRequestType();

        // Assert
        Assert.Equal("InstanceRequest", type);
    }

    [Fact]
    public void BatchInstanceRequest_GetRequestType_ShouldReturnCorrectType()
    {
        // Arrange
        var request = new BatchInstanceRequest
        {
            ServiceName = "test-service",
            GroupName = "test-group",
            Namespace = "test-namespace",
            Type = "batchRegisterInstance",
            Instances = []
        };

        // Act
        var type = request.GetRequestType();

        // Assert
        Assert.Equal("BatchInstanceRequest", type);
    }

    [Fact]
    public void NacosResponse_IsSuccess_ShouldReturnTrueForSuccessCode()
    {
        // Arrange
        var response = new ServerCheckResponse { ResultCode = 200 };

        // Act & Assert
        Assert.True(response.IsSuccess);
    }

    [Fact]
    public void NacosResponse_IsSuccess_ShouldReturnFalseForErrorCode()
    {
        // Arrange
        var response = new ErrorResponse { ResultCode = 500, ErrorCode = 500, Message = "Error" };

        // Act & Assert
        Assert.False(response.IsSuccess);
    }

    [Fact]
    public void GrpcInstance_ToInstance_ShouldConvertCorrectly()
    {
        // Arrange
        var grpcInstance = new GrpcInstance
        {
            Ip = "192.168.1.1",
            Port = 8080,
            Weight = 1.5,
            Healthy = true,
            Enabled = true,
            Ephemeral = true,
            ClusterName = "test-cluster",
            ServiceName = "test-service",
            Metadata = new Dictionary<string, string> { ["key"] = "value" }
        };

        // Act
        var instance = grpcInstance.ToInstance();

        // Assert
        Assert.Equal("192.168.1.1", instance.Ip);
        Assert.Equal(8080, instance.Port);
        Assert.Equal(1.5, instance.Weight);
        Assert.True(instance.Healthy);
        Assert.True(instance.Enabled);
        Assert.True(instance.Ephemeral);
        Assert.Equal("test-cluster", instance.ClusterName);
        Assert.Equal("test-service", instance.ServiceName);
        Assert.Equal("value", instance.Metadata?["key"]);
    }

    [Fact]
    public void GrpcInstance_FromInstance_ShouldConvertCorrectly()
    {
        // Arrange
        var instance = new Instance
        {
            Ip = "192.168.1.1",
            Port = 8080,
            Weight = 1.5,
            Healthy = true,
            Enabled = true,
            Ephemeral = true,
            ClusterName = "test-cluster",
            ServiceName = "test-service",
            Metadata = new Dictionary<string, string> { ["key"] = "value" }
        };

        // Act
        var grpcInstance = GrpcInstance.FromInstance(instance);

        // Assert
        Assert.Equal("192.168.1.1", grpcInstance.Ip);
        Assert.Equal(8080, grpcInstance.Port);
        Assert.Equal(1.5, grpcInstance.Weight);
        Assert.True(grpcInstance.Healthy);
        Assert.True(grpcInstance.Enabled);
        Assert.True(grpcInstance.Ephemeral);
        Assert.Equal("test-cluster", grpcInstance.ClusterName);
        Assert.Equal("test-service", grpcInstance.ServiceName);
        Assert.Equal("value", grpcInstance.Metadata?["key"]);
    }

    [Fact]
    public void ConfigQueryRequest_Module_ShouldBeConfig()
    {
        // Arrange
        var request = new ConfigQueryRequest();

        // Act
        var module = request.Module;

        // Assert
        Assert.Equal("config", module);
    }

    [Fact]
    public void InstanceRequest_Module_ShouldBeNaming()
    {
        // Arrange
        var request = new InstanceRequest();

        // Act
        var module = request.Module;

        // Assert
        Assert.Equal("naming", module);
    }

    [Fact]
    public void ServerCheckRequest_Module_ShouldBeInternal()
    {
        // Arrange
        var request = new ServerCheckRequest();

        // Act
        var module = request.Module;

        // Assert
        Assert.Equal("internal", module);
    }
}
