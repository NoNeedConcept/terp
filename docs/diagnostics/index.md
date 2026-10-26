---
title: Diagnostics
description: Timers, job metrics and an HTTP client handler built on System.Diagnostics.Metrics.
---

# Diagnostics

`Terp.Diagnostics` (package `Terp`) has three small helpers on top of `System.Diagnostics.Metrics`: `TimerScope`, `JobMetrics` with `JobRun`, and `TerpHttpMetricsHandler`.

```csharp
using System.Diagnostics.Metrics;
using Terp.Diagnostics;
```

None of them creates a meter. You pass your own `Meter` and export it yourself.

::: info Exporting
With OpenTelemetry, register the meter by name: `AddMeter("MyApp")`. An instrument on a meter nobody listens to costs close to nothing.
:::

## Time a block with TimerScope

`StartTimer` is an extension on `Histogram<double>`. It returns a `TimerScope`. When the scope is disposed it records the elapsed time in seconds on the histogram.

```csharp
var meter = new Meter("MyApp");
var latency = meter.CreateHistogram<double>("myapp.lookup.duration", "s");

using (var timer = latency.StartTimer(new KeyValuePair<string, object?>("source", "cache")))
{
    try { await LookupAsync(); }
    catch { timer.Fail(); throw; }
}
```

Every measurement carries the tag `outcome`: `success`, or `failure` after you called `Fail()`. The tag is always present, so dashboards can group on it. Your own tags come before it.

The overloads take no tag, one tag, two tags or a `ReadOnlySpan<KeyValuePair<string, object?>>`. Each has an optional `TimeProvider` as the last parameter, which you can replace in tests.

::: warning Pass tags as explicit pairs
Write `new KeyValuePair<string, object?>("source", "cache")`. A target-typed `new("source", "cache")` is ambiguous with the `TimeProvider` overload.
:::

`TimerScope` is a mutable struct, so it does not allocate. Use it directly in a `using` statement or declaration. Do not copy it or box it: a copy that is disposed too records a second measurement. Disposing the same variable twice records once.

## Track a recurring job with JobMetrics

`JobMetrics.Create(meter, name)` creates three instruments from one name. Use one `JobMetrics` per job and call `Start()` for each run.

```csharp
var search = JobMetrics.Create(meter, "myapp.search");

using (var run = search.Start())
{
    try { await SearchAsync(); run.Succeed(); }
    catch (Exception ex) { run.Fail(ex); }
}
```

| Instrument | Type | Unit | Tags |
|------------|------|------|------|
| `{name}.runs` | `Counter<long>` | `{run}` | your tags, `outcome`, `error.type` on failure |
| `{name}.duration` | `Histogram<double>` | `s` | your tags, `outcome`, `error.type` on failure |
| `{name}.last_success_timestamp` | `Gauge<double>` | `s` | your tags only |

The gauge holds the Unix time in seconds of the last successful run. It has no `outcome` tag. `name` must not be blank. `Start` has the same tag overloads as `StartTimer`.

`outcome` is `success` or `failure`. `error.type` is the full type name of the exception passed to `Fail(exception)`, or `unknown` when you call `Fail()` without one.

A run reports exactly once; the first result wins, also across threads. A `JobRun` that is disposed without `Succeed` or `Fail` counts as a failure with `error.type=abandoned`. An early `return` or a cancellation therefore never makes a run disappear from the counts.

::: info Missing series after restart
The gauge has no value until the first success in the process. An alert on `time() - ..._last_success_timestamp_seconds` must handle a missing series after a restart.
:::

With the OpenTelemetry Prometheus exporter, `myapp.search` appears as `myapp_search_runs_total`, `myapp_search_duration_seconds_*` and `myapp_search_last_success_timestamp_seconds`. `StartTimer` on `myapp.lookup.duration` appears as `myapp_lookup_duration_seconds_*` with the `outcome` label.

## Tag the HTTP client metrics

.NET records `http.client.request.duration` on the meter `System.Net.Http` for every `HttpClient` request. `TerpHttpMetricsHandler` adds the custom tag `api` (constant `TerpHttpMetricsHandler.TagName`) to those measurements, so you can tell clients apart.

```csharp
services.AddHttpClient("tmdb")
    .AddHttpMessageHandler(() => new TerpHttpMetricsHandler("tmdb"));
```

The constructor takes the value for the tag and throws for a blank name. The handler uses the enrichment callback of the BCL, so the measurement keeps all built-in tags such as `http.response.status_code` and `error.type`, and gains `api`.

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
listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
    Console.WriteLine($"{instrument.Name} += {value}"));
listener.Start();
```

The `Gauge<double>` of `JobMetrics` is reported through the `double` callback as well.
