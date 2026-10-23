using System.Reflection;
using RedNb.Nacos;
using RedNb.Nacos.Grpc;
using RedNb.Nacos.Grpc.Naming;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Regression;

public class NamingCacheIsolationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DifferentServerClustersMustNotLoadEachOthersNamingCache(bool equivalent)
    {
        var namespaceId = "issue-" + Guid.NewGuid().ToString("N");
        var serviceName = "service-" + Guid.NewGuid().ToString("N");
        var directories = new HashSet<string>();
        var type = typeof(NacosGrpcClient).Assembly.GetType("RedNb.Nacos.Grpc.Naming.NamingServiceInfoHolder")!;
        object Holder(string server, bool load)
        {
            var options = new NacosClientOptions { ServerAddresses = server, Namespace = namespaceId, NamingLoadCacheAtStart = load };
            var holder = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new object?[] { options, null }, null)!;
            directories.Add((string)type.GetField("_cacheDir", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(holder)!);
            return holder;
        }
        try
        {
            var clusterA = Holder(equivalent ? "B.invalid:8848,a.invalid:8848" : "cluster-a.invalid:8848", false);
            type.GetMethod("ProcessServiceInfo")!.Invoke(clusterA, new object[] {
                new NamingServiceInfo {
                    Name = serviceName, GroupName = "G", Clusters = "",
                    Hosts = new() { new() { Ip = "127.0.0.9", Port = 9000, Healthy = true, Enabled = true } },
                    LastRefTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }
            });
            var clusterB = Holder(equivalent ? "http://a.invalid:8848,http://b.invalid:8848,a.invalid:8848" : "cluster-b.invalid:8848", true);
            var fromB = (NamingServiceInfo?)type.GetMethod("GetServiceInfo")!.Invoke(clusterB, new object?[] { serviceName, "G", "" });

            if (equivalent) Assert.NotNull(fromB);
            else Assert.Null(fromB);
        }
        finally
        {
            // Both directories belong to this freshly generated namespace only.
            foreach (var directory in directories)
                if (Directory.Exists(directory)) IssueTestFiles.RemoveDirectory(directory);
        }
    }
}
