## Context

Terp's `ISetup` / `ISetup<T>` system dispatches setups to builder sub-targets (configuration, logging, services, metrics, builder type) or application targets via pure pattern matching — reflection-free and AOT-safe. The current extension methods `Setup<TSetup>()` on `IHostApplicationBuilder` and `IHost` erase the concrete receiver type (C# cannot infer one type parameter while another is explicit), causing type-loss that cascades through the chain: `WebApplicationBuilder` → `IHostApplicationBuilder` → `Build()` returns `IHost` → `ISetup<WebApplication>` dispatch fails.

The existing files: `SetupExtensions.cs` (builder-side, 3 overloads), `ApplicationSetupExtensions.cs` (app-side, 3 overloads), `ISetup.cs`, `ISetup.TTarget.cs`, `SetupContext.cs`. Only the extension method classes change; the interfaces and context stay.

## Goals / Non-Goals

**Goals:**
- Preserve the concrete receiver type across any number of chained setup calls
- Single-generic `Add<TSetup>()` — no need to spell the target type
- Zero-allocation wrapper (`readonly struct`)
- Same dispatch logic (pattern matching, same target order, same exceptions)
- Remain reflection-free, trim-safe, AOT-compatible
- No new dependencies

**Non-Goals:**
- Deferred/batched execution — each `Add` applies immediately
- Discovery/auto-scan of setups — setups are listed explicitly
- Builder-type-specific overloads (no ASP.NET Core reference)
- Changing `ISetup`, `ISetup<T>`, or `SetupContext`

## Decisions

### 1. `readonly struct SetupChain<TTarget>` as the wrapper

The chain wraps the target and returns itself from every `Add` call, preserving `TTarget` through the chain. A `readonly struct` avoids heap allocation — the chain lives on the stack and is effectively free.

**Alternative considered:** A class-based wrapper (like Servus' `AppBuilder`). Rejected because it hides the real builder from the consumer, requires its own `Build()`/`Run()` methods, and allocates.

### 2. Immediate application on every `Add`

Each `Add<TSetup>()` / `Add(ISetup)` applies the setup to the target right away and returns the chain. No `Apply()` needed. This matches the current behavior where `Setup()` runs immediately, and avoids silent bugs from forgotten finalization.

**Alternative considered:** Deferred execution with `Apply()`. Rejected because forgetting `Apply()` causes silent no-ops, and an analyzer to catch it is disproportionate effort.

### 3. Two entry points: `Setups()` on builder and `Setups()` on app

- `Setups<TBuilder>(this TBuilder)` where `TBuilder : IHostApplicationBuilder` → `SetupChain<TBuilder>` (builder sub-target dispatch: config, logging, services, metrics, builder)
- `Setups<TApplication>(this TApplication)` where `TApplication : IHost` → `SetupChain<TApplication>` (application target dispatch: single `ISetup<TApplication>`)

Both infer `TTarget` from the receiver — the key insight that makes single-generic `Add<TSetup>()` work without type-loss.

### 4. Dispatch logic stays in SetupChain, differentiated by constraint

`SetupChain<TTarget>` needs different dispatch for builders (multi-target: config/logging/services/metrics/builder) vs apps (single-target). The struct cannot have two implementations for different constraints.

**Approach:** Two internal static dispatch methods in a `SetupDispatch` helper class — `ApplyToBuilder<TBuilder>(TBuilder, ISetup)` and `ApplyToApplication<TApplication>(TApplication, ISetup)`. The `Setups()` entry points construct the chain with a delegate pointing to the right dispatch. The delegate is a `static` method reference — no closure allocation.

**Alternative considered:** Two separate struct types (`BuilderSetupChain<T>` / `AppSetupChain<T>`). Simpler but doubles the public API surface. A single `SetupChain<T>` with an internal dispatch delegate keeps the consumer API minimal.

### 5. File layout

| File | Content |
|------|---------|
| `SetupChain.cs` | `readonly struct SetupChain<TTarget>` with `Add<TSetup>()` and `Add(ISetup)` |
| `SetupChainExtensions.cs` | `Setups()` entry points for builder and app |
| `SetupExtensions.cs` | **Deleted** |
| `ApplicationSetupExtensions.cs` | **Deleted** |

All in `Terp.Hosting` namespace, `Terp` package.

## Risks / Trade-offs

- **[Breaking change]** → Major version bump via `feat!:` commit. Consumer migration is mechanical: `builder.Setup(new X()).Setup(new Y())` → `builder.Setups().Add<X>().Add<Y>()` or `builder.Setups().Add(new X()).Add(new Y())`.
- **[Delegate field in struct]** → The dispatch delegate adds 8 bytes to the struct (one pointer). Acceptable — still no heap allocation. The delegate targets a static method, so no closure.
- **[Single struct, two behaviors]** → The struct's `Add` method delegates to either builder or app dispatch. A wrong combination (builder dispatch on an app target) is prevented by the `Setups()` entry points being constrained, not by the struct itself. Internal-only construction mitigates this.

## Open Questions

_None — design is straightforward given the decisions above._
