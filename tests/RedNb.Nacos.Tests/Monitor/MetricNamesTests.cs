using FluentAssertions;
using RedNb.Nacos.Monitor;
using Xunit;

namespace RedNb.Nacos.Tests.Monitor;

/// <summary>
/// Unit tests for MetricNames constants.
/// </summary>
public class MetricNamesTests
{
    [Fact]
    public void Namespace_ShouldBeNacosClient()
    {
        MetricNames.Namespace.Should().Be("nacos_client");
    }

    #region Gauge Metric Names

    [Fact]
    public void ServiceInfoMapSize_ShouldHaveCorrectName()
    {
        MetricNames.ServiceInfoMapSize.Should().Be("nacos_client_service_info_map_size");
    }

    [Fact]
    public void ListenConfigCount_ShouldHaveCorrectName()
    {
        MetricNames.ListenConfigCount.Should().Be("nacos_client_listen_config_count");
    }

    [Fact]
    public void ConnectionStatus_ShouldHaveCorrectName()
    {
        MetricNames.ConnectionStatus.Should().Be("nacos_client_connection_status");
    }

    [Fact]
    public void FailoverEnabled_ShouldHaveCorrectName()
    {
        MetricNames.FailoverEnabled.Should().Be("nacos_client_failover_enabled");
    }

    #endregion

    #region Counter Metric Names

    [Fact]
    public void ConfigRequestSuccessTotal_ShouldHaveCorrectName()
    {
        MetricNames.ConfigRequestSuccessTotal.Should().Be("nacos_client_config_request_success_total");
    }

    [Fact]
    public void ConfigRequestFailedTotal_ShouldHaveCorrectName()
    {
        MetricNames.ConfigRequestFailedTotal.Should().Be("nacos_client_config_request_failed_total");
    }

    [Fact]
    public void NamingRequestSuccessTotal_ShouldHaveCorrectName()
    {
        MetricNames.NamingRequestSuccessTotal.Should().Be("nacos_client_naming_request_success_total");
    }

    [Fact]
    public void NamingRequestFailedTotal_ShouldHaveCorrectName()
    {
        MetricNames.NamingRequestFailedTotal.Should().Be("nacos_client_naming_request_failed_total");
    }

    [Fact]
    public void ServiceChangePushTotal_ShouldHaveCorrectName()
    {
        MetricNames.ServiceChangePushTotal.Should().Be("nacos_client_service_change_push_total");
    }

    [Fact]
    public void ConfigChangePushTotal_ShouldHaveCorrectName()
    {
        MetricNames.ConfigChangePushTotal.Should().Be("nacos_client_config_change_push_total");
    }

    [Fact]
    public void RedoOperationTotal_ShouldHaveCorrectName()
    {
        MetricNames.RedoOperationTotal.Should().Be("nacos_client_redo_operation_total");
    }

    [Fact]
    public void FailoverUsedTotal_ShouldHaveCorrectName()
    {
        MetricNames.FailoverUsedTotal.Should().Be("nacos_client_failover_used_total");
    }

    #endregion

    #region Histogram Metric Names

    [Fact]
    public void RequestLatency_ShouldHaveCorrectName()
    {
        MetricNames.RequestLatency.Should().Be("nacos_client_request_latency");
    }

    [Fact]
    public void ConfigRequestLatency_ShouldHaveCorrectName()
    {
        MetricNames.ConfigRequestLatency.Should().Be("nacos_client_config_request_latency");
    }

    [Fact]
    public void NamingRequestLatency_ShouldHaveCorrectName()
    {
        MetricNames.NamingRequestLatency.Should().Be("nacos_client_naming_request_latency");
    }

    #endregion

    #region Naming Convention Tests

    [Fact]
    public void AllMetricNames_ShouldStartWithNamespace()
    {
        // Gauge metrics
        MetricNames.ServiceInfoMapSize.Should().StartWith(MetricNames.Namespace);
        MetricNames.ListenConfigCount.Should().StartWith(MetricNames.Namespace);
        MetricNames.ConnectionStatus.Should().StartWith(MetricNames.Namespace);
        MetricNames.FailoverEnabled.Should().StartWith(MetricNames.Namespace);

        // Counter metrics
        MetricNames.ConfigRequestSuccessTotal.Should().StartWith(MetricNames.Namespace);
        MetricNames.ConfigRequestFailedTotal.Should().StartWith(MetricNames.Namespace);
        MetricNames.NamingRequestSuccessTotal.Should().StartWith(MetricNames.Namespace);
        MetricNames.NamingRequestFailedTotal.Should().StartWith(MetricNames.Namespace);
        MetricNames.ServiceChangePushTotal.Should().StartWith(MetricNames.Namespace);
        MetricNames.ConfigChangePushTotal.Should().StartWith(MetricNames.Namespace);
        MetricNames.RedoOperationTotal.Should().StartWith(MetricNames.Namespace);
        MetricNames.FailoverUsedTotal.Should().StartWith(MetricNames.Namespace);

        // Histogram metrics
        MetricNames.RequestLatency.Should().StartWith(MetricNames.Namespace);
        MetricNames.ConfigRequestLatency.Should().StartWith(MetricNames.Namespace);
        MetricNames.NamingRequestLatency.Should().StartWith(MetricNames.Namespace);
    }

    [Fact]
    public void CounterMetrics_ShouldEndWithTotal()
    {
        MetricNames.ConfigRequestSuccessTotal.Should().EndWith("_total");
        MetricNames.ConfigRequestFailedTotal.Should().EndWith("_total");
        MetricNames.NamingRequestSuccessTotal.Should().EndWith("_total");
        MetricNames.NamingRequestFailedTotal.Should().EndWith("_total");
        MetricNames.ServiceChangePushTotal.Should().EndWith("_total");
        MetricNames.ConfigChangePushTotal.Should().EndWith("_total");
        MetricNames.RedoOperationTotal.Should().EndWith("_total");
        MetricNames.FailoverUsedTotal.Should().EndWith("_total");
    }

    [Fact]
    public void LatencyMetrics_ShouldEndWithLatency()
    {
        MetricNames.RequestLatency.Should().EndWith("_latency");
        MetricNames.ConfigRequestLatency.Should().EndWith("_latency");
        MetricNames.NamingRequestLatency.Should().EndWith("_latency");
    }

    #endregion
}
