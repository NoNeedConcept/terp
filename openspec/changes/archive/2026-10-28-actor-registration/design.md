## Context

`ActorSystemSetup.BuildSystem` is the single entry point for configuring actor topology. It works well for a standalone host, but when multiple libraries each bring their own actors (as in the Njord/FunkArr ecosystem), the host must inline every library's actor wiring. This couples the host to library internals.

The `IActorRegistration` pattern (proven in Njord.Core) decouples this: libraries declare actors via DI, the host dispatches them in `BuildSystem`. With Servus being replaced by Terp, this pattern belongs in `Terp.Akka`.

Current flow:
```
Host.BuildSystem(builder, provider)
  └─ builder.WithActors(...)          // host knows every actor
  └─ builder.WithEntityRegion<T>(...) // host knows every region
```

Target flow:
```
Library.Setup(IServiceCollection)
  └─ services.AddActorRegistration<MyActorRegistration>()

Host.BuildSystem(builder, provider)
  └─ builder.ApplyRegistrations(provider)  // host delegates to libraries
```

## Goals / Non-Goals

**Goals:**
- Let libraries register actors without the host knowing actor types
- Keep the pattern explicit and opt-in (no auto-discovery, no reflection)
- Stay trim-safe and AOT-compatible
- Zero new NuGet dependencies

**Non-Goals:**
- Ordering / priority between registrations (add later if needed, additively)
- Replacing `BuildSystem` — hosts can mix inline wiring and `ApplyRegistrations`
- A general contributor/plugin system for `ISetup<T>` targets
- Lifecycle hooks on registrations (start, stop, health)

## Decisions

### 1. Standalone interface, not `ISetup<AkkaConfigurationBuilder>`

**Decision:** New `IActorRegistration` interface with `Configure(AkkaConfigurationBuilder, IServiceProvider)`.

**Alternative considered:** Reuse `ISetup<AkkaConfigurationBuilder>` to fit into the existing setup dispatch system.

**Rationale:** `ISetup<T>` dispatch happens during `builder.Setup()` (service registration phase). Actor registrations run inside `BuildSystem`, which executes when the DI container resolves the `ActorSystem` — a different lifecycle phase entirely. Mixing these would confuse the phase model. A dedicated interface makes the timing explicit.

### 2. Extension method on `AkkaConfigurationBuilder`, not a base class change

**Decision:** `ApplyRegistrations` is a static extension method, not built into `ActorSystemSetup`.

**Alternative considered:** Have `ActorSystemSetup.Setup` automatically call `ApplyRegistrations` after `BuildSystem`.

**Rationale:** The host controls when registrations run relative to infrastructure setup (logging, persistence, clustering). Auto-dispatch would remove that control. The extension method lets the host place it exactly where it belongs in the builder chain.

### 3. `AddActorRegistration<T>()` convenience extension

**Decision:** Provide `IServiceCollection.AddActorRegistration<T>()` that calls `AddSingleton<IActorRegistration, T>()`.

**Rationale:** Reads better (`AddActorRegistration<BudgetActors>()` vs `AddSingleton<IActorRegistration, BudgetActors>()`), and centralizes the DI registration pattern so it can evolve (e.g., if ordering is added later, only this method changes).

**Constraint:** `T` must have a parameterless constructor (`where T : class, IActorRegistration, new()`). Registrations that need constructor injection use the `AddSingleton<IActorRegistration, T>()` overload directly — the convenience method is for the common case.

### 4. File placement

| Type | File | Namespace |
|------|------|-----------|
| `IActorRegistration` | `src/Terp.Akka/IActorRegistration.cs` | `Terp.Akka` |
| `ActorRegistrationExtensions` | `src/Terp.Akka/ActorRegistrationExtensions.cs` | `Terp.Akka` |

Both are top-level `Terp.Akka` namespace — this is a first-class concept, not a sub-namespace detail. The extensions class holds both `AddActorRegistration<T>` (on `IServiceCollection`) and `ApplyRegistrations` (on `AkkaConfigurationBuilder`).

### 5. Trim/AOT safety

No reflection. `AddSingleton<IActorRegistration, T>()` is a generic DI call (trim-safe). `GetServices<IActorRegistration>()` iterates the service collection (no reflection). `Configure` is a direct interface call. This matches the existing `ActorSystemSetup` pattern.

## Risks / Trade-offs

- **[Registration order is undefined]** → `GetServices<IActorRegistration>` returns registrations in DI insertion order, which is the order libraries are added to the host. This is deterministic but not explicitly controlled. Mitigation: document that order follows DI registration order; add explicit ordering later if needed (additive, non-breaking).

- **[Duplicate registrations]** → Nothing prevents registering the same `IActorRegistration` type twice. This would call `Configure` twice, which could fail (e.g., duplicate actor names). Mitigation: this is a host misconfiguration, same as calling `builder.WithActors` twice with the same actor name. No guard needed — Akka.Hosting itself will throw on duplicate registry keys.

- **[No validation that registrations were applied]** → A library could register an `IActorRegistration` but the host forgets to call `ApplyRegistrations`. The actors simply won't exist at runtime. Mitigation: this is the trade-off of explicit opt-in. A startup gate or health check in the library would catch it.

## Open Questions

_(none — ship minimal, iterate)_
