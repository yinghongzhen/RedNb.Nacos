namespace RedNb.Nacos.Tests;

/// <summary>
/// 负载均衡器测试
/// </summary>
public class LoadBalancerTests
{
    private readonly List<Instance> _instances;

    public LoadBalancerTests()
    {
        _instances =
        [
            new Instance { Ip = "192.168.1.1", Port = 8080, Weight = 1.0, Healthy = true, Enabled = true },
            new Instance { Ip = "192.168.1.2", Port = 8080, Weight = 2.0, Healthy = true, Enabled = true },
            new Instance { Ip = "192.168.1.3", Port = 8080, Weight = 3.0, Healthy = true, Enabled = true },
        ];
    }

    [Fact]
    public void RandomLoadBalancer_Select_ShouldReturnValidInstance()
    {
        // Arrange
        var balancer = new RandomLoadBalancer();

        // Act
        var instance = balancer.Select(_instances);

        // Assert
        Assert.NotNull(instance);
        Assert.Contains(instance, _instances);
    }

    [Fact]
    public void RoundRobinLoadBalancer_Select_ShouldRoundRobin()
    {
        // Arrange
        var balancer = new RoundRobinLoadBalancer();

        // Act & Assert
        var first = balancer.Select(_instances);
        var second = balancer.Select(_instances);
        var third = balancer.Select(_instances);
        var fourth = balancer.Select(_instances);

        Assert.Equal(_instances[0], first);
        Assert.Equal(_instances[1], second);
        Assert.Equal(_instances[2], third);
        Assert.Equal(_instances[0], fourth); // 循环回第一个
    }

    [Fact]
    public void LoadBalancer_Select_EmptyList_ShouldReturnNull()
    {
        // Arrange
        var balancer = new RandomLoadBalancer();
        var emptyList = new List<Instance>();

        // Act
        var instance = balancer.Select(emptyList);

        // Assert
        Assert.Null(instance);
    }

    [Fact]
    public void LoadBalancerFactory_Create_ShouldReturnCorrectBalancer()
    {
        // Act & Assert
        Assert.IsType<RandomLoadBalancer>(LoadBalancerFactory.Create(LoadBalancerStrategy.Random));
        Assert.IsType<RoundRobinLoadBalancer>(LoadBalancerFactory.Create(LoadBalancerStrategy.RoundRobin));
        Assert.IsType<WeightedRandomLoadBalancer>(LoadBalancerFactory.Create(LoadBalancerStrategy.WeightedRandom));
        Assert.IsType<WeightedRoundRobinLoadBalancer>(LoadBalancerFactory.Create(LoadBalancerStrategy.WeightedRoundRobin));
    }
}
