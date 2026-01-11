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
    public const string ServerAddress = "localhost:8848";
    public const string Username = "nacos";
    public const string Password = "nacos";

    public async Task InitializeAsync()
    {
        // Check if Nacos server is available
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);

        var maxRetries = 3;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                // Use the main Nacos page as health check (works with Nacos 3.x)
                var response = await httpClient.GetAsync($"http://{ServerAddress}/nacos/");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Nacos server is available");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Attempt {i + 1}: Failed to connect to Nacos - {ex.Message}");
                if (i < maxRetries - 1)
                {
                    await Task.Delay(1000);
                }
            }
        }

        throw new InvalidOperationException(
            $"Nacos server is not available at {ServerAddress}. " +
            "Please ensure Nacos is running before executing integration tests.");
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
