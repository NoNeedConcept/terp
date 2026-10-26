---
title: Diagnostics
description: Timers, async timing extensions and an HTTP client handler built on System.Diagnostics.Metrics.
---

# Diagnostics

`Terp.Diagnostics` (package `Terp`) has helpers on top of `System.Diagnostics.Metrics`: `TimerScope` for scoped timing, `TimeAsync` for one-call async timing, and `TerpHttpMetricsHandler` for tagging outbound HTTP metrics.

```csharp
using System.Diagnostics.Metrics;
using Terp.Diagnostics;
```

None of them creates a meter. You pass your own `Meter` and export it yourself.

::: info Exporting
With OpenTelemetry, register the meter by name: `AddMeter("MyApp")`. An instrument on a meter nobody listens to costs close to nothing.
:::

## Time an async operation with TimeAsync

`TimeAsync` is the simplest way to measure duration. It executes a delegate, records the elapsed time in seconds, and adds `outcome=success` or `outcome=failure`.

```csharp
var meter = new Meter("MyApp");
var duration = meter.CreateHistogram<double>("myapp.search.duration", "s");

var result = await duration.TimeAsync(
    async ct => await SearchAsync(ct),
    new TagList { { "source", "tmdb" } },
    cancellationToken);
```

On exception, `outcome=failure` is recorded and the exception is rethrown. There is a `Task<T>` overload that returns the result and a `Task` overload for void operations.

## Time a block with TimerScope

When you need manual control (e.g. timing across message boundaries in an actor), use `StartTimer`. It returns a `TimerScope` struct. The scope records elapsed seconds on dispose.

```csharp
var latency = meter.CreateHistogram<double>("myapp.lookup.duration", "s");

using (var timer = latency.StartTimer(new KeyValuePair<string, object?>("source", "cache")))
{
    try { await LookupAsync(); }
    catch { timer.Fail(); throw; }
}
```

Every measurement carries `outcome`: `success`, or `failure` after `Fail()`. The tag is always present so dashboards can group on it.

The overloads take no tag, one tag, two tags or a `ReadOnlySpan<KeyValuePair<string, object?>>`. Each has an optional `TimeProvider` as the last parameter for testing.

::: warning Pass tags as explicit pairs
Write `new KeyValuePair<string, object?>("source", "cache")`. A target-typed `new("source", "cache")` is ambiguous with the `TimeProvider` overload.
:::

`TimerScope` is a mutable struct (zero allocation). Use it directly in a `using` statement. Do not copy or box it.

## Tag the HTTP client metrics

.NET records `http.client.request.duration` on the meter `System.Net.Http` for every `HttpClient` request. `TerpHttpMetricsHandler` adds two custom tags to those measurements:

| Tag | Value | Example |
|-----|-------|---------|
| `api` | The name passed to the constructor | `tmdb` |
| `status_class` | The HTTP status code class, or `error` on failure | `2xx`, `4xx`, `error` |

```csharp
services.AddHttpClient("tmdb")
    .AddHttpMessageHandler(() => new TerpHttpMetricsHandler("tmdb"));
```

The constants `TerpHttpMetricsHandler.ApiTagName` and `TerpHttpMetricsHandler.StatusClassTagName` hold the tag names.

Add the meter `System.Net.Http` to your exporter:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m.AddMeter("MyApp", "System.Net.Http"));
```

::: info Untagged clients
`System.Net.Http` reports every client, also those without the handler. Filter on `api!=""` in queries when you want only tagged ones.
:::

## Listen without OpenTelemetry

A `MeterListener` reads the instruments in process. This is how `src/Terp.Example` prints them:

```csharp
using var listener = new MeterListener();
listener.InstrumentPublished = (instrument, l) =>
{
    if (instrument.Meter.Name == "MyApp" || instrument.Name == "http.client.request.duration")
    {
        l.EnableMeasurementEvents(instrument);
    }
};
listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
    Console.WriteLine($"{instrument.Name} = {value:F3}"));
listener.Start();
```
