using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Terp.Example;

internal sealed class ConsoleMetricsListener : IMetricsListener
{
    public string Name => "console";

    public bool InstrumentPublished(Instrument instrument, out object? userState)
    {
        userState = null;
        return instrument.Meter.Name is DemoWorker.MeterName or "System.Net.Http";
    }

    public MeasurementHandlers GetMeasurementHandlers()
    {
        return new MeasurementHandlers
        {
            LongHandler = (instrument, value, tags, _) =>
                Console.WriteLine($"  metric {instrument.Name} += {value} ({FormatTags(tags)})"),
            DoubleHandler = (instrument, value, tags, _) =>
                Console.WriteLine($"  metric {instrument.Name} = {value:F3} ({FormatTags(tags)})"),
        };
    }

    public void Initialize(IObservableInstrumentsSource source)
    {
    }

    public void MeasurementsCompleted(Instrument instrument, object? userState)
    {
    }

    private static string FormatTags(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var parts = new List<string>();
        foreach (var tag in tags)
        {
            parts.Add($"{tag.Key}={tag.Value}");
        }

        return string.Join(", ", parts);
    }
}
