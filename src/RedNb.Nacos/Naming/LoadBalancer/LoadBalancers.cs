namespace RedNb.Nacos.Naming.LoadBalancer;

/// <summary>
/// 负载均衡器接口
/// </summary>
public interface ILoadBalancer
{
    /// <summary>
    /// 选择一个实例
    /// </summary>
    Instance? Select(List<Instance> instances);
}

/// <summary>
/// 随机负载均衡器
/// </summary>
public sealed class RandomLoadBalancer : ILoadBalancer
{
    public Instance? Select(List<Instance> instances)
    {
        if (instances == null || instances.Count == 0)
        {
            return null;
        }

        var index = Random.Shared.Next(instances.Count);
        return instances[index];
    }
}

/// <summary>
/// 轮询负载均衡器
/// </summary>
public sealed class RoundRobinLoadBalancer : ILoadBalancer
{
    private int _index = -1;

    public Instance? Select(List<Instance> instances)
    {
        if (instances == null || instances.Count == 0)
        {
            return null;
        }

        var index = Interlocked.Increment(ref _index);
        return instances[index % instances.Count];
    }
}

/// <summary>
/// 加权随机负载均衡器
/// </summary>
public sealed class WeightedRandomLoadBalancer : ILoadBalancer
{
    public Instance? Select(List<Instance> instances)
    {
        if (instances == null || instances.Count == 0)
        {
            return null;
        }

        var totalWeight = instances.Sum(i => i.Weight);
        if (totalWeight <= 0)
        {
            return instances[Random.Shared.Next(instances.Count)];
        }

        var random = Random.Shared.NextDouble() * totalWeight;
        var currentWeight = 0d;

        foreach (var instance in instances)
        {
            currentWeight += instance.Weight;
            if (currentWeight >= random)
            {
                return instance;
            }
        }

        return instances[^1];
    }
}

/// <summary>
/// 加权轮询负载均衡器
/// </summary>
public sealed class WeightedRoundRobinLoadBalancer : ILoadBalancer
{
    private readonly ConcurrentDictionary<string, int> _weights = new();
    private int _currentIndex = -1;
    private int _currentWeight = 0;

    public Instance? Select(List<Instance> instances)
    {
        if (instances == null || instances.Count == 0)
        {
            return null;
        }

        var maxWeight = (int)instances.Max(i => i.Weight);
        var gcdWeight = GetGcd(instances.Select(i => (int)i.Weight).ToArray());

        while (true)
        {
            _currentIndex = (_currentIndex + 1) % instances.Count;

            if (_currentIndex == 0)
            {
                _currentWeight -= gcdWeight;
                if (_currentWeight <= 0)
                {
                    _currentWeight = maxWeight;
                    if (_currentWeight == 0)
                    {
                        return null;
                    }
                }
            }

            var instance = instances[_currentIndex];
            if (instance.Weight >= _currentWeight)
            {
                return instance;
            }
        }
    }

    private static int GetGcd(int[] numbers)
    {
        if (numbers.Length == 0) return 1;
        if (numbers.Length == 1) return numbers[0];

        var result = numbers[0];
        for (var i = 1; i < numbers.Length; i++)
        {
            result = Gcd(result, numbers[i]);
        }
        return result;
    }

    private static int Gcd(int a, int b)
    {
        while (b != 0)
        {
            var temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }
}

/// <summary>
/// 负载均衡器工厂
/// </summary>
public static class LoadBalancerFactory
{
    /// <summary>
    /// 创建负载均衡器
    /// </summary>
    public static ILoadBalancer Create(LoadBalancerStrategy strategy)
    {
        return strategy switch
        {
            LoadBalancerStrategy.Random => new RandomLoadBalancer(),
            LoadBalancerStrategy.RoundRobin => new RoundRobinLoadBalancer(),
            LoadBalancerStrategy.WeightedRandom => new WeightedRandomLoadBalancer(),
            LoadBalancerStrategy.WeightedRoundRobin => new WeightedRoundRobinLoadBalancer(),
            _ => new WeightedRandomLoadBalancer()
        };
    }
}
