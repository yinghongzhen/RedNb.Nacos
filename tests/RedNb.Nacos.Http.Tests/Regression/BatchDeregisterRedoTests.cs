using System.Reflection;
using RedNb.Nacos;
using RedNb.Nacos.Grpc;
using RedNb.Nacos.Grpc.Naming;
using RedNb.Nacos.Naming;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Regression;

public sealed class BatchDeregisterRedoTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public async Task RemovingOneBatchInstanceMustRetainOtherInstanceForReconnectReplay(int count, bool failSecond)
    {
        // A unique namespace isolates this offline fixture's Naming cache.
        // The registration/replay defect does not depend on namespace characters.
        var testNamespace = "rednb-batch-redo-" + Guid.NewGuid().ToString("N");
        string? cacheDirectory = null;
        var options = new NacosClientOptions
        {
            Namespace = testNamespace,
            ServerAddresses = "127.0.0.1:1",
            NamingLoadCacheAtStart = false
        };
        var fake = new OfflineClient(options) { RejectSecond = failSecond };
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

            var removed = new Instance { Ip = "127.0.0.1", Port = 19001, ClusterName = "DEFAULT", Ephemeral = true };
            var retained = new Instance { Ip = "127.0.0.2", Port = 19002, ClusterName = "DEFAULT", Ephemeral = true };
            await service.BatchRegisterInstanceAsync("offline-service", "DEFAULT_GROUP", new List<Instance> { removed, retained });
            Task Deregister() => service.BatchDeregisterInstanceAsync("offline-service", "DEFAULT_GROUP", new List<Instance> { removed, retained }.Take(count).ToList());
            if (failSecond) await Assert.ThrowsAsync<NacosException>(Deregister);
            else await Deregister();

            fake.Requests.Clear();
            var redo = GetField(service, "_redoService");
            var replay = redo.GetType().GetMethod("RedoAsync", BindingFlags.Instance | BindingFlags.Public)!;
            await (Task)replay.Invoke(redo, new object[] { CancellationToken.None })!;

            var replayed = fake.Requests.OfType<BatchInstanceRequest>().SelectMany(r => r.Instances).Select(i => i.Ip).Order().ToArray();
            Assert.Equal(new[] { removed, retained }.Skip(failSecond ? 1 : count).Select(i => i.Ip).Order(), replayed);
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
        public bool RejectSecond { get; init; }
        public List<object> Requests { get; } = new();

        public override Task ConnectAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("This offline test must never connect.");

        public override Task<TResponse?> RequestAsync<TResponse>(string type, object request,
            CancellationToken cancellationToken = default) where TResponse : class
        {
            Requests.Add(request);
            object response = request switch
            {
                BatchInstanceRequest => new BatchInstanceResponse { ResultCode = 200 },
                InstanceRequest r => new InstanceResponse { ResultCode = RejectSecond && r.Type == "deregisterInstance" && r.Instance.Port == 19002 ? 500 : 200 },
                _ => throw new InvalidOperationException("Unexpected offline request: " + type)
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
