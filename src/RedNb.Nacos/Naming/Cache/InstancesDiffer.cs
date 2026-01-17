using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedNb.Nacos.Core.Naming;

namespace RedNb.Nacos.Naming.Cache;

/// <summary>
/// Nacos 命名服务实例列表差异比较器
/// 参考 Java SDK: com.alibaba.nacos.client.naming.cache.InstancesDiffer
/// </summary>
public sealed class InstancesDiffer
{
    private readonly ILogger? _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public InstancesDiffer(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 计算新旧服务信息之间的实例差异
    /// </summary>
    /// <param name="oldService">旧服务信息</param>
    /// <param name="newService">新服务信息</param>
    /// <returns>实例差异信息</returns>
    public InstancesDiff DoDiff(ServiceInfo? oldService, ServiceInfo newService)
    {
        var instancesDiff = new InstancesDiff();

        if (oldService == null)
        {
            _logger?.LogInformation("init new ips({IpCount}) service: {Key} -> {Hosts}",
                newService.IpCount(), newService.Key, JsonSerializer.Serialize(newService.Hosts));
            instancesDiff.SetAddedInstances(newService.Hosts);
            return instancesDiff;
        }

        if (oldService.LastRefTime > newService.LastRefTime)
        {
            _logger?.LogWarning("out of date data received, old-t: {OldTime}, new-t: {NewTime}",
                oldService.LastRefTime, newService.LastRefTime);
            return instancesDiff;
        }

        var oldHostMap = new Dictionary<string, Instance>();
        foreach (var host in oldService.Hosts)
        {
            oldHostMap[host.ToInetAddr()] = host;
        }

        var newHostMap = new Dictionary<string, Instance>();
        foreach (var host in newService.Hosts)
        {
            newHostMap[host.ToInetAddr()] = host;
        }

        var modHosts = new HashSet<Instance>();
        var newHosts = new HashSet<Instance>();
        var remvHosts = new HashSet<Instance>();

        // 检查新服务中的实例
        foreach (var (key, host) in newHostMap)
        {
            if (oldHostMap.TryGetValue(key, out var oldHost))
            {
                // 旧服务中存在，检查是否有修改
                if (host.ToString() != oldHost.ToString())
                {
                    modHosts.Add(host);
                }
            }
            else
            {
                // 新增的实例
                newHosts.Add(host);
            }
        }

        // 检查旧服务中的实例是否被移除
        foreach (var (key, host) in oldHostMap)
        {
            if (!newHostMap.ContainsKey(key))
            {
                remvHosts.Add(host);
            }
        }

        if (newHosts.Count > 0)
        {
            _logger?.LogInformation("new ips({Count}) service: {Key} -> {Hosts}",
                newHosts.Count, newService.Key, JsonSerializer.Serialize(newHosts));
            instancesDiff.SetAddedInstances(newHosts);
        }

        if (remvHosts.Count > 0)
        {
            _logger?.LogInformation("removed ips({Count}) service: {Key} -> {Hosts}",
                remvHosts.Count, newService.Key, JsonSerializer.Serialize(remvHosts));
            instancesDiff.SetRemovedInstances(remvHosts);
        }

        if (modHosts.Count > 0)
        {
            _logger?.LogInformation("modified ips({Count}) service: {Key} -> {Hosts}",
                modHosts.Count, newService.Key, JsonSerializer.Serialize(modHosts));
            instancesDiff.SetModifiedInstances(modHosts);
        }

        return instancesDiff;
    }
}
