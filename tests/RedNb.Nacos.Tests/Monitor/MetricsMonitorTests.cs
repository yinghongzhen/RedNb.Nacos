using FluentAssertions;
using RedNb.Nacos.Monitor;
using Xunit;

namespace RedNb.Nacos.Tests.Monitor;

/// <summary>
/// Unit tests for MetricsMonitor class.
/// </summary>
public class MetricsMonitorTests
{
    private readonly MetricsMonitor _monitor;

    public MetricsMonitorTests()
    {
        // Use Default instance for testing
        _monitor = MetricsMonitor.Default;
        _monitor.Reset(); // Reset before each test
        _monitor.Enabled = true;
    }

    #region Enabled Property Tests

    [Fact]
    public void Enabled_Default_ShouldBeTrue()
    {
        // Assert
        _monitor.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Enabled_WhenDisabled_ShouldNotRecordMetrics()
    {
        // Arrange
        _monitor.Reset();
        _monitor.Enabled = false;

        // Act
        _monitor.RecordConfigRequestSuccess();
        _monitor.RecordConfigRequestSuccess();
        _monitor.RecordConfigRequestSuccess();

        // Assert
        _monitor.GetCounterValue(MetricNames.ConfigRequestSuccessTotal).Should().Be(0);

        // Cleanup
        _monitor.Enabled = true;
    }

    [Fact]
    public void Enabled_WhenEnabled_ShouldRecordMetrics()
    {
        // Arrange
        _monitor.Reset();
        _monitor.Enabled = true;

        // Act
        _monitor.RecordConfigRequestSuccess();

        // Assert
        _monitor.GetCounterValue(MetricNames.ConfigRequestSuccessTotal).Should().Be(1);
    }

    #endregion

    #region Counter Tests

    [Fact]
    public void RecordConfigRequestSuccess_ShouldIncrementCounter()
    {
        // Arrange
        _monitor.Reset();

        // Act
        _monitor.RecordConfigRequestSuccess();
        _monitor.RecordConfigRequestSuccess();

        // Assert
        _monitor.GetCounterValue(MetricNames.ConfigRequestSuccessTotal).Should().Be(2);
    }

    [Fact]
    public void RecordConfigRequestFailed_ShouldIncrementCounter()
    {
        // Arrange
        _monitor.Reset();

        // Act
        _monitor.RecordConfigRequestFailed();

        // Assert
        _monitor.GetCounterValue(MetricNames.ConfigRequestFailedTotal).Should().Be(1);
    }

    [Fact]
    public void RecordNamingRequestSuccess_ShouldIncrementCounter()
    {
        // Arrange
        _monitor.Reset();

        // Act
        _monitor.RecordNamingRequestSuccess();
        _monitor.RecordNamingRequestSuccess();
        _monitor.RecordNamingRequestSuccess();

        // Assert
        _monitor.GetCounterValue(MetricNames.NamingRequestSuccessTotal).Should().Be(3);
    }

    [Fact]
    public void RecordNamingRequestFailed_ShouldIncrementCounter()
    {
        // Arrange
        _monitor.Reset();

        // Act
        _monitor.RecordNamingRequestFailed();

        // Assert
        _monitor.GetCounterValue(MetricNames.NamingRequestFailedTotal).Should().Be(1);
    }

    [Fact]
    public void RecordServiceChangePush_ShouldIncrementCounter()
    {
        // Arrange
        _monitor.Reset();

        // Act
        _monitor.RecordServiceChangePush();

        // Assert
        _monitor.GetCounterValue(MetricNames.ServiceChangePushTotal).Should().Be(1);
    }

    [Fact]
    public void RecordConfigChangePush_ShouldIncrementCounter()
    {
        // Arrange
        _monitor.Reset();

        // Act
        _monitor.RecordConfigChangePush();

        // Assert
        _monitor.GetCounterValue(MetricNames.ConfigChangePushTotal).Should().Be(1);
    }

    [Fact]
    public void RecordRedoOperation_ShouldIncrementCounter()
    {
        // Arrange
        _monitor.Reset();

        // Act
        _monitor.RecordRedoOperation();
        _monitor.RecordRedoOperation();

        // Assert
        _monitor.GetCounterValue(MetricNames.RedoOperationTotal).Should().Be(2);
    }

    [Fact]
    public void RecordFailoverUsed_ShouldIncrementCounter()
    {
        // Arrange
        _monitor.Reset();

        // Act
        _monitor.RecordFailoverUsed();

        // Assert
        _monitor.GetCounterValue(MetricNames.FailoverUsedTotal).Should().Be(1);
    }

    [Fact]
    public void IncreaseCounter_CustomValue_ShouldAddValue()
    {
        // Arrange
        _monitor.Reset();

        // Act
        _monitor.IncreaseCounter(MetricNames.ConfigRequestSuccessTotal, 5);

        // Assert
        _monitor.GetCounterValue(MetricNames.ConfigRequestSuccessTotal).Should().Be(5);
    }

    #endregion

    #region Gauge Tests

    [Fact]
    public void SetConnectionStatus_Connected_ShouldSetToOne()
    {
        // Act
        _monitor.SetConnectionStatus(true);

        // Assert
        _monitor.GetGaugeValue(MetricNames.ConnectionStatus).Should().Be(1);
    }

    [Fact]
    public void SetConnectionStatus_Disconnected_ShouldSetToZero()
    {
        // Act
        _monitor.SetConnectionStatus(false);

        // Assert
        _monitor.GetGaugeValue(MetricNames.ConnectionStatus).Should().Be(0);
    }

    [Fact]
    public void SetFailoverEnabled_Enabled_ShouldSetToOne()
    {
        // Act
        _monitor.SetFailoverEnabled(true);

        // Assert
        _monitor.GetGaugeValue(MetricNames.FailoverEnabled).Should().Be(1);
    }

    [Fact]
    public void SetFailoverEnabled_Disabled_ShouldSetToZero()
    {
        // Act
        _monitor.SetFailoverEnabled(false);

        // Assert
        _monitor.GetGaugeValue(MetricNames.FailoverEnabled).Should().Be(0);
    }

    [Fact]
    public void SetServiceInfoMapSize_ShouldSetValue()
    {
        // Act
        _monitor.SetServiceInfoMapSize(42);

        // Assert
        _monitor.GetGaugeValue(MetricNames.ServiceInfoMapSize).Should().Be(42);
    }

    [Fact]
    public void SetListenConfigCount_ShouldSetValue()
    {
        // Act
        _monitor.SetListenConfigCount(10);

        // Assert
        _monitor.GetGaugeValue(MetricNames.ListenConfigCount).Should().Be(10);
    }

    [Fact]
    public void SetGauge_ShouldOverwritePreviousValue()
    {
        // Arrange
        _monitor.SetGauge(MetricNames.ServiceInfoMapSize, 10);

        // Act
        _monitor.SetGauge(MetricNames.ServiceInfoMapSize, 20);

        // Assert
        _monitor.GetGaugeValue(MetricNames.ServiceInfoMapSize).Should().Be(20);
    }

    [Fact]
    public void IncreaseGauge_ShouldAddToCurrentValue()
    {
        // Arrange
        _monitor.SetGauge(MetricNames.ServiceInfoMapSize, 10);

        // Act
        _monitor.IncreaseGauge(MetricNames.ServiceInfoMapSize, 5);

        // Assert
        _monitor.GetGaugeValue(MetricNames.ServiceInfoMapSize).Should().Be(15);
    }

    [Fact]
    public void DecreaseGauge_ShouldSubtractFromCurrentValue()
    {
        // Arrange
        _monitor.SetGauge(MetricNames.ServiceInfoMapSize, 10);

        // Act
        _monitor.DecreaseGauge(MetricNames.ServiceInfoMapSize, 3);

        // Assert
        _monitor.GetGaugeValue(MetricNames.ServiceInfoMapSize).Should().Be(7);
    }

    #endregion

    #region Histogram Tests

    [Fact]
    public void ObserveHistogram_ShouldRecordValue()
    {
        // Arrange
        _monitor.Reset();

        // Act
        _monitor.ObserveHistogram(MetricNames.RequestLatency, 50.0);
        _monitor.ObserveHistogram(MetricNames.RequestLatency, 100.0);

        // Assert - Just verify no exception is thrown
        // Histogram values are recorded internally
    }

    [Fact]
    public void StartTimer_ShouldRecordElapsedTime()
    {
        // Arrange
        _monitor.Reset();

        // Act
        using (_monitor.StartTimer(MetricNames.ConfigRequestLatency))
        {
            Thread.Sleep(10); // Small delay to ensure measurable time
        }

        // Assert - Timer should record the elapsed time
        // We can't easily verify the exact value, but it should not throw
    }

    [Fact]
    public void StartTimer_MultipleTimers_ShouldRecordAllTimes()
    {
        // Arrange
        _monitor.Reset();

        // Act
        using (_monitor.StartTimer(MetricNames.ConfigRequestLatency)) { Thread.Sleep(5); }
        using (_monitor.StartTimer(MetricNames.ConfigRequestLatency)) { Thread.Sleep(5); }
        using (_monitor.StartTimer(MetricNames.ConfigRequestLatency)) { Thread.Sleep(5); }

        // Assert - All three timer observations should be recorded
        // No exception should be thrown
    }

    #endregion

    #region Snapshot Tests

    [Fact]
    public void GetSnapshot_ShouldReturnAllMetrics()
    {
        // Arrange
        _monitor.Reset();
        _monitor.RecordConfigRequestSuccess();
        _monitor.SetConnectionStatus(true);
        _monitor.SetServiceInfoMapSize(5);

        // Act
        var snapshot = _monitor.GetSnapshot();

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        snapshot.Counters.Should().ContainKey(MetricNames.ConfigRequestSuccessTotal);
        snapshot.Gauges.Should().ContainKey(MetricNames.ConnectionStatus);
        snapshot.Gauges.Should().ContainKey(MetricNames.ServiceInfoMapSize);
    }

    [Fact]
    public void GetSnapshot_CountersValues_ShouldMatchRecordedValues()
    {
        // Arrange
        _monitor.Reset();
        _monitor.RecordConfigRequestSuccess();
        _monitor.RecordConfigRequestSuccess();
        _monitor.RecordNamingRequestFailed();

        // Act
        var snapshot = _monitor.GetSnapshot();

        // Assert
        snapshot.Counters[MetricNames.ConfigRequestSuccessTotal].Value.Should().Be(2);
        snapshot.Counters[MetricNames.NamingRequestFailedTotal].Value.Should().Be(1);
    }

    [Fact]
    public void GetSnapshot_GaugeValues_ShouldMatchSetValues()
    {
        // Arrange
        _monitor.Reset();
        _monitor.SetConnectionStatus(true);
        _monitor.SetServiceInfoMapSize(100);

        // Act
        var snapshot = _monitor.GetSnapshot();

        // Assert
        snapshot.Gauges[MetricNames.ConnectionStatus].Value.Should().Be(1);
        snapshot.Gauges[MetricNames.ServiceInfoMapSize].Value.Should().Be(100);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ShouldClearAllCounters()
    {
        // Arrange
        _monitor.RecordConfigRequestSuccess();
        _monitor.RecordNamingRequestFailed();

        // Act
        _monitor.Reset();

        // Assert
        _monitor.GetCounterValue(MetricNames.ConfigRequestSuccessTotal).Should().Be(0);
        _monitor.GetCounterValue(MetricNames.NamingRequestFailedTotal).Should().Be(0);
    }

    [Fact]
    public void Reset_ShouldClearAllGauges()
    {
        // Arrange
        _monitor.SetServiceInfoMapSize(50);
        _monitor.SetConnectionStatus(true);

        // Act
        _monitor.Reset();

        // Assert
        _monitor.GetGaugeValue(MetricNames.ServiceInfoMapSize).Should().Be(0);
        _monitor.GetGaugeValue(MetricNames.ConnectionStatus).Should().Be(0);
    }

    #endregion

    #region Register Custom Metrics Tests

    [Fact]
    public void RegisterGauge_CustomMetric_ShouldBeUsable()
    {
        // Arrange
        const string customMetricName = "custom_gauge_test";

        // Act
        _monitor.RegisterGauge(customMetricName, "Test gauge");
        _monitor.SetGauge(customMetricName, 123);

        // Assert
        _monitor.GetGaugeValue(customMetricName).Should().Be(123);
    }

    [Fact]
    public void RegisterCounter_CustomMetric_ShouldBeUsable()
    {
        // Arrange
        const string customMetricName = "custom_counter_test";

        // Act
        _monitor.RegisterCounter(customMetricName, "Test counter");
        _monitor.IncreaseCounter(customMetricName, 10);

        // Assert
        _monitor.GetCounterValue(customMetricName).Should().Be(10);
    }

    [Fact]
    public void RegisterHistogram_CustomMetric_ShouldBeUsable()
    {
        // Arrange
        const string customMetricName = "custom_histogram_test";
        var buckets = new[] { 10.0, 50.0, 100.0, 500.0 };

        // Act
        _monitor.RegisterHistogram(customMetricName, "Test histogram", buckets);
        _monitor.ObserveHistogram(customMetricName, 75.0);

        // Assert - Should not throw
    }

    #endregion

    #region Singleton Tests

    [Fact]
    public void Default_ShouldReturnSameInstance()
    {
        // Act
        var instance1 = MetricsMonitor.Default;
        var instance2 = MetricsMonitor.Default;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    #endregion

    #region Integration Scenario Tests

    [Fact]
    public void IntegrationScenario_ConfigService_ShouldTrackMetrics()
    {
        // Arrange
        _monitor.Reset();

        // Act - Simulate config service operations
        _monitor.SetConnectionStatus(true);
        _monitor.SetListenConfigCount(5);

        // Successful requests
        _monitor.RecordConfigRequestSuccess();
        _monitor.RecordConfigRequestSuccess();
        _monitor.RecordConfigRequestSuccess();

        // Failed request
        _monitor.RecordConfigRequestFailed();

        // Config change notification
        _monitor.RecordConfigChangePush();

        // Assert
        _monitor.GetGaugeValue(MetricNames.ConnectionStatus).Should().Be(1);
        _monitor.GetGaugeValue(MetricNames.ListenConfigCount).Should().Be(5);
        _monitor.GetCounterValue(MetricNames.ConfigRequestSuccessTotal).Should().Be(3);
        _monitor.GetCounterValue(MetricNames.ConfigRequestFailedTotal).Should().Be(1);
        _monitor.GetCounterValue(MetricNames.ConfigChangePushTotal).Should().Be(1);
    }

    [Fact]
    public void IntegrationScenario_NamingService_ShouldTrackMetrics()
    {
        // Arrange
        _monitor.Reset();

        // Act - Simulate naming service operations
        _monitor.SetConnectionStatus(true);
        _monitor.SetServiceInfoMapSize(10);

        // Successful requests
        _monitor.RecordNamingRequestSuccess();
        _monitor.RecordNamingRequestSuccess();

        // Service change notifications
        _monitor.RecordServiceChangePush();
        _monitor.RecordServiceChangePush();
        _monitor.RecordServiceChangePush();

        // Assert
        _monitor.GetGaugeValue(MetricNames.ServiceInfoMapSize).Should().Be(10);
        _monitor.GetCounterValue(MetricNames.NamingRequestSuccessTotal).Should().Be(2);
        _monitor.GetCounterValue(MetricNames.ServiceChangePushTotal).Should().Be(3);
    }

    [Fact]
    public void IntegrationScenario_FailoverScenario_ShouldTrackMetrics()
    {
        // Arrange
        _monitor.Reset();

        // Act - Simulate failover scenario
        _monitor.SetConnectionStatus(false);
        _monitor.SetFailoverEnabled(true);

        // Use failover data
        _monitor.RecordFailoverUsed();
        _monitor.RecordFailoverUsed();

        // Redo operations after recovery
        _monitor.SetConnectionStatus(true);
        _monitor.SetFailoverEnabled(false);
        _monitor.RecordRedoOperation();

        // Assert
        _monitor.GetGaugeValue(MetricNames.ConnectionStatus).Should().Be(1);
        _monitor.GetGaugeValue(MetricNames.FailoverEnabled).Should().Be(0);
        _monitor.GetCounterValue(MetricNames.FailoverUsedTotal).Should().Be(2);
        _monitor.GetCounterValue(MetricNames.RedoOperationTotal).Should().Be(1);
    }

    #endregion
}
