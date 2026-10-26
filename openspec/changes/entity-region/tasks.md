## 1. AsyncLock (Terp)

- [x] 1.1 Add `AsyncLockScope` readonly struct in `src/Terp/Threading/AsyncLockScope.cs`
- [x] 1.2 Add `AsyncLock` sealed class in `src/Terp/Threading/AsyncLock.cs`
- [x] 1.3 Write `AsyncLockTests` in `src/Terp.Tests/Threading/AsyncLockTests.cs` (uncontended, contended, cancellation, dispose)

## 2. Entity region types (Terp.Akka)

- [x] 2.1 Add `IEntityMessageExtractor` interface in `src/Terp.Akka/IEntityMessageExtractor.cs`
- [x] 2.2 Add `EntityIdExtractor` (default `IWithEntityId`-based extractor) in `src/Terp.Akka/EntityIdExtractor.cs`
- [x] 2.3 Add `EntityRegionOptions` record in `src/Terp.Akka/EntityRegionOptions.cs`
- [x] 2.4 Add `EntityRegionMetrics` static helper in `src/Terp.Akka/EntityRegionMetrics.cs` (Meter + instruments)

## 3. EntityRegionActor (Terp.Akka)

- [x] 3.1 Implement `EntityRegionActor` sealed ReceiveActor in `src/Terp.Akka/EntityRegionActor.cs` — message routing, lazy spawn, activity tracking
- [x] 3.2 Add passivation timer via IWithTimers and idle-entity cleanup logic
- [x] 3.3 Add per-entity message buffering during passivation (buffer + Terminated handler + re-spawn + drain buffer)
- [x] 3.4 Add restart counting with sliding window and permanent stop on max restarts
- [x] 3.5 Add MaxEntities cap enforcement
- [x] 3.6 Add graceful drain state (Become Draining, PoisonPill children, wait for Terminated)
- [x] 3.7 Wire metrics (active count, passivations, restarts, buffered messages)
- [x] 3.8 Add Passivate message class for entity-initiated passivation (Cluster Sharding compatible)

## 4. Registration extension

- [x] 4.1 Add `WithEntityRegion<TKey>()` extension on `AkkaConfigurationBuilder` in `src/Terp.Akka/EntityRegionExtensions.cs`

## 5. Tests (Terp.Akka)

- [x] 5.1 Write `EntityIdExtractorTests` — IWithEntityId routing, non-IWithEntityId returns null
- [x] 5.2 Write `EntityRegionActorTests` — spawn on first message, route to existing, null entity ID unhandled
- [x] 5.3 Write passivation tests — idle entity stopped, active entity kept, messages buffered and replayed
- [x] 5.4 Write restart limit tests — within limit respawns, exceeds limit sends to DeadLetters
- [x] 5.5 Write max-entities tests — under limit spawns, at limit rejects
- [x] 5.6 Write drain tests — no new entities in drain, existing entities receive PoisonPill
- [x] 5.7 Write metrics tests — verify counters increment on spawn, passivation, restart

## 6. API snapshots and format

- [x] 6.1 Update `Terp.API.Tests` verified snapshots for new public API in both packages
- [x] 6.2 Run `dotnet format Terp.slnx --verify-no-changes` and fix any violations
- [x] 6.3 Run full build with `-p:ContinuousIntegrationBuild=true` to verify CI parity

## 7. Documentation

- [x] 7.1 Add entity region docs page at `docs/akka/entity-region.md`
- [x] 7.2 Add async lock docs page at `docs/threading/index.md`
- [x] 7.3 Update `docs/.vitepress/config.ts` sidebar with new pages
- [x] 7.4 Update `AGENTS.md` namespace listing with new types
