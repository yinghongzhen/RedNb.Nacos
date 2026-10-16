using RedNb.Nacos;
using RedNb.Nacos.Config;
using RedNb.Nacos.Grpc;

var dataId = Environment.GetEnvironmentVariable("NACOS_DATA_ID");
if (string.IsNullOrWhiteSpace(dataId)) { Console.Error.WriteLine("Set NACOS_DATA_ID to an existing configuration."); return 2; }
var options = new NacosClientOptions
{
    ServerAddresses = Environment.GetEnvironmentVariable("NACOS_SERVER") ?? "localhost:8848",
    ConsoleAddresses = Environment.GetEnvironmentVariable("NACOS_CONSOLE") ?? "",
    GrpcPortOffset = int.Parse(Environment.GetEnvironmentVariable("NACOS_GRPC_PORT_OFFSET") ?? "1000", System.Globalization.CultureInfo.InvariantCulture),
    Username = Environment.GetEnvironmentVariable("NACOS_USERNAME"),
    Password = Environment.GetEnvironmentVariable("NACOS_PASSWORD"),
    Namespace = Environment.GetEnvironmentVariable("NACOS_NAMESPACE") ?? ""
};
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
await using var config = await NacosGrpcFactory.CreateConfigServiceAsync(options);
var content = await config.GetConfigAsync(dataId, "DEFAULT_GROUP", 5000, stop.Token);
Console.WriteLine(content == null ? "Configuration does not exist." : $"Loaded {content.Length} characters.");
var listener = new Listener();
await config.AddListenerAsync(dataId, "DEFAULT_GROUP", listener, stop.Token);
try { await Task.Delay(Timeout.Infinite, stop.Token); }
catch (OperationCanceledException) { }
finally { config.RemoveListener(dataId, "DEFAULT_GROUP", listener); }
return 0;

sealed class Listener : IConfigChangeListener
{
    public void OnReceiveConfigInfo(ConfigInfo info) => Console.WriteLine(info.Content == null ? "Configuration deleted." : $"Updated: {info.Content.Length} characters.");
}
