using System.Diagnostics.Metrics;

namespace Terp.Example;

/// <summary>Prints the measurements of the demo meter and of the HttpClient request duration; a real app exports them via OpenTelemetry instead.</summary>
internal sealed class MeterPrinter : IDisposable
{
    private readonly MeterListener _listener = new();

    public MeterPrinter(Meter demoMeter)
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter == demoMeter || instrument.Name == "http.client.request.duration")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            Console.WriteLine($"  {instrument.Name} = {value:F3} ({FormatTags(tags)})"));
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            Console.WriteLine($"  {instrument.Name} += {value} ({FormatTags(tags)})"));
        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
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
