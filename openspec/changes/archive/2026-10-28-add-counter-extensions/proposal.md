## Why

`Terp.Diagnostics` provides `HistogramTimerExtensions` for histogram timing but has no ergonomic helpers for counters. Analysis of FunkArr (52 `.Add()` calls) and Njord (11 `.Add()` calls) shows every single counter increment is `.Add(1)` or `.Add(-1)` — never a value > 1. The call sites are verbose (`counter.Add(1, new KeyValuePair<string, object?>("key", "value"))`) and semantically flat. Servus had `CounterExtensions` but allowed `.Down()` on monotonic `Counter<T>`, violating the OTel spec.

**Commit type:** `feat` (additive public API on the `Terp` package)

## What Changes

- **New `CounterExtensions` static class** in `Terp.Diagnostics` with 12 extension methods:
  - `Counter<T>`: `.Up()` with 0/1/2/Span tag overloads (4 methods)
  - `UpDownCounter<T>`: `.Up()` and `.Down()` with 0/1/2/Span tag overloads (8 methods)
- Uses `INumber<T>` generic math (`T.One` / `-T.One`), no value parameter
- **No `.Down()` on `Counter<T>`** — enforced by type system, fixing the Servus design bug
- Tag overloads mirror the existing `HistogramTimerExtensions.StartTimer()` pattern (0/1/2/`ReadOnlySpan`)
- API snapshot tests updated

## Capabilities

### New Capabilities

- `counter-extensions`: Semantic `.Up()` / `.Down()` extension methods for `Counter<T>` and `UpDownCounter<T>`, ensuring correct OTel semantics via the type system

### Modified Capabilities

_(none — purely additive)_

## Impact

- **Public API**: Additive only — new extension methods, no changes to existing API
- **Affected code**: `src/Terp/Diagnostics/`, `src/Terp.Tests/Diagnostics/`, `src/Terp.API.Tests/`
- **Dependencies**: None new — `System.Diagnostics.Metrics` and `INumber<T>` are already available in net10.0
- **Breaking**: No
