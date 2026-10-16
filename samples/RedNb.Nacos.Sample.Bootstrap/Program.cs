using RedNb.Nacos;
using RedNb.Nacos.Grpc;

var options = new NacosClientOptions
{
    ServerAddresses = Environment.GetEnvironmentVariable("NACOS_SERVER") ?? throw new InvalidOperationException("NACOS_SERVER is required."),
    ConsoleAddresses = Environment.GetEnvironmentVariable("NACOS_CONSOLE") ?? "",
    Username = Environment.GetEnvironmentVariable("NACOS_USERNAME") ?? "nacos",
    Password = Environment.GetEnvironmentVariable("NACOS_PASSWORD") ?? throw new InvalidOperationException("NACOS_PASSWORD is required."),
    GrpcPortOffset = int.Parse(Environment.GetEnvironmentVariable("NACOS_GRPC_PORT_OFFSET") ?? "1000", System.Globalization.CultureInfo.InvariantCulture)
};
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
using var response = await http.PostAsync(options.GetBaseUrl(options.ServerAddresses) + "/v3/auth/user/admin",
    new FormUrlEncodedContent(new Dictionary<string, string> { ["password"] = options.Password }));
response.EnsureSuccessStatusCode();

await using var config = await NacosGrpcFactory.CreateConfigServiceAsync(options);
if (!await config.PublishConfigAsync("app-config", "DEFAULT_GROUP", """{"Demo":{"Message":"Hello from Aspire and Nacos"}}""", "json"))
    throw new InvalidOperationException("Failed to seed app-config.");
Console.WriteLine("Nacos initialized; app-config is ready. Starting samples.");
