---
title: Getting started
description: Install Terp, write a first setup and a startup gate, and record a metric.
---

# Getting started

Terp is a set of small helper libraries for .NET 10 applications. This page installs the package, builds a minimal host with one setup and one startup gate, and times a block with a metric helper.

## Install

```bash
dotnet add package Terp
```

`Terp` depends on the Microsoft.Extensions abstractions only (hosting, logging). It does not reference ASP.NET Core and does not bring a concrete host. Your application project provides that, for example through `Microsoft.NET.Sdk.Web` or the `Microsoft.Extensions.Hosting` package.

For Akka.NET applications add the second package, which builds on Akka.Hosting and references `Terp`:

```bash
dotnet add package Terp.Akka
```

## A minimal host

A setup is a class that configures one concern of the host. It declares what it may change by implementing `ISetup<TTarget>`. The `Setup` method receives that target and a read-only `SetupContext`.

```csharp
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terp.Diagnostics;
using Terp.Hosting;
using Terp.Hosting.Gates;

var builder = Host.CreateApplicationBuilder(args);

builder.Setup(new GreetingSetup());                                  // runs immediately
builder.Services.AddStartupGate("greeting", ct => Task.FromResult(true));

using var meter = new Meter("MyApp");
var startup = meter.CreateHistogram<double>("myapp.startup.duration", "s");

using var host = builder.Build();
using (startup.StartTimer())                                         // records seconds with outcome=success
{
    await host.StartAsync();
}

Console.WriteLine(host.Services.GetRequiredService<Greeting>().Text);
await host.StopAsync();

internal sealed record Greeting(string Text);

internal sealed class GreetingSetup : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context) =>
        services.AddSingleton(new Greeting(context.Configuration["Greeting"] ?? "hello"));
}
```

`builder.Setup(...)` applies the setup at once and returns the builder, so calls chain. A setup that implements no supported target throws an `InvalidOperationException` that names the type.

::: info Order
Every `Setup` call runs when you make it. The order of your calls is the order of the setups.
:::

The gate runs while the host starts, before the host reports started. This one is always clear, so startup continues. A gate that returns `false` is checked again with exponential backoff. See [startup gates](/hosting/gates).

`StartTimer()` returns a `TimerScope`. When the `using` block ends it records the elapsed seconds on the histogram and tags the measurement with `outcome`. See [diagnostics](/diagnostics/).

::: info Meters
Terp creates no meter of its own. You pass your `Meter` and export it, for example with `AddMeter("MyApp")` in OpenTelemetry.
:::

## IWithId

`IWithId` is a small interface in the namespace `Terp` (package `Terp`). It has one member, `Guid Id`. Implement it on types that carry a unique identity, so code that only needs the identity can accept `IWithId`.

```csharp
using Terp;

public sealed record Order(Guid Id, string Customer) : IWithId;
```

Terp ships the interface only. It contains no repository, generator or lookup that uses it.

## Where to go next

- [Hosting](/hosting/) explains the model, the targets and the order setups run in.
- [Setups](/hosting/setups) shows a setup for each target and how to test one.
- [Startup gates](/hosting/gates) covers readiness checks before the host starts.
- [Diagnostics](/diagnostics/) has timers, job metrics and the HTTP client metrics handler.
- [Akka](/akka/) describes `Terp.Akka`.

The repository contains runnable demo apps: `src/Terp.Example` (diagnostics, backoff, port finder, date helpers), `src/Terp.Hosting.Example` (setups for all targets, a gate, Native AOT) and `src/Terp.Akka.Example` (actor system, backoff supervisor).
