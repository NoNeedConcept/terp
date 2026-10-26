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

Console.WriteLine("JobMetrics: one run counts as succeeded, one as failed");
var job = JobMetrics.Create(meter, "demo.job");
using (var run = job.Start())
{
    run.Succeed();
}

using (var run = job.Start())
{
    run.Fail(new InvalidOperationException("simulated failure"));
}

Console.WriteLine("PortFinder + TerpHttpMetricsHandler: the BCL HttpClient metric gets the tag api=demo");
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
