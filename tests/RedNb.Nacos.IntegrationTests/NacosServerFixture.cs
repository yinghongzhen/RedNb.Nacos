using Xunit;

namespace RedNb.Nacos.IntegrationTests;

/// <summary>
/// Collection definition for Nacos integration tests.
/// Tests in this collection will not run in parallel.
/// </summary>
[CollectionDefinition("NacosIntegration")]
public class NacosIntegrationCollection : ICollectionFixture<NacosServerFixture>
{
}

/// <summary>
/// Fixture that ensures Nacos server is available before running tests.
/// </summary>
public class NacosServerFixture : IAsyncLifetime
{
    public static string ServerAddress => Environment.GetEnvironmentVariable("NACOS_TEST_SERVER") ?? "localhost:8848";
    public static string ConsoleAddress => Environment.GetEnvironmentVariable("NACOS_TEST_CONSOLE") ?? "localhost:8080";
    public static string Username => Environment.GetEnvironmentVariable("NACOS_TEST_USERNAME") ?? "nacos";
    public static string Password => Environment.GetEnvironmentVariable("NACOS_TEST_PASSWORD") ?? "nacos";
    public static string Namespace => Environment.GetEnvironmentVariable("NACOS_TEST_NAMESPACE") ?? "";

    // Nacos 3.2.4 boots slower than 3.1.x due to dist module initialization,
    // so the readiness probe is given a generous start_period budget (matches
    // the compose healthcheck start_period in deploy/docker-compose/*.yml).
    // The wait is implemented as a deadline so the actual elapsed time on a
    // fully unresponsive server is bounded by this value plus one in-flight
    // probe (the per-attempt HTTP timeout is capped at min(10s, remaining)).
    private const int StartPeriodSeconds = 90;
    private const int PollIntervalSeconds = 3;
    private static readonly TimeSpan MaxProbeTimeout = TimeSpan.FromSeconds(10);

    public async ValueTask InitializeAsync()
    {
        // Deadline-based wait: elapsed time is bounded by StartPeriodSeconds
        // regardless of per-attempt timeouts.
        // The readiness endpoint lives on the Nacos Console (port 8080 by
        // default), not on the API port (8848). It is bound to
        // com/alibaba/nacos/console/controller/v3/ConsoleHealthController
        // in nacos-console-3.2.4.jar.
        // We construct a fresh HttpClient per iteration: HttpClient.Timeout
        // is immutable after the first request, so reusing a single instance
        // across iterations throws once the cap is reapplied.

        var deadline = DateTime.UtcNow.AddSeconds(StartPeriodSeconds);
        var attempt = 0;

        while (true)
        {
            attempt++;

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            // Cap the per-attempt HTTP timeout so a single probe can never
            // blow past the deadline by itself.
            var probeTimeout = remaining < MaxProbeTimeout ? remaining : MaxProbeTimeout;

            try
            {
                using var httpClient = new HttpClient { Timeout = probeTimeout };
                var response = await httpClient.GetAsync(
                    $"{(ConsoleAddress.Contains("://") ? ConsoleAddress : "http://" + ConsoleAddress).TrimEnd('/')}/v3/console/health/readiness");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Nacos server is ready (attempt {attempt})");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Attempt {attempt}: Failed to connect to Nacos - {ex.Message}");
            }

            // Bail out before sleeping if we have no budget left, and bound
            // the sleep so it cannot run past the deadline either.
            if (DateTime.UtcNow >= deadline)
            {
                break;
            }

            var sleep = TimeSpan.FromSeconds(PollIntervalSeconds);
            var remainingAfterProbe = deadline - DateTime.UtcNow;
            if (sleep > remainingAfterProbe)
            {
                sleep = remainingAfterProbe;
            }
            if (sleep > TimeSpan.Zero)
            {
                await Task.Delay(sleep);
            }
        }

        throw new InvalidOperationException(
            $"Nacos server did not become ready within {StartPeriodSeconds}s at {ServerAddress}. " +
            "Please ensure Nacos 3.x is running before executing integration tests.");
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
