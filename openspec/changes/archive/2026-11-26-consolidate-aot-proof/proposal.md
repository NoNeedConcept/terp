## Why

The recent `IsAotCompatible=true` / `IsTrimmable=true` declaration in `Directory.Build.props` applies to **all** projects, including `Terp.Akka` — which depends on Akka.NET, a library that is not yet trim/AOT-safe. This caused an IL2091 build failure on CI. Meanwhile, the AOT proof is split across two examples (`Terp.Example` for diagnostics/resilience, `Terp.Hosting.Example` for hosting/gates), but only the latter actually publishes with Native AOT.

**Conventional commit type:** ci, refactor (internal only, no public API change)

## What Changes

- **Remove `IsAotCompatible` and `IsTrimmable` from `Directory.Build.props`** — stop declaring all projects as AOT-compatible.
- **Add `IsAotCompatible=true` and `IsTrimmable=true` to `Terp.csproj` only** — the one package that is genuinely AOT-proven.
- **Leave `Terp.Akka` without AOT declarations** — it cannot be AOT until Akka.NET upstream supports it. Keep existing `[DynamicallyAccessedMembers]` annotations as forward-looking preparation.
- **Merge `Terp.Hosting.Example` into `Terp.Example`** — one consolidated example that exercises all Terp features (diagnostics, resilience, hosting setups, startup gates) and serves as the single Native AOT proof (`PublishAot=true`).
- **Delete `Terp.Hosting.Example`** — remove project, solution reference, and CI job reference.
- **Update `extra-ci.yml`** to publish `Terp.Example` instead of `Terp.Hosting.Example`.
- **Update `AGENTS.md`** to reflect the new example layout.

## Capabilities

### New Capabilities

- `consolidated-aot-example`: Single `Terp.Example` project that demonstrates all Terp library features and serves as the Native AOT proof for the `Terp` package in CI.

### Modified Capabilities

_(none — no spec-level requirement changes)_

## Impact

- **Code:** `src/Terp.Example/` rewritten as hosted app with `PublishAot=true`; `src/Terp.Hosting.Example/` deleted; `Terp.slnx` updated.
- **Build props:** `Directory.Build.props` loses two properties; `Terp.csproj` gains them.
- **CI:** `extra-ci.yml` publish path changes. The `dotnet build` step no longer triggers IL20xx warnings for `Terp.Akka`.
- **APIs / Dependencies:** No changes — both NuGet packages ship identical public APIs.
- **`PortFinder` / `TerpHttpMetricsHandler`:** No longer exercised in the example (require `HttpListener`, not AOT-safe on Linux). Covered by unit tests only.
