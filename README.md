<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/st0o0/terp/main/docs/public/readme-header-dark.png">
    <img alt="terp" src="https://raw.githubusercontent.com/st0o0/terp/main/docs/public/readme-header-light.png">
  </picture>
</p>

# Terp

Terp is a pair of small .NET libraries for host setup, startup gates, metrics, backoff and Akka.NET.

[![Terp on NuGet](https://img.shields.io/nuget/v/Terp.svg?style=flat-square&label=Terp)](https://www.nuget.org/packages/Terp)
[![Terp downloads](https://img.shields.io/nuget/dt/Terp.svg?style=flat-square&label=downloads)](https://www.nuget.org/packages/Terp)
[![Terp.Akka on NuGet](https://img.shields.io/nuget/v/Terp.Akka.svg?style=flat-square&label=Terp.Akka)](https://www.nuget.org/packages/Terp.Akka)
[![Terp.Akka downloads](https://img.shields.io/nuget/dt/Terp.Akka.svg?style=flat-square&label=downloads)](https://www.nuget.org/packages/Terp.Akka)
[![License: MIT](https://img.shields.io/github/license/st0o0/terp.svg?style=flat-square)](https://github.com/st0o0/terp/blob/main/LICENSE)
[![CI](https://img.shields.io/github/actions/workflow/status/st0o0/terp/ci.yml?style=flat-square&label=CI)](https://github.com/st0o0/terp/actions/workflows/ci.yml)
[![Docs](https://img.shields.io/badge/docs-terp.noneedconcept.org-5C6B3E?style=flat-square)](https://terp.noneedconcept.org)

Terp is a project of [NoNeedConcept](https://github.com/NoNeedConcept).

A *terp* is a Frisian dwelling mound: the artificial hill that keeps a home dry and gives it a place to stand. Terp
follows the same idea. It is a small, solid foundation you build your applications on, and it stays out of your way.

## What you get

**Terp** depends on the Microsoft.Extensions abstractions only. It has no ASP.NET Core reference.

- Host setups: `ISetup<TTarget>` classes that each configure one concern of a host or web application builder.
- Startup gates: `IStartupGate` and `AddStartupGate` hold the host back until dependencies are ready.
- Metrics helpers: `TimerScope`, `JobMetrics` and `TerpHttpMetricsHandler` on top of `System.Diagnostics.Metrics`.
- Backoff: `Backoff.Create` returns a `BackoffPolicy` with exponential delays and optional jitter.
- Port finder: `PortFinder.FindFreeLocalPort` returns a free local TCP port.
- Date helpers: `DateTimeExtensions` such as `IsWorkday`, `IsWeekend`, `IsToday` and `IsBetween`.

**Terp.Akka** builds on `Terp` and Akka.Hosting.

- Single actor system setup: `ActorSystemSetup` owns the one actor system of a host.
- Actor resolve: `ResolveActor` and `ResolveChildActor` create actors with dependency injection.
- Registry: `GetActor<T>`, `TryGetActor` and `GetRegistry` look up registered actors.
- Backoff supervisor helpers: `BackoffOnFailure` and `BackoffOnStop` wrap `Props` in a backoff supervisor.
- Context helpers: `GetChild`, `ChildTell` and `ChildForward`.

## Documentation

The full documentation is at [terp.noneedconcept.org](https://terp.noneedconcept.org): a getting-started guide, the hosting model with setups and startup gates, and a page for each helper area and for `Terp.Akka`.
It also publishes [`llms.txt`](https://terp.noneedconcept.org/llms.txt) and [`llms-full.txt`](https://terp.noneedconcept.org/llms-full.txt) for use with coding assistants.

## Installation

```powershell
dotnet add package Terp
dotnet add package Terp.Akka
```

Or reference them in your project file and use the latest version of each:

```xml
<ItemGroup>
  <PackageReference Include="Terp" />
  <PackageReference Include="Terp.Akka" />
</ItemGroup>
```

Both packages target .NET 10. Each one is versioned and released on its own, and dependencies are declared as minimum
versions, so you update when you want to.

## Quick start

One setup class, one startup gate, one host:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terp.Hosting;
using Terp.Hosting.Gates;

var builder = Host.CreateApplicationBuilder(args);
builder.Setup(new WarmupSetup());         // runs immediately, returns the same builder type

using var host = builder.Build();
await host.RunAsync();                    // starts only after every gate is open

internal sealed class WarmupSetup : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        services.AddStartupGate("warmup", ct => Task.FromResult(true));   // replace with a real dependency check
    }
}
```

Gates run before any hosted service starts. Open gates are checked again with exponential backoff until they return
`true` or the host start token is cancelled. Raise `HostOptions.StartupTimeout` for slow dependencies.

With `Terp.Akka`, one `ActorSystemSetup` subclass describes the actor system:

```csharp
internal sealed class MyActorSystem : ActorSystemSetup
{
    protected override string ActorSystemName => "my-system";

    protected override void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider serviceProvider) =>
        builder.WithActors((system, registry, resolver) =>
            registry.Register<GreeterActor>(system.ActorOf(resolver.Props<GreeterActor>(), "greeter")));
}

builder.Setup(new MyActorSystem());   // one actor system per host
```

## How setups run

- A setup implements `ISetup<TTarget>` once per target: `IConfigurationManager`, `ILoggingBuilder`, `IServiceCollection`, `IMetricsBuilder`, `IHostApplicationBuilder` or the concrete builder, and `IHost` after `Build()`.
- Inside one setup class the order is fixed: configuration, logging, services, metrics, builder.
- Across setups, `builder.Setup(...)` runs immediately, so the call order is the run order. You list setups explicitly.
- Dispatch uses pattern matching and no reflection, so trimming and Native AOT work. The hosting example publishes as Native AOT without warnings.

## Samples

The demo apps need no external services and exit on their own.

| Project | Shows |
|---------|-------|
| [`Terp.Example`](https://github.com/st0o0/terp/tree/main/src/Terp.Example) | `TimerScope`, `JobMetrics`, `TerpHttpMetricsHandler`, `Backoff`, `PortFinder`, `DateTimeExtensions` |
| [`Terp.Hosting.Example`](https://github.com/st0o0/terp/tree/main/src/Terp.Hosting.Example) | One setup for every target, the fixed order, a startup gate, Native AOT publish |
| [`Terp.Akka.Example`](https://github.com/st0o0/terp/tree/main/src/Terp.Akka.Example) | `ActorSystemSetup`, actor registry and `BackoffOnFailure` |

```powershell
dotnet run --project src/Terp.Hosting.Example
```

## Contributing

Run these commands from the repository root. Always use the solution file.

```powershell
dotnet build src/Terp.slnx
dotnet test --solution Terp.slnx          # run from src/
dotnet format Terp.slnx --verify-no-changes
```

- Commits follow [Conventional Commits](https://www.conventionalcommits.org/); commitlint enforces them in CI.
- `src/Terp.API.Tests` snapshots the public API in `verify/*.verified.txt`. After an intentional API change, review the diff and approve the new snapshot. Breaking changes need a `feat!:` commit.
- Read [AGENTS.md](https://github.com/st0o0/terp/blob/main/AGENTS.md) for the code conventions.

## License

Terp is released under the [MIT license](https://github.com/st0o0/terp/blob/main/LICENSE).
