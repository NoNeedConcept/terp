<div align="center">

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/NoNeedConcept/terp/main/docs/public/readme-header-dark.png">
  <img alt="Terp" src="https://raw.githubusercontent.com/NoNeedConcept/terp/main/docs/public/readme-header-light.png">
</picture>

**Small foundation libraries for .NET**

[![Terp on NuGet](https://img.shields.io/nuget/v/Terp.svg?style=flat-square&label=Terp)](https://www.nuget.org/packages/Terp)
[![Terp.Akka on NuGet](https://img.shields.io/nuget/v/Terp.Akka.svg?style=flat-square&label=Terp.Akka)](https://www.nuget.org/packages/Terp.Akka)
[![Downloads](https://img.shields.io/nuget/dt/Terp.svg?style=flat-square)](https://www.nuget.org/packages/Terp)
[![License: MIT](https://img.shields.io/github/license/NoNeedConcept/terp.svg?style=flat-square)](https://github.com/NoNeedConcept/terp/blob/main/LICENSE)
[![CI](https://img.shields.io/github/actions/workflow/status/NoNeedConcept/terp/ci.yml?style=flat-square&label=CI)](https://github.com/NoNeedConcept/terp/actions/workflows/ci.yml)
[![Docs](https://img.shields.io/badge/docs-terp.noneedconcept.org-5C6B3E?style=flat-square)](https://terp.noneedconcept.org)

</div>

---

A *terp* is a Frisian dwelling mound: the artificial hill that keeps a home dry and gives it a place to stand.
Terp follows the same idea -- a small, solid foundation you build your .NET applications on, and it stays out of your way.

Terp is a project of [NoNeedConcept](https://github.com/NoNeedConcept).

## Packages

Both packages share one version and are released together.

| Package | What's inside | Depends on |
|---------|--------------|------------|
| **`Terp`** | Host setups (`ISetup<T>`), startup gates, metrics helpers (`TimerScope`, `TerpHttpMetricsHandler`), exponential backoff, port finder, async lock, date helpers | Microsoft.Extensions abstractions only |
| **`Terp.Akka`** | Actor system setup (`ActorSystemSetup`), actor resolving with DI, actor registry, backoff supervisors, context helpers, entity region actor | `Terp` + Akka.Hosting |

`Terp` has no ASP.NET Core reference and no concrete host dependency -- it works with any host.

## Installation

```bash
dotnet add package Terp
dotnet add package Terp.Akka   # only if you use Akka.NET
```

Both packages target .NET 10.

## Quick start

One setup class, one startup gate, one host:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terp.Hosting;
using Terp.Hosting.Gates;

var builder = Host.CreateApplicationBuilder(args);
builder.Setup(new WarmupSetup());

using var host = builder.Build();
await host.RunAsync();

internal sealed class WarmupSetup : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        services.AddStartupGate("warmup", ct => Task.FromResult(true));
    }
}
```

Gates hold the host back until all dependencies are ready. Open gates are checked with exponential backoff until they return `true`.

### Terp.Akka

One `ActorSystemSetup` subclass describes the actor system:

```csharp
internal sealed class MyActorSystem : ActorSystemSetup
{
    protected override string ActorSystemName => "my-system";

    protected override void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider serviceProvider) =>
        builder.WithActors((system, registry, resolver) =>
            registry.Register<GreeterActor>(system.ActorOf(resolver.Props<GreeterActor>(), "greeter")));
}

builder.Setup(new MyActorSystem());
```

## How setups run

- A setup implements `ISetup<TTarget>` for each target it configures: `IConfigurationManager`, `ILoggingBuilder`, `IServiceCollection`, `IMetricsBuilder`, `IHostApplicationBuilder`, or `IHost`.
- Inside one setup, targets run in a fixed order: configuration, logging, services, metrics, builder.
- Across setups, `builder.Setup(...)` runs immediately -- call order is run order.
- Dispatch uses pattern matching with no reflection. Trimming and Native AOT work out of the box.

## Samples

The demo apps need no external services and exit on their own.

| Project | Shows |
|---------|-------|
| [`Terp.Example`](https://github.com/NoNeedConcept/terp/tree/main/src/Terp.Example) | `TimerScope`, `TerpHttpMetricsHandler`, `Backoff`, `PortFinder`, `DateTimeExtensions` |
| [`Terp.Hosting.Example`](https://github.com/NoNeedConcept/terp/tree/main/src/Terp.Hosting.Example) | Setups for every target, startup gate, Native AOT publish |
| [`Terp.Akka.Example`](https://github.com/NoNeedConcept/terp/tree/main/src/Terp.Akka.Example) | `ActorSystemSetup`, actor registry, `BackoffOnFailure` |

```bash
dotnet run --project src/Terp.Hosting.Example
```

## Documentation

Full documentation at [terp.noneedconcept.org](https://terp.noneedconcept.org) including the getting-started guide, hosting model, and API reference.

Also publishes [`llms.txt`](https://terp.noneedconcept.org/llms.txt) and [`llms-full.txt`](https://terp.noneedconcept.org/llms-full.txt) for coding assistants.

## Contributing

```bash
dotnet build src/Terp.slnx
dotnet test --solution Terp.slnx          # run from src/
dotnet format Terp.slnx --verify-no-changes
```

- Commits follow [Conventional Commits](https://www.conventionalcommits.org/); commitlint enforces them in CI.
- `src/Terp.API.Tests` snapshots the public API. After an intentional change, review the diff and approve the new snapshot. Breaking changes need a `feat!:` commit.
- See [AGENTS.md](https://github.com/NoNeedConcept/terp/blob/main/AGENTS.md) for code conventions.

## License

Terp is released under the [MIT License](https://github.com/NoNeedConcept/terp/blob/main/LICENSE).
