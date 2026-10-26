---
title: Registry and context
description: Look up registered actors, message children, match on Option and mark entity ids.
---

# Registry and context

This page covers the smaller helpers of `Terp.Akka`: registry lookups, child lookups on an `IActorContext`, `Option<T>` matching and `IWithEntityId`.

```csharp
using Akka.Actor;
using Akka.Util;
using Terp.Akka;
```

## Registry lookups

Akka.Hosting keeps registered actors in an `ActorRegistry`, keyed by a type. The extensions work on `ActorSystem` and on `IActorContext`, which uses `Context.System`.

```csharp
// registration, in ActorSystemSetup.BuildSystem
registry.Register<OrderManager>(system.ActorOf(resolver.Props<OrderManager>(), "orders"));

// lookup, inside an actor
var orders = Context.GetActor<OrderManager>();
```

| Method | If nothing is registered |
|--------|--------------------------|
| `GetActor<T>()` | throws `MissingActorRegistryEntryException` |
| `TryGetActor<T>(out actor)` | returns `false`; `actor` is `ActorRefs.Nobody` |
| `TryGetActor(Type key, out actor)` | returns `false`; `actor` is `ActorRefs.Nobody` |
| `GetActorAsync<T>()` | waits until the actor is registered; never throws for a missing entry, and never completes if the entry never appears |
| `GetRegistry()` (on `ActorSystem`) | returns the registry as `IReadOnlyActorRegistry` |

```csharp
if (Context.TryGetActor<OrderManager>(out var manager))
{
    manager.Tell(new Refresh());
}

var late = await Context.GetActorAsync<OrderManager>();
```

::: info Waiting forever
`GetActorAsync` completes when the entry appears. If it never does, the task never completes. Pass the task through a timeout such as `WaitAsync` when that matters.
:::

## Child lookups

Three extensions on `IActorContext` work with children by name.

`GetChild(name)` returns `Option<IActorRef>`, which is `None` when no child with that name exists.

`ChildTell(name, message)` sends the message to the child with the parent (`Self`) as sender. `ChildForward(name, message)` sends it with the sender of the message being processed, so the child can reply to the original sender. Both return `true` when the child exists and the message was sent, and `false` otherwise. A missing child is not an error and sends nothing.

```csharp
Receive<Cancel>(m =>
{
    if (!Context.ChildForward("worker", m))
    {
        Sender.Tell(new NotFound());
    }
});
```

## Matching on Option

Akka's `Option<T>` has no `Match`. `AkkaOptionsExtensions` adds two overloads. One returns a value, the other runs actions.

```csharp
var text = Context.GetChild("worker").Match(
    some: child => $"worker at {child.Path}",
    none: () => "no worker");

Context.GetChild("worker").Match(
    some: child => child.Tell(new Ping()),
    none: () => Context.GetLogger().Warning("no worker"));
```

Both branches are required and must not be null.

## IWithEntityId

`IWithEntityId` has one member, `string EntityId`. Implement it on messages or types that carry an entity identity.

```csharp
public sealed record AddItem(string EntityId, string Sku) : IWithEntityId;
```

Terp.Akka ships the interface only. A sharding message extractor or a router that reads `EntityId` is yours to write.
