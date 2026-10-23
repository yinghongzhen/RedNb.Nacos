using System.Reflection;
using RedNb.Nacos;
using RedNb.Nacos.Grpc;
using RedNb.Nacos.Grpc.Naming;
using RedNb.Nacos.Naming;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Regression;

public sealed class CachedResubscribeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubscribedQueryAfterUnsubscribeMustRestoreServerSubscriptionDespiteCachedInstances(bool cachedOnly)
    {
        // A unique namespace isolates this offline fixture's Naming cache.
        // The subscription defect also occurs with ordinary production namespaces.
        var testNamespace = "rednb-cached-resubscribe-" + Guid.NewGuid().ToString("N");
        string? cacheDirectory = null;
        var options = new NacosClientOptions
        {
            Namespace = testNamespace,
            ServerAddresses = "127.0.0.1:1",
            NamingLoadCacheAtStart = false
        };
        var fake = new OfflineClient(options);
        NacosGrpcNamingService? service = null;
        NacosGrpcClient? originalClient = null;
        try
        {
            service = new NacosGrpcNamingService(options);
            cacheDirectory = (string)GetField(GetField(service, "_serviceInfoHolder"), "_cacheDir");
            originalClient = (NacosGrpcClient)GetField(service, "_grpcClient");
            var transport = GetField(service, "_transportClient");
            SetField(transport, "_grpcClient", fake);
            SetField(service, "_grpcClient", fake);
            SetField(service, "_initialized", true);

            Action<IInstancesChangeEvent> listener = _ => { };
            if (cachedOnly)
            {
                var holder = GetField(service, "_serviceInfoHolder");
                holder.GetType().GetMethod("ProcessServiceInfo")!.Invoke(holder, [new NamingServiceInfo
                { Name = "offline-service", GroupName = "DEFAULT_GROUP", Clusters = "", Hosts = new() { new() { Ip = "127.0.0.1", Port = 19001, Healthy = true, Enabled = true } } }]);
            }
            else
            {
                await service.SubscribeAsync("offline-service", "DEFAULT_GROUP", listener);
                await service.UnsubscribeAsync("offline-service", "DEFAULT_GROUP", listener);
                Assert.Contains(fake.Requests, request => !request.Subscribe);
            }

            fake.Requests.Clear();
            await service.GetAllInstancesAsync("offline-service", "DEFAULT_GROUP", new List<string>(), subscribe: true);

            Assert.Contains(fake.Requests, request => request.Subscribe && request.ServiceName == "offline-service");
            fake.Requests.Clear();
            await service.GetAllInstancesAsync("offline-service", "DEFAULT_GROUP", new List<string>(), subscribe: true);
            Assert.Empty(fake.Requests);
        }
        finally
        {
            try { if (service is not null) await service.DisposeAsync(); }
            finally
            {
                try { if (originalClient is not null) await originalClient.DisposeAsync(); }
                finally
                {
                    try { await fake.DisposeAsync(); }
                    finally { if (cacheDirectory != null) IssueTestFiles.RemoveDirectory(cacheDirectory); }
                }
            }
        }
    }

    private static object GetField(object target, string name) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;

    private static void SetField(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

    private sealed class OfflineClient(NacosClientOptions options) : NacosGrpcClient(options)
    {
        public List<SubscribeServiceRequest> Requests { get; } = new();

        public override Task ConnectAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("This offline test must never connect.");

        public override Task<TResponse?> RequestAsync<TResponse>(string type, object request,
            CancellationToken cancellationToken = default) where TResponse : class
        {
            if (request is not SubscribeServiceRequest subscription)
                throw new InvalidOperationException("Unexpected offline request: " + type);
            Requests.Add(subscription);
            object response = new SubscribeServiceResponse
            {
                ResultCode = 200,
                ServiceInfo = new NamingServiceInfo
                {
                    Name = "offline-service",
                    GroupName = "DEFAULT_GROUP",
                    Clusters = "",
                    Hosts = new List<NamingInstance>
                    {
                        new() { Ip = "127.0.0.1", Port = 19001, ClusterName = "DEFAULT", Healthy = true, Enabled = true }
                    }
                }
            };
            return Task.FromResult((TResponse?)response);
        }

        public override Task SendRequestAsync(string type, object request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected streaming request in offline test.");

        public override Task<TResponse?> SendRequestWithResponseAsync<TResponse>(string type, object request,
            TimeSpan timeout, CancellationToken cancellationToken = default) where TResponse : class =>
            throw new InvalidOperationException("Unexpected unary request in offline test.");
    }
}
