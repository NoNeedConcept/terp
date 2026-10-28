## Why

Terp.Akka's `ActorSystemSetup.BuildSystem` is the single entry point for actor topology, but it forces the host to know every actor in every library. As Terp replaces Servus across projects (Njord, FunkArr), libraries need a way to declare their own actors without the host wiring them by hand. The `IActorRegistration` pattern (proven in Njord.Core) solves this: libraries own their actors, the host owns the composition.

Conventional commit type: **feat** · Public API: **additive** (non-breaking)

## What Changes

- New `IActorRegistration` interface in `Terp.Akka` — single method `Configure(AkkaConfigurationBuilder, IServiceProvider)`.
- New `AddActorRegistration<T>()` extension on `IServiceCollection` for DI convenience.
- New `ApplyRegistrations(IServiceProvider)` extension on `AkkaConfigurationBuilder` — iterates all registered `IActorRegistration` instances. Called explicitly by the host in `BuildSystem`, not auto-discovered.
- AGENTS.md update: document the pattern and adjust the "no contributor setups" statement (actor registrations are a scoped contributor pattern for Akka topology only, not a general setup contributor system).
- Documentation page for the actor registration pattern.

## Capabilities

### New Capabilities

- `actor-registration`: Interface, DI extension, and builder extension that let libraries register their actors into an `ActorSystemSetup` without the host knowing the actor types directly.

### Modified Capabilities

_(none — `entity-region` and `async-lock` are unaffected)_

## Impact

- **Package**: `Terp.Akka` only — no changes to `Terp`.
- **Public API**: Three new public types/extensions added; API snapshot tests will need updating.
- **Existing consumers**: No breaking change. `ActorSystemSetup.BuildSystem` stays abstract; `ApplyRegistrations` is opt-in.
- **Dependencies**: None added — uses `Microsoft.Extensions.DependencyInjection.Abstractions` (already a transitive dependency via Akka.Hosting).
