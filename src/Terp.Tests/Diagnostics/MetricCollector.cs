using System.Diagnostics.Metrics;

namespace Terp.Tests.Diagnostics;

internal sealed class MetricCollector : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly List<RecordedMeasurement> _measurements = [];
    private readonly Lock _gate = new();

    public MetricCollector(Meter meter)
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (ReferenceEquals(instrument.Meter, meter))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Add(instrument, value, tags));
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Add(instrument, value, tags));
        _listener.Start();
    }

    public IReadOnlyList<RecordedMeasurement> For(string instrument)
    {
        lock (_gate)
        {
            return _measurements.Where(m => m.Instrument == instrument).ToList();
        }
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    private void Add(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var map = new Dictionary<string, object?>();
        foreach (var tag in tags)
        {
            map[tag.Key] = tag.Value;
        }

        lock (_gate)
        {
            _measurements.Add(new RecordedMeasurement(instrument.Name, value, map));
        }
    }
}
