## Why

The current `Setup<TSetup>()` single-generic extension methods on both `IHostApplicationBuilder` and `IHost` erase the concrete receiver type. On the builder side this means `WebApplicationBuilder` becomes `IHostApplicationBuilder`, so `builder.Build()` returns `IHost` instead of `WebApplication` — breaking any downstream `ISetup<WebApplication>` dispatch. A single call to the single-generic form poisons the entire chain. The two-generic form (`Setup<TBuilder, TSetup>()`) works but is verbose. C# cannot infer one type parameter while another is explicit, so no library-level fix can make the single-generic preserve types. This is a design-level problem that requires a new API shape.

## What Changes

- **BREAKING**: Remove all six `Setup` extension methods from `SetupExtensions` and `ApplicationSetupExtensions` (instance, single-generic, two-generic on both builder and app sides).
- **feat**: Introduce `SetupChain<TTarget>` — a `readonly struct` wrapper that preserves the concrete target type across chained `.Add<TSetup>()` / `.Add(ISetup)` calls. Each `Add` applies the setup immediately (no deferred `Apply()`).
- **feat**: Add `Setups()` extension methods on `IHostApplicationBuilder` (builder side) and `IHost` (app side) that return `SetupChain<TTarget>` with `TTarget` inferred from the receiver.
- `ISetup`, `ISetup<T>`, and `SetupContext` are unchanged. The dispatch logic (pattern-matching against supported targets) moves into `SetupChain<TTarget>`.
- Commit type: `feat!:` (breaking public API change → major version bump).

## Capabilities

### New Capabilities
- `setup-chain`: The `SetupChain<T>` readonly struct, `Setups()` entry-point extensions, `Add<TSetup>()`/`Add(ISetup)` methods, and the internal dispatch that applies setups to builder sub-targets (configuration, logging, services, metrics, builder) or application targets.

### Modified Capabilities
_(none — no existing specs cover the setup extensions being removed)_

## Impact

- **Public API**: Breaking change in `Terp` package. `SetupExtensions` and `ApplicationSetupExtensions` lose all `Setup` methods; consumers must migrate to `builder.Setups().Add<T>()` / `app.Setups().Add<T>()`.
- **Terp.Akka**: `ActorSystemSetup` implements `ISetup<IServiceCollection>` — unaffected, but example/test call sites need migration.
- **API snapshot tests**: `Terp.API.Tests` verified snapshots will change (removed methods, new types).
- **Examples**: `Terp.Example`, `Terp.Hosting.Example`, `Terp.Akka.Example` call sites need migration.
- **AOT**: `SetupChain<T>` is a readonly struct with the same pattern-match dispatch — remains reflection-free and trim-safe.
- **Dependencies**: None added.
