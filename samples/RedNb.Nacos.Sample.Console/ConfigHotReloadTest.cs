using Microsoft.Extensions.Logging;
using RedNb.Nacos.Client;
using RedNb.Nacos.Core;
using RedNb.Nacos.Core.Config;

namespace RedNb.Nacos.Sample.Console;

/// <summary>
/// Nacos 3 配置热加载测试类
/// </summary>
public static class ConfigHotReloadTest
{
    public static async Task RunAsync()
    {
        System.Console.WriteLine("===========================================");
        System.Console.WriteLine("  Nacos 3 配置热加载测试");
        System.Console.WriteLine("===========================================");
        System.Console.WriteLine();

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Configuration - 请根据实际 Nacos 服务器配置修改
        var options = new NacosClientOptions
        {
            ServerAddresses = "localhost:8848",
            Username = "nacos",
            Password = "nacos",
            Namespace = "",
            EnableGrpc = false,
            DefaultTimeout = 5000,
            LongPollTimeout = 30000 // 长轮询超时时间
        };

        System.Console.WriteLine($"连接到 Nacos 服务器: {options.ServerAddresses}");
        System.Console.WriteLine();

        // Create config service
        var factory = new NacosFactory(loggerFactory);
        var configService = factory.CreateConfigService(options);

        var dataId = "hot-reload-test-config";
        var group = "DEFAULT_GROUP";
        var configChangeCount = 0;
        var configChangedEvent = new ManualResetEventSlim(false);

        try
        {
            // 1. 创建配置变更监听器
            System.Console.WriteLine("步骤 1: 创建配置变更监听器");
            var listener = new HotReloadTestListener(info =>
            {
                configChangeCount++;
                System.Console.WriteLine();
                System.Console.WriteLine("╔════════════════════════════════════════════╗");
                System.Console.WriteLine("║  ?? 检测到配置热加载!                      ║");
                System.Console.WriteLine("╠════════════════════════════════════════════╣");
                System.Console.WriteLine($"║  DataId: {info.DataId,-32} ║");
                System.Console.WriteLine($"║  Group:  {info.Group,-32} ║");
                System.Console.WriteLine($"║  MD5:    {info.Md5?[..Math.Min(32, info.Md5?.Length ?? 0)],-32} ║");
                System.Console.WriteLine($"║  变更次数: {configChangeCount,-30} ║");
                System.Console.WriteLine("╠════════════════════════════════════════════╣");
                System.Console.WriteLine("║  新配置内容:                               ║");
                System.Console.WriteLine("╚════════════════════════════════════════════╝");
                System.Console.WriteLine(info.Content);
                System.Console.WriteLine();
                configChangedEvent.Set();
            });

            // 2. 发布初始配置
            System.Console.WriteLine("步骤 2: 发布初始配置");
            var initialContent = CreateConfigContent("1.0.0", "初始配置");
            var publishResult = await configService.PublishConfigAsync(dataId, group, initialContent, ConfigType.Json);
            System.Console.WriteLine($"发布结果: {(publishResult ? "成功 ?" : "失败 ?")}");
            System.Console.WriteLine();

            // 等待配置保存
            await Task.Delay(1000);

            // 3. 获取并验证配置
            System.Console.WriteLine("步骤 3: 获取并验证初始配置");
            var content = await configService.GetConfigAsync(dataId, group, 5000);
            System.Console.WriteLine($"获取到的配置内容:");
            System.Console.WriteLine(content);
            System.Console.WriteLine();

            // 4. 添加监听器
            System.Console.WriteLine("步骤 4: 添加配置变更监听器");
            await configService.AddListenerAsync(dataId, group, listener);
            System.Console.WriteLine("监听器已添加 ?");
            System.Console.WriteLine();

            // 5. 交互式测试
            System.Console.WriteLine("===========================================");
            System.Console.WriteLine("  开始交互式热加载测试");
            System.Console.WriteLine("===========================================");
            System.Console.WriteLine();
            System.Console.WriteLine("操作说明:");
            System.Console.WriteLine("  - 按 Enter: 自动发布新配置并等待热加载通知");
            System.Console.WriteLine("  - 输入 'manual': 手动在 Nacos 控制台修改配置，然后等待通知");
            System.Console.WriteLine("  - 输入 'q': 退出测试");
            System.Console.WriteLine();

            var updateCount = 0;
            while (true)
            {
                System.Console.Write("请输入命令: ");
                var input = System.Console.ReadLine()?.ToLower().Trim();

                if (input == "q")
                {
                    break;
                }

                if (input == "manual")
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine("请在 Nacos 控制台手动修改配置:");
                    System.Console.WriteLine($"  - DataId: {dataId}");
                    System.Console.WriteLine($"  - Group: {group}");
                    System.Console.WriteLine("等待配置变更通知...");
                    System.Console.WriteLine("(最长等待 60 秒，或按 Ctrl+C 取消)");
                    System.Console.WriteLine();

                    configChangedEvent.Reset();
                    if (configChangedEvent.Wait(TimeSpan.FromSeconds(60)))
                    {
                        System.Console.WriteLine("? 成功接收到配置变更通知!");
                    }
                    else
                    {
                        System.Console.WriteLine("? 等待超时，未收到配置变更通知");
                    }
                }
                else
                {
                    // 自动发布新配置
                    updateCount++;
                    var newContent = CreateConfigContent($"1.0.{updateCount}", $"第 {updateCount} 次更新");

                    System.Console.WriteLine();
                    System.Console.WriteLine($"发布新配置 (版本 1.0.{updateCount})...");
                    var result = await configService.PublishConfigAsync(dataId, group, newContent, ConfigType.Json);
                    System.Console.WriteLine($"发布结果: {(result ? "成功 ?" : "失败 ?")}");
                    System.Console.WriteLine("等待热加载通知...");

                    configChangedEvent.Reset();
                    if (configChangedEvent.Wait(TimeSpan.FromSeconds(35)))
                    {
                        System.Console.WriteLine("? 热加载测试成功!");
                    }
                    else
                    {
                        System.Console.WriteLine("? 等待超时，未收到热加载通知");
                        System.Console.WriteLine("  可能的原因:");
                        System.Console.WriteLine("  1. 长轮询超时设置过短");
                        System.Console.WriteLine("  2. 网络问题");
                        System.Console.WriteLine("  3. Nacos 服务器配置问题");
                    }
                }
                System.Console.WriteLine();
            }

            // 清理
            System.Console.WriteLine();
            System.Console.WriteLine("--- 清理测试资源 ---");
            configService.RemoveListener(dataId, group, listener);
            System.Console.WriteLine("监听器已移除 ?");

            await configService.RemoveConfigAsync(dataId, group);
            System.Console.WriteLine("测试配置已删除 ?");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine();
            System.Console.WriteLine($"错误: {ex.Message}");
            System.Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            if (configService is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
        }

        System.Console.WriteLine();
        System.Console.WriteLine("测试完成。按任意键退出...");
        System.Console.ReadKey();
    }

    private static string CreateConfigContent(string version, string description)
    {
        return $$"""
        {
            "app": {
                "name": "HotReloadTest",
                "version": "{{version}}",
                "description": "{{description}}",
                "updateTime": "{{DateTime.Now:yyyy-MM-dd HH:mm:ss}}"
            },
            "settings": {
                "enabled": true,
                "timeout": 30000,
                "retryCount": 3
            }
        }
        """;
    }

    private class HotReloadTestListener : IConfigChangeListener
    {
        private readonly Action<ConfigInfo> _callback;

        public HotReloadTestListener(Action<ConfigInfo> callback)
        {
            _callback = callback;
        }

        public void OnReceiveConfigInfo(ConfigInfo configInfo)
        {
            _callback(configInfo);
        }
    }
}
