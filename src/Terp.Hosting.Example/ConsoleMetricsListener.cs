using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Terp.Hosting.Example;

internal sealed class ConsoleMetricsListener : IMetricsListener
{
    public string Name => "console";

    public bool InstrumentPublished(Instrument instrument, out object? userState)
    {
        userState = null;
        return instrument.Meter.Name == DemoWorker.MeterName;
    }

    public MeasurementHandlers GetMeasurementHandlers()
    {
        return new MeasurementHandlers
        {
            LongHandler = (instrument, value, _, _) => Console.WriteLine($"  metric {instrument.Name} += {value}"),
        };
    }

    public void Initialize(IObservableInstrumentsSource source)
    {
    }

    public void MeasurementsCompleted(Instrument instrument, object? userState)
    {
    }
}
