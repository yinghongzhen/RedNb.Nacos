using System.Reflection;
using System.Runtime.CompilerServices;
using RedNb.Nacos.Http.Config;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Regression;

public class HttpConfigCacheKeyTests
{
    [Theory]
    [InlineData("a", "b_c", "a_b", "c")]
    [InlineData("G", "key", "g", "key")]
    [InlineData("group", "a/b", "group", "a_b")]
    public void DistinctGroupDataIdPairsMustNotOverwriteEachOther(string g1, string d1, string g2, string d2)
    {
        var directory = Path.Combine(Path.GetTempPath(), "nacos-cache-issue-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            // Isolate files in a temporary directory; keep the package's real key generation and IO methods.
            var cache = (LocalConfigCache)RuntimeHelpers.GetUninitializedObject(typeof(LocalConfigCache));
            typeof(LocalConfigCache).GetField("_cacheDir", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(cache, directory);
            typeof(LocalConfigCache).GetField("_lock", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(cache, new object());
            cache.SaveSnapshot(d1, g1, "snapshot-A");
            cache.SaveSnapshot(d2, g2, "snapshot-B");
            cache.SaveFailover(d1, g1, "failover-A");
            cache.SaveFailover(d2, g2, "failover-B");

            Assert.Equal(
                new[] { "snapshot-A", "snapshot-B", "failover-A", "failover-B" },
                new[] { cache.GetSnapshot(d1, g1), cache.GetSnapshot(d2, g2), cache.GetFailover(d1, g1), cache.GetFailover(d2, g2) });
        }
        finally
        {
            IssueTestFiles.RemoveDirectory(directory);
        }
    }
}
