using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;

namespace Terp.Hosting.Example;

internal sealed class DemoWorker(IMeterFactory meterFactory, AppInfo app, WarmupGate gate, IHostApplicationLifetime lifetime)
    : BackgroundService
{
    public const string MeterName = "Terp.Hosting.Example";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"{app.Name}: {app.Greeting}; the startup gate was checked {gate.Attempts} times before this worker started");
        Console.WriteLine("Targets in the order they were set up:");
        var position = 1;
        foreach (var entry in SetupTrace.Entries)
        {
            Console.WriteLine($"  {position++}. {entry}");
        }

        using var meter = meterFactory.Create(MeterName);
        meter.CreateCounter<long>("example.setups").Add(SetupTrace.Entries.Count);

        lifetime.StopApplication();
        return Task.CompletedTask;
    }
}
