using FluentAssertions;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.Core.Naming.Selector;
using Xunit;

namespace RedNb.Nacos.Tests.Naming.Selector;

public class NamingSelectorTests
{
    #region LabelSelector Tests

    [Fact]
    public void LabelSelector_WithDictionary_ShouldFilterByLabels()
    {
        // Arrange
        var labels = new Dictionary<string, string>
        {
            { "env", "production" },
            { "version", "v1" }
        };
        var selector = new LabelSelector(labels);

        var instances = new List<Instance>
        {
            new Instance
            {
                Ip = "192.168.1.1",
                Port = 8080,
                Metadata = new Dictionary<string, string>
                {
                    { "env", "production" },
                    { "version", "v1" }
                }
            },
            new Instance
            {
                Ip = "192.168.1.2",
                Port = 8080,
                Metadata = new Dictionary<string, string>
                {
                    { "env", "staging" },
                    { "version", "v1" }
                }
            },
            new Instance
            {
                Ip = "192.168.1.3",
                Port = 8080,
                Metadata = new Dictionary<string, string>
                {
                    { "env", "production" },
                    { "version", "v2" }
                }
            }
        };

        var context = new NamingContext
        {
            ServiceName = "test-service",
            GroupName = "DEFAULT_GROUP",
            Instances = instances
        };

        // Act
        var result = selector.Select(context);

        // Assert
        result.Count.Should().Be(1);
        result.Instances[0].Ip.Should().Be("192.168.1.1");
    }

    [Fact]
    public void LabelSelector_WithExpression_ShouldParseCorrectly()
    {
        // Arrange
        var selector = new LabelSelector("env=production,version=v1");

        // Assert
        selector.Type.Should().Be("label");
        selector.Expression.Should().Contain("env=production");
        selector.Expression.Should().Contain("version=v1");
    }

    [Fact]
    public void LabelSelector_WithEmptyLabels_ShouldReturnAllInstances()
    {
        // Arrange
        var selector = new LabelSelector(new Dictionary<string, string>());
        var instances = new List<Instance>
        {
            new Instance { Ip = "192.168.1.1", Port = 8080 },
            new Instance { Ip = "192.168.1.2", Port = 8080 }
        };

        var context = new NamingContext { Instances = instances };

        // Act
        var result = selector.Select(context);

        // Assert
        result.Count.Should().Be(2);
    }

    [Fact]
    public void LabelSelector_Builder_ShouldBuildCorrectly()
    {
        // Arrange & Act
        var selector = LabelSelector.Builder()
            .AddLabel("env", "production")
            .AddLabel("region", "us-east")
            .Build();

        // Assert
        selector.Expression.Should().Contain("env=production");
        selector.Expression.Should().Contain("region=us-east");
    }

    #endregion

    #region ClusterSelector Tests

    [Fact]
    public void ClusterSelector_ShouldFilterByCluster()
    {
        // Arrange
        var selector = ClusterSelector.Of("cluster-a", "cluster-b");

        var instances = new List<Instance>
        {
            new Instance { Ip = "192.168.1.1", Port = 8080, ClusterName = "cluster-a" },
            new Instance { Ip = "192.168.1.2", Port = 8080, ClusterName = "cluster-b" },
            new Instance { Ip = "192.168.1.3", Port = 8080, ClusterName = "cluster-c" }
        };

        var context = new NamingContext
        {
            ServiceName = "test-service",
            Instances = instances
        };

        // Act
        var result = selector.Select(context);

        // Assert
        result.Count.Should().Be(2);
        result.Instances.Should().NotContain(i => i.ClusterName == "cluster-c");
    }

    [Fact]
    public void ClusterSelector_WithExpression_ShouldParseCorrectly()
    {
        // Arrange
        var selector = new ClusterSelector("cluster-a,cluster-b,cluster-c");

        // Assert
        selector.Type.Should().Be("cluster");
        selector.Expression.Should().Contain("cluster-a");
        selector.Expression.Should().Contain("cluster-b");
        selector.Expression.Should().Contain("cluster-c");
    }

    [Fact]
    public void ClusterSelector_WithEmptyClusters_ShouldReturnAllInstances()
    {
        // Arrange
        var selector = new ClusterSelector(Array.Empty<string>());
        var instances = new List<Instance>
        {
            new Instance { Ip = "192.168.1.1", Port = 8080, ClusterName = "cluster-a" },
            new Instance { Ip = "192.168.1.2", Port = 8080, ClusterName = "cluster-b" }
        };

        var context = new NamingContext { Instances = instances };

        // Act
        var result = selector.Select(context);

        // Assert
        result.Count.Should().Be(2);
    }

    #endregion

    #region CompositeSelector Tests

    [Fact]
    public void CompositeSelector_ShouldCombineSelectors()
    {
        // Arrange
        var labelSelector = new LabelSelector(new Dictionary<string, string> { { "env", "production" } });
        var clusterSelector = ClusterSelector.Of("cluster-a");
        var compositeSelector = new CompositeSelector(labelSelector, clusterSelector);

        var instances = new List<Instance>
        {
            new Instance
            {
                Ip = "192.168.1.1",
                Port = 8080,
                ClusterName = "cluster-a",
                Metadata = new Dictionary<string, string> { { "env", "production" } }
            },
            new Instance
            {
                Ip = "192.168.1.2",
                Port = 8080,
                ClusterName = "cluster-a",
                Metadata = new Dictionary<string, string> { { "env", "staging" } }
            },
            new Instance
            {
                Ip = "192.168.1.3",
                Port = 8080,
                ClusterName = "cluster-b",
                Metadata = new Dictionary<string, string> { { "env", "production" } }
            }
        };

        var context = new NamingContext
        {
            ServiceName = "test-service",
            Instances = instances
        };

        // Act
        var result = compositeSelector.Select(context);

        // Assert
        result.Count.Should().Be(1);
        result.Instances[0].Ip.Should().Be("192.168.1.1");
    }

    [Fact]
    public void CompositeSelector_Builder_ShouldBuildCorrectly()
    {
        // Arrange & Act
        var selector = CompositeSelector.Builder()
            .WithLabels(new Dictionary<string, string> { { "env", "production" } })
            .WithClusters("cluster-a")
            .Build();

        // Assert
        selector.Type.Should().Be("composite");
        selector.Expression.Should().Contain("label");
        selector.Expression.Should().Contain("cluster");
    }

    [Fact]
    public void CompositeSelector_WithNoMatch_ShouldReturnEmpty()
    {
        // Arrange
        var labelSelector = new LabelSelector(new Dictionary<string, string> { { "env", "nonexistent" } });
        var compositeSelector = new CompositeSelector(labelSelector);

        var instances = new List<Instance>
        {
            new Instance
            {
                Ip = "192.168.1.1",
                Port = 8080,
                Metadata = new Dictionary<string, string> { { "env", "production" } }
            }
        };

        var context = new NamingContext { Instances = instances };

        // Act
        var result = compositeSelector.Select(context);

        // Assert
        result.Count.Should().Be(0);
        result.Success.Should().BeFalse();
    }

    #endregion

    #region NamingContext Tests

    [Fact]
    public void NamingContext_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var context = new NamingContext();

        // Assert
        context.ServiceName.Should().BeEmpty();
        context.GroupName.Should().BeEmpty();
        context.Clusters.Should().BeEmpty();
        context.Instances.Should().BeEmpty();
        context.HealthyOnly.Should().BeTrue();
    }

    #endregion

    #region NamingResult Tests

    [Fact]
    public void NamingResult_Of_ShouldCreateWithInstances()
    {
        // Arrange
        var instances = new List<Instance>
        {
            new Instance { Ip = "192.168.1.1", Port = 8080 }
        };

        // Act
        var result = NamingResult.Of(instances);

        // Assert
        result.Count.Should().Be(1);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void NamingResult_Empty_ShouldCreateEmptyResult()
    {
        // Act
        var result = NamingResult.Empty();

        // Assert
        result.Count.Should().Be(0);
        result.Success.Should().BeFalse();
    }

    #endregion
}
