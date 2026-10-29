## Why

Terp.Akka has 14 source files flat in the project root, all in namespace `Terp.Akka`. The EntityRegion feature (6 files) and hosting setup (3 files) sit mixed in with general-purpose extensions. As the library grows, this flat layout will only get harder to navigate. The base library `Terp` already follows topic-based namespaces (`Terp.Hosting`, `Terp.Hosting.Gates`, `Terp.Diagnostics`); Terp.Akka should mirror that pattern. Since the package is pre-release (`0.0.0`), restructuring now costs zero breaking changes for consumers — waiting until after 1.0 would require a major version bump.

## What Changes

- **BREAKING** — Move `ActorSystemSetup`, `IActorRegistration`, `ActorRegistrationExtensions` into `Hosting/` subfolder → namespace `Terp.Akka.Hosting`.
- **BREAKING** — Move `EntityRegionActor`, `EntityRegionOptions`, `EntityRegionMetrics`, `EntityIdExtractor`, `EntityRegionExtensions` into `Entities/` subfolder → namespace `Terp.Akka.Entities`.
- `IWithEntityId` stays in root namespace `Terp.Akka` because consumer message records implement it and should not need `using Terp.Akka.Entities` in every message file.
- General-purpose extensions (`ContextExtensions`, `RegistryExtensions`, `ResolveExtensions`, `BackoffExtensions`, `AkkaOptionsExtensions`) remain in root — used everywhere.
- Update API snapshot tests to match the new namespace layout.
- Update test files and examples for the new `using` statements.

Conventional commit type: `refactor!:` (breaking namespace change).

## Capabilities

### New Capabilities

- `akka-hosting-namespace`: Introduce `Terp.Akka.Hosting` namespace for actor system setup and registration, mirroring `Terp.Hosting` from the base library.
- `akka-entities-namespace`: Introduce `Terp.Akka.Entities` namespace for the child-per-entity region pattern.

### Modified Capabilities

_(none — no spec-level behavior changes, only namespace reorganization)_

## Impact

- **Public API**: Breaking namespace change for 8 types. Consumers must add `using Terp.Akka.Hosting;` and/or `using Terp.Akka.Entities;`.
- **Affected code**: `src/Terp.Akka/`, `src/Terp.Akka.Tests/`, `src/Terp.Akka.Example/`, `src/Terp.API.Tests/`.
- **No new dependencies**, no runtime behavior change — purely structural.
- **Trim/AOT**: No impact — namespaces are a compile-time concept.
