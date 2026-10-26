---
title: Akka
description: What Terp.Akka adds on top of Akka.Hosting, and how its pieces fit together.
---

# Akka

`Terp.Akka` is a small package of helpers for Akka.NET applications that use Akka.Hosting. It references `Terp` and `Akka.Hosting`.

```bash
dotnet add package Terp.Akka
```

```csharp
using Terp.Akka;
```

## What it adds

| Piece | Purpose | Page |
|-------|---------|------|
| `ActorSystemSetup` | an `ISetup<IServiceCollection>` that owns the one actor system of a host | [Actor system](/akka/actor-system) |
| `ResolveActor`, `ResolveChildActor` | create actors whose constructor dependencies come from DI | [Resolve actors](/akka/resolve) |
| `GetActor`, `TryGetActor`, `GetActorAsync`, `GetRegistry` | look up actors in the Akka.Hosting registry | [Registry and context](/akka/registry) |
| `GetChild`, `ChildTell`, `ChildForward` | find and message children of an actor context | [Registry and context](/akka/registry) |
| `Match` on `Option<T>` | pattern-match Akka's `Option<T>` | [Registry and context](/akka/registry) |
| `IWithEntityId` | marker for types that carry an entity id | [Registry and context](/akka/registry) |
| `BackoffOnFailure`, `BackoffOnStop` | wrap `Props` in a `BackoffSupervisor` with defaults | [Backoff helpers](/akka/backoff) |
| `EntityRegionActor`, `WithEntityRegion` | route messages to per-entity children with passivation, restart limits and metrics | [Entity region](/akka/entity-region) |
| `Passivate` | entity-initiated passivation, compatible with Akka Cluster Sharding | [Entity region](/akka/entity-region) |

## A host with an actor system

You derive one class from `ActorSystemSetup`, then apply it like any other setup. This is `src/Terp.Akka.Example`:

```csharp
using Akka.Hosting;
using Terp.Akka;

internal sealed class ExampleActorSystem : ActorSystemSetup
{
    protected override string ActorSystemName => "terp-example";

    protected override void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider serviceProvider)
    {
        builder
            .ConfigureLoggers(l =>
            {
                l.ClearLoggers();
                l.AddLoggerFactory();   // route Akka logs through Microsoft.Extensions.Logging
            })
            .WithActors((system, registry, resolver) =>
                registry.Register<GreeterActor>(system.ActorOf(resolver.Props<GreeterActor>(), "greeter")));
    }
}
```

```csharp
using Terp.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IGreeter, Greeter>();   // injected into the actor
builder.Setup(new ExampleActorSystem());
await builder.Build().RunAsync();
```

Akka.Hosting starts the actor system with the host and stops it when the host stops.

::: info Plain Akka.Hosting stays available
Terp.Akka does not hide Akka.Hosting. Inside `BuildSystem` you use the normal `AkkaConfigurationBuilder` API: remoting, persistence, sharding and so on.
:::

## Not included

Terp.Akka has no contributor setups, no cluster or sharding abstraction and no actor base classes. Actors are registered in `BuildSystem`. The [entity region](/akka/entity-region) provides local child-per-entity routing with the same passivation protocol as Akka Cluster Sharding, so you can migrate later.
