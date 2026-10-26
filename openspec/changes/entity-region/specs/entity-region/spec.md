## ADDED Requirements

### Requirement: Entity region routes messages to per-entity child actors

The `EntityRegionActor` in package `Terp.Akka`, namespace `Terp.Akka`, SHALL receive messages,
extract an entity ID via `IEntityMessageExtractor.EntityId(object)`, and forward the unwrapped
message (`IEntityMessageExtractor.EntityMessage(object)`) to a child actor named by that entity ID.

If the child does not exist, the region SHALL spawn it using the `Props` from the configured factory
and then forward the message. Messages where `EntityId` returns `null` SHALL be sent to `Unhandled`.

```csharp
// Registration in ActorSystemSetup.BuildSystem:
builder.WithEntityRegion<OrderActor>(
    resolver => resolver.Props<OrderActor>(),
    options => options with { PassivationTimeout = TimeSpan.FromMinutes(5) });
```

#### Scenario: First message for an entity spawns the child

- **WHEN** the region receives a message with entity ID `"order-42"` and no child named `"order-42"` exists
- **THEN** the region creates a child actor named `"order-42"` using the configured props factory and forwards the unwrapped message to it

#### Scenario: Subsequent messages route to existing child

- **WHEN** the region receives a message with entity ID `"order-42"` and a child named `"order-42"` already exists
- **THEN** the region forwards the unwrapped message to the existing child without spawning a new one

#### Scenario: Message with null entity ID is unhandled

- **WHEN** the region receives a message where `EntityId` returns `null`
- **THEN** the message is passed to `Unhandled`

### Requirement: Default extractor uses IWithEntityId

The default `IEntityMessageExtractor` implementation (`EntityIdExtractor`) SHALL cast the message
to `IWithEntityId` and return `EntityId`. If the message does not implement `IWithEntityId`,
`EntityId` SHALL return `null`. `EntityMessage` SHALL return the message unchanged.

```csharp
public interface IWithEntityId
{
    string EntityId { get; }
}

public interface IEntityMessageExtractor
{
    string? EntityId(object message);
    object EntityMessage(object message);
}
```

#### Scenario: Message implements IWithEntityId

- **WHEN** `EntityIdExtractor.EntityId` receives a message implementing `IWithEntityId` with `EntityId = "abc"`
- **THEN** it returns `"abc"`

#### Scenario: Message does not implement IWithEntityId

- **WHEN** `EntityIdExtractor.EntityId` receives a message that does not implement `IWithEntityId`
- **THEN** it returns `null`

### Requirement: Idle entities are passivated

The region SHALL periodically check entity activity (every `PassivationInterval`) and stop entities
that have not received a message within `PassivationTimeout`. The activity timestamp is updated
on every message routed to that entity. Passivation uses `Context.Stop`.

```csharp
public sealed record EntityRegionOptions
{
    public TimeSpan PassivationTimeout { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan PassivationInterval { get; init; } = TimeSpan.FromMinutes(1);
}
```

#### Scenario: Entity idle beyond timeout is stopped

- **WHEN** an entity has not received a message for longer than `PassivationTimeout` and the passivation timer fires
- **THEN** the region calls `Context.Stop` on that entity's `IActorRef`

#### Scenario: Active entity is not passivated

- **WHEN** an entity received a message within the last `PassivationTimeout` window and the passivation timer fires
- **THEN** the entity is not stopped

### Requirement: Messages for passivating entities are buffered per-entity

When an entity is being stopped (between `Context.Stop` and `Terminated`), messages for that entity
SHALL be buffered in a per-entity queue. When the `Terminated` message arrives, the entity is removed.
If the buffer for that entity is non-empty, a new entity with the same ID is spawned and buffered
messages are delivered in order. The buffer per entity is capped at `MaxBufferedMessagesPerEntity`;
overflow messages are sent to `DeadLetters`.

#### Scenario: Message arrives during entity passivation

- **WHEN** entity `"order-42"` is being stopped and a message arrives for `"order-42"`
- **THEN** the message is buffered, not dropped and not sent to the stopping actor

#### Scenario: Buffered messages delivered after passivation completes

- **WHEN** entity `"order-42"` has terminated and 3 messages are buffered for it
- **THEN** a new entity `"order-42"` is spawned and all 3 messages are forwarded in order

#### Scenario: Messages for other entities are not affected

- **WHEN** entity `"order-42"` is passivating and a message arrives for `"order-99"`
- **THEN** the message for `"order-99"` is routed normally (not buffered)

#### Scenario: Buffer overflow sends to DeadLetters

- **WHEN** the buffer for entity `"order-42"` reaches `MaxBufferedMessagesPerEntity`
- **THEN** further messages for `"order-42"` are sent to `DeadLetters` with a warning log

### Requirement: Entities that restart too often are stopped permanently

The region SHALL track restart counts per entity within a sliding `RestartWindow`. When an entity's
`Terminated` message arrives (without an explicit passivation) and its restart count within the window
reaches `MaxRestarts`, the region SHALL NOT respawn that entity. Further messages for that entity
are sent to `DeadLetters` with an error log.

```csharp
public sealed record EntityRegionOptions
{
    public int MaxRestarts { get; init; } = 3;
    public TimeSpan RestartWindow { get; init; } = TimeSpan.FromMinutes(1);
}
```

#### Scenario: Entity restarted within limit

- **WHEN** entity `"order-42"` terminates unexpectedly for the 2nd time within `RestartWindow` and `MaxRestarts = 3`
- **THEN** the entity is respawned when the next message arrives

#### Scenario: Entity exceeds restart limit

- **WHEN** entity `"order-42"` terminates unexpectedly for the 3rd time within `RestartWindow` and `MaxRestarts = 3`
- **THEN** the entity is NOT respawned and messages for it are sent to `DeadLetters`

### Requirement: Entity count can be capped

When `MaxEntities` is greater than 0, the region SHALL refuse to spawn new entities beyond that limit.
Messages for entities that cannot be spawned are sent to `DeadLetters` with a warning log.

#### Scenario: Entity limit not reached

- **WHEN** the region has 99 active entities and `MaxEntities = 100`
- **THEN** a message for a new entity spawns it normally

#### Scenario: Entity limit reached

- **WHEN** the region has 100 active entities and `MaxEntities = 100`
- **THEN** a message for a new (non-existing) entity is sent to `DeadLetters`

### Requirement: Graceful drain on shutdown

When the region actor is stopping (`PostStop`), it SHALL enter a draining state: no new entities
are spawned, and existing entities are allowed to finish processing their current message.
The region watches (`Context.Watch`) all children and waits for all `Terminated` messages
before completing shutdown.

#### Scenario: No new entities during drain

- **WHEN** the region is draining and a message arrives for a non-existing entity
- **THEN** the message is sent to `DeadLetters`

#### Scenario: Existing entities complete during drain

- **WHEN** the region enters drain and has 5 active entities
- **THEN** all 5 entities receive a `PoisonPill` and the region waits for their `Terminated` messages

### Requirement: Entity region emits metrics

The region SHALL report metrics via a `System.Diagnostics.Metrics.Meter` named `Terp.Akka.EntityRegion`:

| Instrument | Type | Description |
|------------|------|-------------|
| `entity_region.active_entities` | UpDownCounter&lt;int&gt; | Current live entity count |
| `entity_region.passivations` | Counter&lt;long&gt; | Total passivation events |
| `entity_region.restarts` | Counter&lt;long&gt; | Total unexpected restart events |
| `entity_region.messages_buffered` | Counter&lt;long&gt; | Messages buffered during passivation |

All instruments carry a tag `region` with the `TKey` type name (e.g. `"OrderActor"`).

#### Scenario: Entity spawned increments active counter

- **WHEN** a new entity is spawned
- **THEN** `entity_region.active_entities` is incremented by 1

#### Scenario: Entity passivated decrements active counter and increments passivations

- **WHEN** an idle entity is passivated
- **THEN** `entity_region.active_entities` is decremented by 1 and `entity_region.passivations` is incremented by 1

### Requirement: Entity region is registered via WithEntityRegion

The extension method `WithEntityRegion<TKey>` on `AkkaConfigurationBuilder` SHALL register the
entity region actor in the `ActorRegistry` under `TKey`, using the provided props factory and options.

```csharp
public static AkkaConfigurationBuilder WithEntityRegion<TKey>(
    this AkkaConfigurationBuilder builder,
    Func<DependencyResolver, Props> entityPropsFactory,
    Action<EntityRegionOptions>? configure = null,
    IEntityMessageExtractor? extractor = null)
```

#### Scenario: Region registered and resolvable

- **WHEN** `WithEntityRegion<OrderActor>(resolver => resolver.Props<OrderActor>())` is called
- **THEN** `system.GetActor<OrderActor>()` returns the region actor's `IActorRef`
