---
title: Writing setups
description: Setups for each target, one class for several targets, the generic overloads, post-build setups and testing.
---

# Writing setups

A setup (package `Terp`, namespace `Terp.Hosting`) is a small class named `XxxSetup` that implements `ISetup<TTarget>` for the parts of the host it changes. This page shows one for each target. For the model and the ordering rules see [Hosting](/hosting/).

```csharp
using Terp.Hosting;
```

## One target each

Configuration sources, through `IConfigurationManager`:

```csharp
using Microsoft.Extensions.Configuration;

internal sealed class ConfigurationSetup : ISetup<IConfigurationManager>
{
    public void Setup(IConfigurationManager configuration, SetupContext context) =>
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Demo:Greeting"] = "Hello",
        });
}
```

Logging, through `ILoggingBuilder`:

```csharp
using Microsoft.Extensions.Logging;

internal sealed class LoggingSetup : ISetup<ILoggingBuilder>
{
    public void Setup(ILoggingBuilder logging, SetupContext context)
    {
        logging.ClearProviders().AddSimpleConsole(o => o.SingleLine = true);
        logging.AddFilter("Microsoft", LogLevel.Warning);
    }
}
```

Services, through `IServiceCollection`. The context gives read access to configuration and the environment:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal sealed class ServicesSetup : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        var greeting = context.Configuration["Demo:Greeting"] ?? "no greeting";
        services.AddSingleton(new AppInfo(context.Environment.ApplicationName, greeting));
    }
}
```

Metrics, through `IMetricsBuilder`:

```csharp
using Microsoft.Extensions.Diagnostics.Metrics;

internal sealed class MetricsSetup : ISetup<IMetricsBuilder>
{
    public void Setup(IMetricsBuilder metrics, SetupContext context) =>
        metrics.EnableMetrics("MyApp");
}
```

The whole builder, through `IHostApplicationBuilder`. Use this when a setup needs more than the narrower targets give. It runs with every builder type:

```csharp
internal sealed class WorkerSetup : ISetup<IHostApplicationBuilder>
{
    public void Setup(IHostApplicationBuilder builder, SetupContext context) =>
        builder.Services.AddHostedService<DemoWorker>();
}
```

The concrete web builder, through `ISetup<WebApplicationBuilder>`:

```csharp
using Microsoft.AspNetCore.Builder;

internal sealed class KestrelSetup : ISetup<WebApplicationBuilder>
{
    public void Setup(WebApplicationBuilder builder, SetupContext context) =>
        builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);
}
```

The built app, through `ISetup<WebApplication>`. It runs after `Build()`:

```csharp
internal sealed class MapEndpoints : ISetup<WebApplication>
{
    public void Setup(WebApplication app, SetupContext context) =>
        app.MapGet("/ping", () => "pong");
}
```

```csharp
var app = builder.Build();
app.Setup(new MapEndpoints());
```

The context of a post-build setup comes from the app's services: `Configuration` and `Environment` are the resolved `IConfiguration` and `IHostEnvironment`.

## One class for several targets

A class implements `ISetup<TTarget>` once per target. The `Setup` methods are overloads that differ by the target type.

```csharp
internal sealed class DemoSetup
    : ISetup<IConfigurationManager>, ISetup<ILoggingBuilder>, ISetup<IServiceCollection>
{
    public void Setup(IConfigurationManager configuration, SetupContext context) { /* sources */ }
    public void Setup(ILoggingBuilder logging, SetupContext context) { /* providers, filters */ }
    public void Setup(IServiceCollection services, SetupContext context) { /* registrations */ }
}

builder.Setup(new DemoSetup());
```

The targets run in the fixed order configuration, logging, services, metrics, builder. A class can also combine builder targets with `ISetup<WebApplication>`. `builder.Setup` runs the builder-side parts, and `app.Setup` runs the post-build part:

```csharp
internal sealed class ReportsSetup : ISetup<IServiceCollection>, ISetup<WebApplication>
{
    public void Setup(IServiceCollection services, SetupContext context) =>
        services.AddSingleton<ReportService>();

    public void Setup(WebApplication app, SetupContext context) =>
        app.MapGet("/reports", (ReportService reports) => reports.List());
}

builder.Setup(new ReportsSetup());
// ...
app.Setup(new ReportsSetup());
```

::: info Two instances
Each `Setup` call takes its own instance. Keep setups stateless, or pass the same instance to both calls.
:::

## Generic overloads

For a setup with a parameterless constructor you can name the type instead of creating it:

```csharp
builder.Setup<OtherSetup>();                          // returns IHostApplicationBuilder
builder.Setup<WebApplicationBuilder, KestrelSetup>(); // keeps the builder type
app.Setup<MapEndpoints>();                            // returns IHost
app.Setup<WebApplication, MapEndpoints>();            // keeps the app type
```

`Setup<TSetup>()` on a builder returns `IHostApplicationBuilder`, so the concrete builder type is lost for the next call in a chain. `Setup<TBuilder, TSetup>()` returns `TBuilder`. Use it, or `Setup(new ...)`, when a later setup is an `ISetup<WebApplicationBuilder>`.

## Testing a setup

A setup is an ordinary class. Create the target yourself, build a `SetupContext` from a configuration and an environment, and call `Setup`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terp.Hosting;

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?> { ["Demo:Greeting"] = "Hi" })
    .Build();
var environment = Host.CreateApplicationBuilder().Environment;   // any IHostEnvironment works
var context = new SetupContext(configuration, environment);

var services = new ServiceCollection();
new ServicesSetup().Setup(services, context);

using var provider = services.BuildServiceProvider();
Assert.Equal("Hi", provider.GetRequiredService<AppInfo>().Greeting);
```

`SetupContext` has a public constructor taking `IConfiguration` and `IHostEnvironment`. Both arguments must be non-null. Take the environment from a throwaway `Host.CreateApplicationBuilder()` or write a small fake.

::: info Overloads
When a class implements several targets, the compiler picks the overload by the type of the first argument. Pass a variable typed as the target.
:::
