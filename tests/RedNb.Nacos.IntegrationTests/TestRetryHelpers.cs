using RedNb.Nacos;
using Xunit;

namespace RedNb.Nacos.IntegrationTests;

/// <summary>
/// Shared settle-aware retry helpers for integration tests. The console API
/// can lag a released write by up to ~1s; polls tolerate that lag while a
/// plain Task.Delay either flakes or wastes time.
/// </summary>
internal static class TestRetryHelpers
{
    internal static async Task<T> WaitForAsync<T>(
        ITestOutputHelper output,
        Func<Task<T>> read,
        Func<T, bool> isSettled,
        int timeoutMs = 2000,
        int pollMs = 200)
        where T : class?
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        NacosException? lastServerError = null;

        while (true)
        {
            try
            {
                var value = await read();
                if (value is not null && isSettled(value))
                {
                    return value;
                }
            }
            catch (NacosException ex)
            {
                lastServerError = ex; // transient while the release settles
            }

            if (DateTime.UtcNow >= deadline)
            {
                if (lastServerError is not null)
                {
                    throw lastServerError;
                }

                throw new TimeoutException($"The console read did not settle within {timeoutMs} ms.");
            }

            output.WriteLine($"WaitForAsync: read not settled yet; retrying in {pollMs} ms.");
            await Task.Delay(pollMs);
        }
    }

    internal static async Task DeleteWithRetryAsync(ITestOutputHelper output, Func<Task> delete, int attempts = 3)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await delete();
                return;
            }
            catch (NacosException ex) when (attempt < attempts)
            {
                output.WriteLine($"DeleteWithRetryAsync: {ex.Message}; retrying.");
                await Task.Delay(200);
            }
        }
    }
}
