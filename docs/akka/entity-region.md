---
title: Entity region
description: A parent actor that routes messages to per-entity children by identity, with idle passivation, restart limits and metrics.
---

# Entity region

`EntityRegionActor` in `Terp.Akka` is a parent actor that routes messages to per-entity child actors by identity. Entities are spawned lazily on first message and passivated after being idle. It is a local alternative to Akka Cluster Sharding with the same passivation protocol, so you can migrate later without rewriting your message contracts.

```csharp
using Terp.Akka;
```

## Register a region

Call `WithEntityRegion<TKey>()` inside your `ActorSystemSetup.BuildSystem`. `TKey` is the registry key you use with `GetActor<TKey>()`.

```csharp
protected override void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider serviceProvider)
{
    builder.WithEntityRegion<OrderActor>(
        resolver => resolver.Props<OrderActor>(),
        options => options with { PassivationTimeout = TimeSpan.FromMinutes(5) });
}
```

The region actor is registered in the `ActorRegistry` under `TKey`:

```csharp
var region = system.GetActor<OrderActor>();
region.Tell(new PlaceOrder("order-42", items));
```

## Messages and extraction

Messages carry an entity identity via `IWithEntityId`:

```csharp
public sealed record PlaceOrder(string EntityId, List<Item> Items) : IWithEntityId;
public sealed record CancelOrder(string EntityId) : IWithEntityId;
```

The default `EntityIdExtractor` casts each message to `IWithEntityId` and returns its `EntityId`. Messages that do not implement `IWithEntityId` are passed to `Unhandled`.

For custom extraction, implement `IEntityMessageExtractor`:

```csharp
public sealed class OrderExtractor : IEntityMessageExtractor
{
    public string? EntityId(object message) => message switch
    {
        PlaceOrder cmd => cmd.OrderId,
        CancelOrder cmd => cmd.OrderId,
        _ => null
    };

    public object EntityMessage(object message) => message;
}
```

The interface shape matches Akka's `IMessageExtractor`, so your extractor works with cluster sharding too.

## Entity-initiated passivation

An entity can request its own passivation by sending `Passivate` to its parent. The region then buffers messages for that entity, sends the stop message back, and waits for `Terminated`:

```csharp
public sealed class OrderActor : ReceiveActor
{
    public OrderActor()
    {
        Receive<PlaceOrder>(Handle);
        Receive<PoisonPill>(_ => Context.Stop(Self));
    }

    private void Handle(PlaceOrder cmd)
    {
        // ... process order ...

        if (isDone)
        {
            Context.Parent.Tell(Passivate.Instance);
        }
    }
}
```

`Passivate.Instance` uses `PoisonPill` as the stop message. Pass a custom stop message with `new Passivate(new GracefulStop())`.

## Idle passivation

The region checks for idle entities every `PassivationInterval`. Entities that have not received a message within `PassivationTimeout` are stopped with `PoisonPill`. If a message arrives for an entity that is stopping, it is buffered and replayed after the entity terminates and a new one is spawned.

## Options

All options have defaults. Configure them with `with` expressions or a configure delegate:

| Option | Default | Meaning |
|--------|---------|---------|
| `PassivationTimeout` | 2 min | idle time before an entity is stopped |
| `PassivationInterval` | 1 min | how often the region checks for idle entities |
| `MaxRestarts` | 3 | unexpected terminations within `RestartWindow` before permanent stop |
| `RestartWindow` | 1 min | sliding window for counting restarts |
| `MaxEntities` | 0 (unlimited) | maximum number of live entities; overflow goes to `DeadLetters` |
| `MaxBufferedMessagesPerEntity` | 1000 | messages buffered during passivation; overflow goes to `DeadLetters` |

## Restart limits

When an entity terminates without passivation (a crash), the region counts it as a restart. After `MaxRestarts` unexpected terminations within `RestartWindow`, the entity is stopped permanently: messages for it go to `DeadLetters` with an error log.

Combine with `BackoffOnFailure` on the entity props if you want exponential backoff between restarts:

```csharp
builder.WithEntityRegion<OrderActor>(
    resolver => resolver.Props<OrderActor>().BackoffOnFailure("order"));
```

## Graceful drain

When the region actor stops, it enters a draining state: no new entities are spawned, existing entities receive `PoisonPill`, and the region waits for all `Terminated` messages before completing shutdown.

## Metrics

The region emits metrics via a `Meter` named `Terp.Akka.EntityRegion`. All instruments carry a `region` tag with the `TKey` type name.

| Instrument | Type | Description |
|------------|------|-------------|
| `entity_region.active_entities` | UpDownCounter | current live entity count |
| `entity_region.passivations` | Counter | total passivation events |
| `entity_region.restarts` | Counter | total unexpected restart events |
| `entity_region.messages_buffered` | Counter | messages buffered during passivation |

Subscribe to the meter in your OpenTelemetry setup:

```csharp
builder.Services.AddOpenTelemetry().WithMetrics(m => m.AddMeter("Terp.Akka.EntityRegion"));
```

## Migration to cluster sharding

`IEntityMessageExtractor` has the same method signatures as Akka's `IMessageExtractor`. When you move to a cluster, replace `WithEntityRegion<TKey>()` with `WithShardRegion<TKey>()` and adapt the extractor to also return a `ShardId`. Your messages and entity actors stay the same.

## Related

- [Actor system](/akka/actor-system) for setting up the host.
- [Backoff helpers](/akka/backoff) for wrapping entity props in a backoff supervisor.
- [Registry and context](/akka/registry) for `GetActor<TKey>()`.
