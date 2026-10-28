---
title: Hosting
description: The setup model, its targets, the order setups run in, and what is safe for trimming and Native AOT.
---

# Hosting

Terp structures host startup as a list of setups. A setup is a class that configures one concern. It implements `ISetup<TTarget>` once for each part of the host it changes. You list the setups in `Program.cs` and Terp applies them. The types live in `Terp.Hosting` (package `Terp`).

```csharp
using Terp.Hosting;
```

## The model

`ISetup` is a marker interface. `ISetup<in TTarget>` adds one method:

```csharp
void Setup(TTarget target, SetupContext context);
```

The target is the part of the host the setup may change. `SetupContext` is read-only and exposes `Configuration` (`IConfiguration`) and `Environment` (`IHostEnvironment`). A setup receives only the part of the host it asks for.

| Implement | Target | Typical use |
|-----------|--------|-------------|
| `ISetup<IConfigurationManager>` | `builder.Configuration` | add configuration sources |
| `ISetup<ILoggingBuilder>` | `builder.Logging` | configure logging |
| `ISetup<IServiceCollection>` | `builder.Services` | register services |
| `ISetup<IMetricsBuilder>` | `builder.Metrics` | configure metrics |
| `ISetup<IHostApplicationBuilder>` | the builder | full builder access; fits every builder type |
| `ISetup<WebApplicationBuilder>` | the concrete builder | for example `builder.WebHost.ConfigureKestrel` |
| `ISetup<WebApplication>` | the built app | after `Build()`, for example map endpoints |

The last row, and any other `IHost`, is applied with `app.Setup(...)` after `Build()`. All others are applied with `builder.Setup(...)` before it.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .Setup<LoggingSetup>()
    .Setup<ServicesSetup>();

var app = builder.Build();
app.Setup(new MapEndpoints());           // MapEndpoints : ISetup<WebApplication>
await app.RunAsync();
```

[Setups](/hosting/setups) shows an example for each target.

## Order

`builder.Setup(x)` runs the setup immediately, so setups run in the order of your calls. There is no discovery, no ordering attribute and no phase numbers. You write the order down.

::: info Order across setups
If setup B reads a service that setup A registers, call A first. Terp does not sort.
:::

Inside one setup class that implements several targets, the targets run in a fixed order: configuration, logging, services, metrics, builder. The order of the interfaces in the class declaration does not matter.

::: info Why a fixed order
A setup can add a configuration source and then read `context.Configuration` in its services part. The configuration part has already run.
:::

`app.Setup(...)` also runs immediately and in call order. Order matters here for routing: a catch-all route registered early swallows endpoints mapped later.

## Builder types

`Setup` works on any `IHostApplicationBuilder`, including `WebApplication.CreateBuilder(args)` and `Host.CreateApplicationBuilder(args)`. The method returns the same builder type it was called on, so calls chain without losing the type.

The builder target is matched against the static type of the receiver. To apply an `ISetup<WebApplicationBuilder>`, call `Setup` on an expression typed `WebApplicationBuilder`. Use `Setup<TBuilder, TSetup>()` when you create setups by type.

::: warning Mismatched builder target
If a setup implements `ISetup<WebApplicationBuilder>` and you apply it to a `HostApplicationBuilder`, that part is silently skipped, provided another target of the same setup matches. Terp cannot detect this without reflection.
:::

## A setup that matches nothing

If a setup implements none of the supported targets for the builder you call it on, `builder.Setup` throws an `InvalidOperationException` that names the setup type and the builder type. The same happens when you pass a setup to `app.Setup` that does not implement `ISetup<TApplication>` (or one for a base type of it).

```csharp
// MapEndpoints implements only ISetup<WebApplication>
builder.Setup(new MapEndpoints());       // throws InvalidOperationException
```

Setups for the built app belong to `app.Setup`.

## Trimming and Native AOT

Dispatch is plain pattern matching (`setup is ISetup<IServiceCollection>` and so on). Terp uses no reflection and no assembly scanning, so explicit setups are safe for trimming and Native AOT. The repository publishes `src/Terp.Hosting.Example` with Native AOT without trim or AOT warnings.

```powershell
dotnet publish src/Terp.Hosting.Example -c Release -r win-x64
```

## Related pages

- [Setups](/hosting/setups) covers writing and testing setups.
- [Startup gates](/hosting/gates) covers readiness checks that run before the host reports started.
