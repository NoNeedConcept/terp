## Context

Terp.Akka is a single NuGet package with 14 source files, all in `namespace Terp.Akka`, all in the project root. The base library `Terp` already uses topic-based namespaces (`Terp.Hosting`, `Terp.Hosting.Gates`, `Terp.Diagnostics`, `Terp.Threading`). The package is pre-release (`0.0.0`), so namespace changes are free.

The project convention is: namespace matches folder (IDE0130, enforced by `.editorconfig`).

## Goals / Non-Goals

**Goals:**

- Organize Terp.Akka into topic-based subfolders/namespaces mirroring the Terp pattern.
- Keep consumer ergonomics high — general-purpose extensions stay in root namespace.
- Establish namespace structure that supports future growth without breaking changes.

**Non-Goals:**

- No behavior changes, no new types, no API additions.
- No new NuGet packages — this stays one package with multiple namespaces.
- No changes to the `Terp` base library.

## Decisions

### 1. Three namespaces: root, Hosting, Entities

**Choice**: `Terp.Akka` (root), `Terp.Akka.Hosting`, `Terp.Akka.Entities`.

**Why**: Mirrors `Terp.Hosting` / `Terp.Hosting.Gates` from the base library. Each namespace represents a distinct consumer concern: general actor helpers vs. system setup vs. entity regions.

**Alternative considered**: Only `Entities` subfolder, keep setup in root. Rejected because `ActorSystemSetup` + `IActorRegistration` are a clear cohesive group and the `Hosting` namespace aligns with the base lib pattern.

### 2. `IWithEntityId` stays in root

**Choice**: `IWithEntityId` remains in `Terp.Akka` despite being part of the entity concept.

**Why**: Consumer message records implement this interface. Requiring `using Terp.Akka.Entities` in every message file would be hostile — messages are defined project-wide, entity region setup is done once.

### 3. Extensions stay in root

**Choice**: `ContextExtensions`, `RegistryExtensions`, `ResolveExtensions`, `BackoffExtensions`, `AkkaOptionsExtensions` stay in `Terp.Akka`.

**Why**: These are extension methods on core Akka types (`IActorContext`, `ActorRegistry`, `Props`, `Option<T>`) used throughout consumer code. A separate `Terp.Akka.Extensions` namespace would add a `using` to nearly every file — all cost, no benefit.

### 4. File moves via folder + namespace change

**Choice**: Move files to subfolders and update namespace declarations. The convention `namespace = folder` (IDE0130) handles the rest.

**Why**: No special tooling needed. `dotnet format` will flag any namespace/folder mismatches.

## Risks / Trade-offs

- **[Risk] Internal references between namespaces** → Files in `Entities/` that reference types from `Hosting/` (e.g., `EntityRegionExtensions` uses `AkkaConfigurationBuilder`) will need cross-namespace `using` statements. This is expected and mirrors how `Terp.Hosting.Gates` references `Terp.Hosting` types. → Mitigation: natural, no action needed.
- **[Risk] API snapshot diff is large** → The Verify snapshots will show every moved type as removed+added under new namespace. → Mitigation: Review the `.received.` files carefully, then rename to `.verified.`.
- **[Trade-off] More `using` statements for consumers** → Consumers go from 1 `using` to up to 3. Acceptable because each namespace serves a distinct purpose and consumers rarely need all three in the same file.
