---
title: Actor system
description: ActorSystemSetup registers the one actor system of a host and validates its name early.
---

# Actor system

`ActorSystemSetup` is an abstract class in `Terp.Akka` that implements `ISetup<IServiceCollection>`. It registers the actor system with Akka.Hosting (`services.AddAkka(name, BuildSystem)`) under the name you give it.

```csharp
using Akka.Hosting;
using Terp.Akka;
using Terp.Hosting;
```

## Write the setup

A subclass overrides two members:

```csharp
internal sealed class ShopActorSystem : ActorSystemSetup
{
    protected override string ActorSystemName => "shop";

    protected override void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider serviceProvider)
    {
        builder.WithActors((system, registry, resolver) =>
            registry.Register<OrderManager>(system.ActorOf(resolver.Props<OrderManager>(), "orders")));
    }
}
```

`ActorSystemName` is the name of the system. `BuildSystem(AkkaConfigurationBuilder, IServiceProvider)` configures it. Add your actors, persistence, remoting and other Akka.Hosting configuration there. The service provider is the application's provider.

Apply the setup like any other:

```csharp
builder.Setup<ShopActorSystem>();
```

The `Setup` method itself is not virtual. You customise only the name and `BuildSystem`. The service provider calls `BuildSystem` when it creates the actor system, not when you call `builder.Setup`, so it may resolve services that a later setup registers.

::: info Order
`BuildSystem` runs when the service provider creates the actor system, not when you call `builder.Setup`. A service that `BuildSystem` resolves can be registered by a setup that you apply later.
:::

## One actor system per host

A host has exactly one actor system. If the service collection already contains an `ActorSystem` registration, `Setup` throws an `InvalidOperationException`. This also catches two `ActorSystemSetup` classes and any earlier `AddAkka` call.

::: warning Why it throws
A second registration would replace the first without any message. Terp stops you instead. Register the whole system in a single `BuildSystem`.
:::

## Name validation

`Setup` validates the name before it registers anything, so a wrong name fails at startup and not later inside Akka. The name must

- not be null, empty or whitespace,
- start with an ASCII letter or digit,
- contain only ASCII letters, digits, `-` and `_`.

Otherwise `Setup` throws an `ArgumentException` that names the offending value. `"funkarr"`, `"terp-example"` and `"shop_2"` pass. `"my system"` and `"-shop"` do not.

## Related

- [Resolve actors](/akka/resolve) for creating actors with DI dependencies.
- [Registry and context](/akka/registry) for finding the actors you registered.
