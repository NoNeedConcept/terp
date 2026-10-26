namespace Terp.Tests.Diagnostics;

internal sealed record RecordedMeasurement(string Instrument, double Value, IReadOnlyDictionary<string, object?> Tags);
