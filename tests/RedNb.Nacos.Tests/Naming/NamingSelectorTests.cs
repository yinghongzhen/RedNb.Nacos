using FluentAssertions;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Naming;
using RedNb.Nacos.Core.Naming.Selector;
using Xunit;

namespace RedNb.Nacos.Tests.Naming;

public class NamingSelectorTests
{
    [Fact]
    public void NamingContext_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var context = new NamingContext();

        // Assert
        context.ServiceName.Should().Be(string.Empty);
        context.GroupName.Should().Be(string.Empty);
        context.Clusters.Should().NotBeNull();
        context.Clusters.Should().BeEmpty();
        context.ServiceInfo.Should().BeNull();
        context.Instances.Should().NotBeNull();
        context.Instances.Should().BeEmpty();
        context.HealthyOnly.Should().BeTrue();
    }

    [Fact]
    public void NamingResult_Of_ShouldCreateResultWithInstances()
    {
        // Arrange
        var instances = new List<Instance>
        {
            new() { Ip = "1.1.1.1", Port = 8080 },
            new() { Ip = "2.2.2.2", Port = 8080 }
        };

        // Act
        var result = NamingResult.Of(instances);

        // Assert
        result.Instances.Should().HaveCount(2);
        result.Count.Should().Be(2);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void NamingResult_Empty_ShouldCreateEmptyResult()
    {
        // Act
        var result = NamingResult.Empty();

        // Assert
        result.Instances.Should().BeEmpty();
        result.Count.Should().Be(0);
        result.Success.Should().BeFalse();
    }
}

public class ClusterSelectorTests
{
    [Fact]
    public void ClusterSelector_FromEnumerable_ShouldSetClustersAndExpression()
    {
        // Arrange
        var clusters = new[] { "cluster1", "cluster2" };

        // Act
        var selector = new ClusterSelector(clusters);

        // Assert
        selector.Type.Should().Be("cluster");
        selector.Expression.Should().Contain("cluster1");
        selector.Expression.Should().Contain("cluster2");
    }

    [Fact]
    public void ClusterSelector_FromExpression_ShouldParseClusters()
    {
        // Act
        var selector = new ClusterSelector("cluster1,cluster2,cluster3");

        // Assert
        selector.Expression.Should().Be("cluster1,cluster2,cluster3");
    }

    [Fact]
    public void ClusterSelector_Of_ShouldCreateSelector()
    {
        // Act
        var selector = ClusterSelector.Of("c1", "c2");

        // Assert
        selector.Should().NotBeNull();
        selector.Expression.Should().Contain("c1");
    }

    [Fact]
    public void ClusterSelector_Select_EmptyClusters_ShouldReturnAllInstances()
    {
        // Arrange
        var selector = new ClusterSelector(Array.Empty<string>());
        var instances = new List<Instance>
        {
            new() { Ip = "1.1.1.1", Port = 8080, ClusterName = "c1" },
            new() { Ip = "2.2.2.2", Port = 8080, ClusterName = "c2" }
        };
        var context = new NamingContext { Instances = instances };

        // Act
        var result = selector.Select(context);

        // Assert
        result.Instances.Should().HaveCount(2);
    }

    [Fact]
    public void ClusterSelector_Select_MatchingClusters_ShouldFilterInstances()
    {
        // Arrange
        var selector = new ClusterSelector("cluster1");
        var instances = new List<Instance>
        {
            new() { Ip = "1.1.1.1", Port = 8080, ClusterName = "cluster1" },
            new() { Ip = "2.2.2.2", Port = 8080, ClusterName = "cluster2" },
            new() { Ip = "3.3.3.3", Port = 8080, ClusterName = "cluster1" }
        };
        var context = new NamingContext { Instances = instances };

        // Act
        var result = selector.Select(context);

        // Assert
        result.Instances.Should().HaveCount(2);
        result.Instances.Should().OnlyContain(i => i.ClusterName == "cluster1");
    }

    [Fact]
    public void ClusterSelector_Select_CaseInsensitive_ShouldMatchAnyCase()
    {
        // Arrange
        var selector = new ClusterSelector("Cluster1");
        var instances = new List<Instance>
        {
            new() { Ip = "1.1.1.1", Port = 8080, ClusterName = "cluster1" },
            new() { Ip = "2.2.2.2", Port = 8080, ClusterName = "CLUSTER1" }
        };
        var context = new NamingContext { Instances = instances };

        // Act
        var result = selector.Select(context);

        // Assert
        result.Instances.Should().HaveCount(2);
    }

    [Fact]
    public void ClusterSelector_Select_NoMatchingClusters_ShouldReturnEmpty()
    {
        // Arrange
        var selector = new ClusterSelector("cluster3");
        var instances = new List<Instance>
        {
            new() { Ip = "1.1.1.1", Port = 8080, ClusterName = "cluster1" },
            new() { Ip = "2.2.2.2", Port = 8080, ClusterName = "cluster2" }
        };
        var context = new NamingContext { Instances = instances };

        // Act
        var result = selector.Select(context);

        // Assert
        result.Instances.Should().BeEmpty();
        result.Success.Should().BeFalse();
    }

    [Fact]
    public void ClusterSelector_Select_NullClusterName_ShouldMatchDefault()
    {
        // Arrange
        var selector = new ClusterSelector(NacosConstants.DefaultClusterName);
        var instances = new List<Instance>
        {
            new() { Ip = "1.1.1.1", Port = 8080, ClusterName = null },
            new() { Ip = "2.2.2.2", Port = 8080, ClusterName = "other" }
        };
        var context = new NamingContext { Instances = instances };

        // Act
        var result = selector.Select(context);

        // Assert
        result.Instances.Should().HaveCount(1);
        result.Instances[0].Ip.Should().Be("1.1.1.1");
    }

    [Fact]
    public void ClusterSelector_MultipleClusters_ShouldMatchAny()
    {
        // Arrange
        var selector = new ClusterSelector("cluster1,cluster3");
        var instances = new List<Instance>
        {
            new() { Ip = "1.1.1.1", Port = 8080, ClusterName = "cluster1" },
            new() { Ip = "2.2.2.2", Port = 8080, ClusterName = "cluster2" },
            new() { Ip = "3.3.3.3", Port = 8080, ClusterName = "cluster3" }
        };
        var context = new NamingContext { Instances = instances };

        // Act
        var result = selector.Select(context);

        // Assert
        result.Instances.Should().HaveCount(2);
        result.Instances.Select(i => i.Ip).Should().Contain("1.1.1.1");
        result.Instances.Select(i => i.Ip).Should().Contain("3.3.3.3");
    }
}
