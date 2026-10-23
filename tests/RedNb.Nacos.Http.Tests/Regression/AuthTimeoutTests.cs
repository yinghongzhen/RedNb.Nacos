using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using RedNb.Nacos;
using RedNb.Nacos.Http.Transport;
using Xunit;

namespace RedNb.Nacos.Http.Tests.Regression;

public sealed class AuthTimeoutTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PerRequestTimeout_ShouldAlsoCancelTheLoginPhase(bool raw)
    {
        var loginRequests = 0;
        var slowLogin = false;
        await using var server = new LoopbackHttpServer(async context =>
        {
            if (context.Request.Url!.AbsolutePath == "/nacos/v3/auth/user/login")
            {
                Interlocked.Increment(ref loginRequests);
                if (slowLogin) await Task.Delay(TimeSpan.FromSeconds(2));
                await RespondAsync(context, "{\"accessToken\":\"local-test-token\",\"tokenTtl\":18000}");
                return;
            }
            await RespondAsync(context, "ok");
        });
        using var client = new NacosHttpClient(new NacosClientOptions
        {
            ServerAddresses = server.Address,
            Username = "local-test-user",
            Password = "local-test-password",
            DefaultTimeout = 10_000
        });

        // Warm the transport/JIT separately; the measured client still has no token.
        using (var warmup = new NacosHttpClient(new NacosClientOptions
        { ServerAddresses = server.Address, Username = "local-test-user", Password = "local-test-password", DefaultTimeout = 10000 }))
        {
            await warmup.GetAsync("probe", cancellationToken: TestContext.Current.CancellationToken);
            await warmup.GetRawAsync("probe", cancellationToken: TestContext.Current.CancellationToken);
        }
        Interlocked.Exchange(ref loginRequests, 0);
        slowLogin = true;

        var watch = Stopwatch.StartNew();
        var error = await Record.ExceptionAsync(async () => { if (raw) await client.GetRawAsync("probe", timeout: 100); else await client.GetAsync("probe", timeout: 100); });
        watch.Stop();

        // Confirm the failure is the SDK's timeout, rather than an unrelated transport/login error.
        var timeout = Assert.IsType<NacosException>(error);
        Assert.Equal(NacosException.ServerError, timeout.ErrorCode);
        Assert.Equal("Request timeout", timeout.Message);
        Assert.Equal(1, Volatile.Read(ref loginRequests));
        Assert.True(watch.Elapsed <= TimeSpan.FromSeconds(1),
            $"100ms request budget must cover authentication; elapsed {watch.Elapsed.TotalMilliseconds:F0}ms.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WaitingForLoginLockHonorsOwnTimeoutOrCancellation(bool cancel)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var logins = 0;
        await using var server = new LoopbackHttpServer(async context =>
        {
            if (context.Request.Url!.AbsolutePath.EndsWith("/login", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref logins); entered.TrySetResult();
                await Task.Delay(700);
                await RespondAsync(context, "{\"accessToken\":\"local-test-token\",\"tokenTtl\":18000}");
            }
            else await RespondAsync(context, "ok");
        });
        using var client = new NacosHttpClient(new NacosClientOptions { ServerAddresses = server.Address, Username = "test", Password = "test", DefaultTimeout = 5000 });
        var owner = client.GetAsync("probe", cancellationToken: TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        if (cancel) stop.CancelAfter(70);
        var watch = Stopwatch.StartNew();
        var error = await Record.ExceptionAsync(() => client.GetRawAsync("probe", timeout: cancel ? 5000 : 100, cancellationToken: stop.Token));
        if (cancel) Assert.IsAssignableFrom<OperationCanceledException>(error);
        else Assert.Equal("Request timeout", Assert.IsType<NacosException>(error).Message);
        Assert.True(watch.Elapsed < TimeSpan.FromMilliseconds(600));
        Assert.Equal("ok", await owner);
        Assert.Equal(1, logins);
    }

    private static async Task RespondAsync(HttpListenerContext context, string body)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }
        catch (HttpListenerException) { }
        catch (ObjectDisposedException) { }
    }

    private sealed class LoopbackHttpServer : IAsyncDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly ConcurrentBag<Task> _requests = new();
        private readonly Task _acceptLoop;
        public string Address { get; }

        public LoopbackHttpServer(Func<HttpListenerContext, Task> handle)
        {
            var portReservation = new TcpListener(IPAddress.Loopback, 0);
            portReservation.Start();
            var port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
            portReservation.Stop();
            Address = $"127.0.0.1:{port}";
            _listener.Prefixes.Add($"http://{Address}/");
            _listener.Start();
            _acceptLoop = Task.Run(async () =>
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext context;
                    try { context = await _listener.GetContextAsync(); }
                    catch (HttpListenerException) { break; }
                    catch (ObjectDisposedException) { break; }
                    _requests.Add(handle(context));
                }
            });
        }

        public async ValueTask DisposeAsync()
        {
            _listener.Close();
            await _acceptLoop;
            await Task.WhenAll(_requests.ToArray());
        }
    }
}
