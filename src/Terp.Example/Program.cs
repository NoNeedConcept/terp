using System.Diagnostics.Metrics;
using System.Net;
using Terp;
using Terp.Diagnostics;
using Terp.Example;
using Terp.Network;
using Terp.Resilience;

using var meter = new Meter("Terp.Example");
using var printer = new MeterPrinter(meter);

Console.WriteLine("TimerScope: a histogram records the duration of each step, tagged with the step number");
var stepDuration = meter.CreateHistogram<double>("demo.step.duration", "s");
for (var step = 1; step <= 2; step++)
{
    using (stepDuration.StartTimer(new KeyValuePair<string, object?>("step", step)))
    {
        await Task.Delay(50);
    }
}

Console.WriteLine("TimeAsync: times an async operation into a histogram with outcome tag");
var asyncDuration = meter.CreateHistogram<double>("demo.async.duration", "s");
await asyncDuration.TimeAsync(async ct => await Task.Delay(50, ct), cancellationToken: CancellationToken.None);

Console.WriteLine("PortFinder + TerpHttpMetricsHandler: the BCL HttpClient metric gets the tags api=demo, status_class=2xx");
var port = PortFinder.FindFreeLocalPort();
var prefix = $"http://localhost:{port}/";
using var server = new HttpListener();
server.Prefixes.Add(prefix);
server.Start();
var serving = Task.Run(async () =>
{
    var context = await server.GetContextAsync();
    context.Response.Close();
});

using (var client = new HttpClient(new TerpHttpMetricsHandler("demo") { InnerHandler = new SocketsHttpHandler() }))
{
    using var response = await client.GetAsync(prefix);
    Console.WriteLine($"  GET {prefix} -> {(int)response.StatusCode}");
}

await serving;
server.Stop();

Console.WriteLine("Backoff: the delay doubles per attempt up to maxDelay");
var backoff = Backoff.Create(TimeSpan.FromMilliseconds(100), multiplier: 2, maxDelay: TimeSpan.FromSeconds(1));
Console.WriteLine($"  {string.Join(", ", Enumerable.Range(0, 5).Select(attempt => $"{backoff.Delay(attempt).TotalMilliseconds}ms"))}");

Console.WriteLine("DateTimeExtensions");
var now = DateTime.Now;
Console.WriteLine($"  workday: {now.IsWorkday()}, weekend: {now.IsWeekend()}, in one hour is in the future: {now.AddHours(1).IsInFuture()}");
