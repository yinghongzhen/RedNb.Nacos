using System.Globalization;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);
var password = builder.AddParameter("nacos-password", new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true);
// 64 alphanumeric characters are valid Base64 encoding for 48 bytes.
var token = builder.AddParameter("nacos-token", new GenerateParameterDefault { MinLength = 64, Special = false }, secret: true);
var identity = builder.AddParameter("nacos-identity", new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true);

var nacos = builder.AddContainer("nacos", "nacos/nacos-server", "v3.2.4")
    .WithEnvironment("MODE", "standalone")
    .WithEnvironment("NACOS_AUTH_ENABLE", "true")
    .WithEnvironment("NACOS_AUTH_ADMIN_ENABLE", "true")
    .WithEnvironment("NACOS_AUTH_CONSOLE_ENABLE", "true")
    .WithEnvironment("NACOS_AUTH_TOKEN", token)
    .WithEnvironment("NACOS_AUTH_IDENTITY_KEY", "aspire")
    .WithEnvironment("NACOS_AUTH_IDENTITY_VALUE", identity)
    .WithEnvironment("JVM_XMS", "256m")
    .WithEnvironment("JVM_XMX", "512m")
    .WithHttpEndpoint(targetPort: 8848, name: "api")
    .WithHttpEndpoint(targetPort: 8080, name: "console")
    .WithEndpoint(targetPort: 9848, scheme: "tcp", name: "grpc")
    .WithHttpHealthCheck("/v3/console/health/readiness", endpointName: "console");

var bootstrap = builder.AddProject<Projects.RedNb_Nacos_Sample_Bootstrap>("bootstrap")
    .WithEnvironment("NACOS_USERNAME", "nacos")
    .WithEnvironment("NACOS_PASSWORD", password)
    .WithEnvironment(context => SetConnectionAsync(context, "NACOS_SERVER", "NACOS_CONSOLE", "NACOS_GRPC_PORT_OFFSET"))
    .WaitFor(nacos);

var web = builder.AddProject<Projects.RedNb_Nacos_Sample_WebApi>("webapi", launchProfileName: null)
    .WithHttpEndpoint(name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("ServiceRegistry__Ip", "127.0.0.1")
    .WithEnvironment("Nacos__Username", "nacos")
    .WithEnvironment("Nacos__Password", password)
    .WithEnvironment(context => SetConnectionAsync(context, "Nacos__ServerAddresses", "Nacos__ConsoleAddresses", "Nacos__GrpcPortOffset"))
    .WithHttpHealthCheck("/health")
    .WaitForCompletion(bootstrap);
web.WithEnvironment("ServiceRegistry__Port", web.GetEndpoint("http").Property(EndpointProperty.Port));

builder.AddProject<Projects.RedNb_Nacos_Sample_Console>("console")
    .WithEnvironment("NACOS_USERNAME", "nacos")
    .WithEnvironment("NACOS_PASSWORD", password)
    .WithEnvironment("NACOS_DATA_ID", "app-config")
    .WithEnvironment(context => SetConnectionAsync(context, "NACOS_SERVER", "NACOS_CONSOLE", "NACOS_GRPC_PORT_OFFSET"))
    .WaitForCompletion(bootstrap);

builder.AddProject<Projects.RedNb_Nacos_Sample_AI>("ai")
    .WithEnvironment("REDNB_NACOS_Nacos__Username", "nacos")
    .WithEnvironment("REDNB_NACOS_Nacos__Password", password)
    .WithEnvironment("REDNB_NACOS_ChatProvider", "echo")
    .WithEnvironment(context => SetConnectionAsync(context, "REDNB_NACOS_Nacos__ServerAddresses", "REDNB_NACOS_Nacos__ConsoleAddresses", "REDNB_NACOS_Nacos__GrpcPortOffset"))
    .WaitForCompletion(bootstrap);

builder.Build().Run();

async Task SetConnectionAsync(EnvironmentCallbackContext context, string apiKey, string consoleKey, string offsetKey)
{
    var api = new Uri((await nacos.GetEndpoint("api").GetValueAsync(context.CancellationToken))!);
    var grpc = new Uri((await nacos.GetEndpoint("grpc").GetValueAsync(context.CancellationToken))!);
    context.EnvironmentVariables[apiKey] = api.Authority;
    context.EnvironmentVariables[consoleKey] = nacos.GetEndpoint("console");
    context.EnvironmentVariables[offsetKey] = (grpc.Port - api.Port).ToString(CultureInfo.InvariantCulture);
}

namespace RedNb.Nacos.Sample.AppHost
{
    // Entry assembly marker for Aspire.Hosting.Testing.
    public sealed class AppHostMarker;
}
