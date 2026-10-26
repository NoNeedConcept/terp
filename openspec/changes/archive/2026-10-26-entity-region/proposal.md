## Why

Terp.Akka has actor resolving, registry and backoff supervisors but no child-per-entity routing.
The pattern (parent actor routes messages to per-entity children by ID, passivates idle ones) is
the most common Akka actor topology after plain parent-child. Servus.Akka had `LocalEntityRegionActor`
but its implementation has structural issues: global stash blocking the whole region during any
passivation, fire-and-forget persistence errors, no graceful drain, hardcoded restart limits,
and an `IEntityIdStore` that duplicates information already in Akka.Persistence.

Additionally, Terp has no async synchronization primitive. `AsyncLock` (scoped `SemaphoreSlim`)
is a tiny, common need in host and actor code that keeps getting reimplemented.

Both additions are **public API**, **additive** (no breaking changes), conventional commit type `feat`.

## What Changes

**Terp.Akka** (`feat(akka)`):
- `EntityRegion<TKey>` — sealed parent actor, routes by entity ID, spawns children lazily on first message
- `EntityRegionOptions` — immutable record: passivation timeout, max restarts, max entities, passivation interval
- `IEntityMessageExtractor` — extract entity ID from message; compatible shape with Akka's `IMessageExtractor` for future cluster-sharding migration
- `IWithEntityId` — marker interface for self-routing messages
- `WithEntityRegion<TKey>()` extension on `AkkaConfigurationBuilder` — single overload with options action
- Per-entity message buffer during passivation (not global stash)
- Graceful drain on shutdown
- Metrics via `System.Diagnostics.Metrics` (active entities, passivations, restarts)
- No `IEntityIdStore` — entities spawn on demand, persistent actors recover from journal

**Terp** (`feat`):
- `AsyncLock` — sealed, struct-disposable scope over `SemaphoreSlim(1,1)` for `using await`

## Capabilities

### New Capabilities
- `entity-region`: Child-per-entity parent actor with passivation, restart limits, metrics and graceful drain
- `async-lock`: Scoped async mutual exclusion primitive

### Modified Capabilities

_(none)_

## Impact

- **Terp.Akka**: new public types in a new `Terp.Akka.Entities` namespace (or root, TBD in design)
- **Terp**: one new type in `Terp.Threading` namespace
- **Dependencies**: no new NuGet packages needed
- **API snapshots**: `Terp.API.Tests` verified files need updating for both packages
- **Docs**: new pages for entity region and async lock
