using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Terp.Diagnostics;
using Terp.Network;
using Terp.Resilience;
using Terp.Threading;

namespace Terp.Example;

internal sealed class DemoWorker(IMeterFactory meterFactory, AppInfo app, WarmupGate gate, IHostApplicationLifetime lifetime)
    : BackgroundService
{
    public const string MeterName = "Terp.Example";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var meter = meterFactory.Create(MeterName);

        Console.WriteLine($"{app.Name}: {app.Greeting}");
        Console.WriteLine($"The startup gate was checked {gate.Attempts} time(s) before this worker started");
        Console.WriteLine();

        PrintSetupTrace();
        DemoTimerScope(meter);
        await DemoTimeAsync(meter, stoppingToken).ConfigureAwait(false);
        DemoCounterExtensions(meter);
        DemoBackoff();
        await DemoHttpMetrics(meter, stoppingToken).ConfigureAwait(false);
        await DemoAsyncLock().ConfigureAwait(false);
        DemoDateTimeExtensions();

        lifetime.StopApplication();
    }

    private static void PrintSetupTrace()
    {
        Console.WriteLine("Targets in the order they were set up:");
        var position = 1;
        foreach (var entry in SetupTrace.Entries)
        {
            Console.WriteLine($"  {position++}. {entry}");
        }

        Console.WriteLine();
    }

    private static void DemoTimerScope(Meter meter)
    {
        Console.WriteLine("TimerScope: a histogram records the duration of each step, tagged with the step number");
        var stepDuration = meter.CreateHistogram<double>("demo.step.duration", "s");
        for (var step = 1; step <= 2; step++)
        {
            using (stepDuration.StartTimer(new KeyValuePair<string, object?>("step", step)))
            {
                Thread.Sleep(50);
            }
        }

        Console.WriteLine();
    }

    private static async Task DemoTimeAsync(Meter meter, CancellationToken ct)
    {
        Console.WriteLine("TimeAsync: times an async operation into a histogram with outcome tag");
        var asyncDuration = meter.CreateHistogram<double>("demo.async.duration", "s");
        await asyncDuration.TimeAsync(
            async token => await Task.Delay(50, token).ConfigureAwait(false),
            cancellationToken: ct).ConfigureAwait(false);
        Console.WriteLine();
    }

    private static void DemoCounterExtensions(Meter meter)
    {
        Console.WriteLine("Counter.Up / UpDownCounter.Up+Down: semantic increment/decrement helpers");
        var counter = meter.CreateCounter<long>("demo.counter");
        counter.Up();
        counter.Up(new KeyValuePair<string, object?>("tag", "a"));

        var upDown = meter.CreateUpDownCounter<long>("demo.updown");
        upDown.Up();
        upDown.Down();
        Console.WriteLine();
    }

    private static void DemoBackoff()
    {
        Console.WriteLine("Backoff: the delay doubles per attempt up to maxDelay");
        var backoff = Backoff.Create(TimeSpan.FromMilliseconds(100), multiplier: 2, maxDelay: TimeSpan.FromSeconds(1));
        Console.WriteLine($"  {string.Join(", ", Enumerable.Range(0, 5).Select(attempt => $"{backoff.Delay(attempt).TotalMilliseconds}ms"))}");
        Console.WriteLine();
    }

    private static async Task DemoHttpMetrics(Meter meter, CancellationToken ct)
    {
        Console.WriteLine("PortFinder + TerpHttpMetricsHandler: the BCL HttpClient metric gets api=demo, status_class=2xx");
        var port = PortFinder.FindFreeLocalPort();

        using var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();

        var serving = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
            var stream = client.GetStream();

            var buffer = new byte[1024];
            _ = await stream.ReadAsync(buffer, ct).ConfigureAwait(false);

            var response = "HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray();
            await stream.WriteAsync(response, ct).ConfigureAwait(false);
        }, ct);

        using (var client = new HttpClient(new TerpHttpMetricsHandler("demo") { InnerHandler = new SocketsHttpHandler() }))
        {
            using var response = await client.GetAsync($"http://localhost:{port}/", ct).ConfigureAwait(false);
            Console.WriteLine($"  GET http://localhost:{port}/ -> {(int)response.StatusCode}");
        }

        await serving.ConfigureAwait(false);
        listener.Stop();
        Console.WriteLine();
    }

    private static async Task DemoAsyncLock()
    {
        Console.WriteLine("AsyncLock: scoped async mutual exclusion");
        using var asyncLock = new AsyncLock();
        using (await asyncLock.AcquireAsync().ConfigureAwait(false))
        {
            Console.WriteLine("  Lock acquired and released");
        }

        Console.WriteLine();
    }

    private static void DemoDateTimeExtensions()
    {
        Console.WriteLine("DateTimeExtensions");
        var now = DateTime.UtcNow;
        Console.WriteLine($"  workday: {now.IsWorkday()}, weekend: {now.IsWeekend()}, in one hour is in the future: {now.AddHours(1).IsInFuture()}");
    }
}
