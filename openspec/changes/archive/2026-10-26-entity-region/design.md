## Context

Terp.Akka provides actor resolving (`ResolveChildActor`), registry lookups (`GetActor<T>`),
and backoff supervisors (`BackoffOnFailure`/`BackoffOnStop`) but has no child-per-entity routing.
The pattern is the most common Akka topology after plain parent-child: a parent actor that receives
messages, extracts an entity ID, and forwards to a per-entity child — spawning it lazily on first message.

Servus.Akka had `LocalEntityRegionActor` but with structural flaws (global stash, `IEntityIdStore` that
duplicates persistence, hardcoded limits, no drain). This design starts fresh.

Terp also lacks a basic async synchronization primitive. `AsyncLock` fills that gap.

Both additions are trim/AOT-safe: no reflection, no code generation, sealed types only.
No new NuGet dependencies are needed.

## Goals / Non-Goals

**Goals:**
- Provide a child-per-entity parent actor with lazy spawning, idle passivation, and restart limiting
- Make the message extractor shape compatible with Akka's `IMessageExtractor` so users can migrate to cluster sharding later
- Emit `System.Diagnostics.Metrics` for observability (active entities, passivations, restarts)
- Graceful drain on shutdown: stop spawning, let in-flight work complete
- Per-entity message buffering during passivation (not global stash)
- Provide `AsyncLock` as a scoped `SemaphoreSlim(1,1)` wrapper
- Register the entity region via a single `WithEntityRegion<TKey>()` extension on `AkkaConfigurationBuilder`

**Non-Goals:**
- No `IEntityIdStore` or entity pre-loading — entities spawn on demand, persistent actors recover from journal
- No cluster sharding — this is local-only; users migrate by switching to `WithShardRegion` when ready
- No custom supervision strategies beyond restart counting — use Akka's built-in supervision
- No distributed pub-sub or entity-to-entity messaging patterns
- `AsyncLock` is not reentrant and does not support reader-writer semantics

## Decisions

### 1. Entity region as a `ReceiveActor` with `Become` states

**Choice:** `EntityRegionActor` is a sealed `ReceiveActor` with two states: `Active` and `Draining`.

**Why:** `ReceiveActor` is Terp.Akka's base for all non-persistent actors (consistent with `ActorSystemSetup`
registrations). `Become` cleanly separates the active routing state from the shutdown drain state.
`UntypedActor` would work but lacks the typed `Receive<T>()` registration that makes message handling explicit.

### 2. `IEntityMessageExtractor` compatible with `IMessageExtractor`

**Choice:** Define `IEntityMessageExtractor` with the same method signatures as Akka's `IMessageExtractor`
(`EntityId(object)`, `EntityMessage(object)`) but as a Terp-owned interface. Provide a default implementation
`EntityIdExtractor` that casts to `IWithEntityId`.

**Why:** Users write one extractor that works for local entity regions today and cluster sharding tomorrow.
We don't reference `Akka.Cluster.Sharding` (dependency-light), but the shape is identical so wrapping
is trivial. `ShardId` is not needed for local regions and returns `string.Empty`.

**Alternative considered:** Delegate-based extraction (`Func<object, string?>`). Simpler but loses the
`EntityMessage` unwrap step and isn't compatible with `IMessageExtractor`.

### 3. Per-entity buffer using `Dictionary<string, Queue<object>>`

**Choice:** When an entity is passivating (between `Context.Stop` and `Terminated`), messages for that
entity are buffered in a per-entity queue. On `Terminated`, the entity is removed and if the buffer is
non-empty, a new entity is spawned and the buffer is drained.

**Why:** Servus used a global `IStash` which blocked ALL entities when any one was passivating. Per-entity
buffers keep the region responsive. The buffer is bounded by `MaxBufferedMessagesPerEntity` (default 1000)
— overflow messages are sent to `DeadLetters` with a warning log.

### 4. `EntityRegionOptions` as an immutable record

**Choice:**
```csharp
public sealed record EntityRegionOptions
{
    public TimeSpan PassivationTimeout { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan PassivationInterval { get; init; } = TimeSpan.FromMinutes(1);
    public int MaxRestarts { get; init; } = 3;
    public TimeSpan RestartWindow { get; init; } = TimeSpan.FromMinutes(1);
    public int MaxEntities { get; init; } = 0; // 0 = unlimited
    public int MaxBufferedMessagesPerEntity { get; init; } = 1000;
}
```

**Why:** Immutable record follows Terp convention (DTOs are records with `init`). All knobs are configurable
with sensible defaults. `RestartWindow` + `MaxRestarts` together define the restart policy (3 failures in
1 minute = stop). `MaxEntities` caps memory; 0 = no limit.

### 5. Registration via `WithEntityRegion<TKey>`

**Choice:** Single extension method on `AkkaConfigurationBuilder`:
```csharp
public static AkkaConfigurationBuilder WithEntityRegion<TKey>(
    this AkkaConfigurationBuilder builder,
    Func<DependencyResolver, Props> entityPropsFactory,
    Action<EntityRegionOptions>? configure = null,
    IEntityMessageExtractor? extractor = null)
```

**Why:** Follows the `WithActors` pattern from Akka.Hosting. `TKey` is the registry key (same as
`GetActor<TKey>()`). `entityPropsFactory` receives the `DependencyResolver` so entities can use DI.
Default extractor is `EntityIdExtractor` (casts to `IWithEntityId`).

### 6. Metrics via `System.Diagnostics.Metrics`

**Choice:** A static `Meter` named `Terp.Akka.EntityRegion` with:
- `entity_region.active_entities` (UpDownCounter) — current live entities
- `entity_region.passivations` (Counter) — total passivation events
- `entity_region.restarts` (Counter) — total restart events
- `entity_region.messages_buffered` (Counter) — messages buffered during passivation

All counters carry a `region` tag with the `TKey` type name.

**Why:** Follows `Terp.Diagnostics` patterns (`TimerScope`, `JobMetrics`). Uses BCL `Meter` directly,
no OpenTelemetry dependency. The `region` tag keeps cardinality low (one per registered region type).

### 7. `AsyncLock` in `Terp.Threading`

**Choice:**
```csharp
public sealed class AsyncLock
{
    public ValueTask<AsyncLockScope> AcquireAsync(CancellationToken ct = default);
}

public readonly struct AsyncLockScope : IDisposable { ... }
```

**Why:** Struct-based scope avoids allocation on release. `ValueTask` avoids allocation when the lock
is uncontended (common case). Follows the `TimerScope` pattern from `Terp.Diagnostics`.

### 8. No `ISetup<T>` targets affected

The entity region is registered inside `BuildSystem` of an `ActorSystemSetup`. `AsyncLock` is a
standalone utility. Neither requires changes to the setup dispatch.

## Risks / Trade-offs

**[Per-entity buffer memory]** → Each passivating entity can buffer up to `MaxBufferedMessagesPerEntity`
messages. Mitigation: configurable cap with overflow to `DeadLetters`; short passivation window (Stop is fast).

**[Restart counting without backoff]** → Entity restart after failure is immediate (no delay).
Mitigation: users combine with `BackoffOnFailure` on the entity props if they need backoff.
The region only counts restarts and stops spawning after `MaxRestarts` in `RestartWindow`.

**[No cluster migration path at runtime]** → Switching from `WithEntityRegion` to `WithShardRegion`
requires a code change and redeploy. Mitigation: extractor compatibility means the message contracts
and extractors don't change, only the registration call.

**[`AsyncLock` is not reentrant]** → Acquiring the same lock twice on one call chain deadlocks.
Mitigation: documented; `SemaphoreSlim(1,1)` has the same behavior, this is expected.
