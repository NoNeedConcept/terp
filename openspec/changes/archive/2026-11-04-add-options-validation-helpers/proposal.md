## Why

Terp (package `Terp`, public API, additive, `feat:`) gives consumers `ISetup<IServiceCollection>` to register services, but no help with options validation. Njord and FunkArr both validate options with hand-written `IValidateOptions<T>` classes (55 of them) that repeat the same failure-list boilerplate and re-type the config section prefix in every message. Inline `.Validate(...)` chains and per-project `AddNjordOptions` wrappers have grown alongside, so there is no single agreed way to validate options.

.NET already covers registration (`AddOptionsWithValidateOnStart<T, V>().Bind(section)` is Native AOT safe when `Bind` stays at the call site), so Terp should not wrap it. The real gap is the failure collection inside the validators, plus guidance.

## What Changes

- Add `ValidationFailures` in the new `Terp.Options` namespace of the `Terp` package: a small collector with a section prefix, `Require(condition, message)`, `Add(message)` and `ToResult()` returning `ValidateOptionsResult`.
- Add a VitePress docs page "Options mit Setups": the call-site pattern for AOT, the `SYSLIB1104` pitfall with generic wrappers, validator class as the standard, the array-defaults binder pitfall (documented only).
- Add `Migration.md` for Njord and FunkArr: `AddNjordOptions<T, V>` to `AddOptionsWithValidateOnStart<T, V>().Bind(...)`, inline `.Validate(...)` to a validator class, hand-rolled failure lists to `ValidationFailures`.
- Update the `Terp.API.Tests` snapshot for the new public type.
- No new NuGet reference: `Microsoft.Extensions.Options` 10.0.0 is already a transitive dependency of `Terp`; only an explicit `PackageReference` with a `[10.0.0,)` floor in `Directory.Packages.props` is added to make the dependency intentional.

Out of scope: array defaults (`IHasArrayDefaults`), `IConfigSection`, any wrapper around `Bind`/`BindConfiguration` (not AOT safe), attribute-based validation, test helpers.

## Capabilities

### New Capabilities
- `options-validation-failures`: the `ValidationFailures` collector in `Terp.Options` and its contract (prefixing, requirements, result mapping, null handling).
- `docs-options-validation`: the docs page and `Migration.md` describing the recommended options validation path.

### Modified Capabilities

None.

## Impact

- `src/Terp`: new `Options/ValidationFailures.cs`; `Terp.csproj` and `src/Directory.Packages.props` gain an explicit `Microsoft.Extensions.Options` reference.
- `src/Terp.Tests`: new `ValidationFailuresTests`.
- `src/Terp.API.Tests`: updated `CoreApiTests` verified snapshot.
- `docs/`: new page and sidebar entry; `Migration.md` at the repository root or in `docs/`.
- Consumers (Njord, FunkArr) migrate on their own; nothing breaks for existing Terp users.
