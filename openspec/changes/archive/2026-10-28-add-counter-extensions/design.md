## Context

`Terp.Diagnostics` already provides `HistogramTimerExtensions` (6 methods on `Histogram<double>`) and `TimerScope` for timing. Counter instruments have no equivalent ergonomics — consumers call `counter.Add(1, new KeyValuePair<string, object?>("key", "value"))` directly.

The existing `HistogramTimerExtensions` establishes the pattern: static extension class, tag overloads at 0/1/2/`ReadOnlySpan<KeyValuePair<string, object?>>` arity, null-guard on the instrument, XML docs on every method.

No new dependencies are needed — `INumber<T>` generic math is part of `System.Numerics` in net10.0, and `System.Diagnostics.Metrics` is already used throughout `Terp.Diagnostics`. No `ISetup<T>` targets are affected. The implementation is purely static extension methods — fully trim/AOT-safe.

## Goals / Non-Goals

**Goals:**

- Semantic `.Up()` / `.Down()` for counters, matching the ergonomic standard set by `HistogramTimerExtensions`
- Enforce OTel spec via the type system: `.Down()` only on `UpDownCounter<T>`, not on monotonic `Counter<T>`
- Same tag overload pattern (0/1/2/Span) as `StartTimer()` for consistency across `Terp.Diagnostics`

**Non-Goals:**

- No value overloads (`.Up(5)`, `.Down(3)`) — real-world usage is always ±1
- No `TagList` overload — `TagList` implicitly converts to `ReadOnlySpan<KVP>`, and `Counter<T>.Add` itself lacks a `TagList` overload
- No `Abs()` on input values — Servus did this and it masks bugs
- No changes to `HistogramTimerExtensions` or `TimerScope`
- No `ObservableGauge` helpers — .NET 9+ `Gauge<T>` replaces the manual pattern

## Decisions

### One file: `CounterExtensions.cs`

Follows the existing pattern: `HistogramTimerExtensions.cs` is one file with all overloads. `CounterExtensions.cs` mirrors this. File lives in `src/Terp/Diagnostics/`.

**Alternative considered:** Separate files for `Counter<T>` and `UpDownCounter<T>` extensions. Rejected — the overloads are small and conceptually one unit.

### Generic `T` with `INumber<T>` constraint

```csharp
public static void Up<T>(this Counter<T> counter) where T : struct, INumber<T>
    => counter.Add(T.One);
```

Uses `T.One` and `-T.One` from `INumber<T>`. This matches how `Counter<T>` and `UpDownCounter<T>` are defined in the BCL (they require `struct` on `T`; the `INumber<T>` constraint is needed for `T.One`). Adding `INumber<T>` is not restrictive — the BCL's `Add` methods work with `byte`, `short`, `int`, `long`, `float`, `double`, `decimal`, all of which implement `INumber<T>`.

**Alternative considered:** Concrete overloads for `int` and `long` only. Rejected — the generic approach covers all numeric types the BCL supports with no additional code.

### No value parameter

Every counter call in FunkArr (52×) and Njord (11×) uses exactly `1` or `-1`. A `.Up(value)` overload would add API surface for a case that doesn't exist in practice, and `Abs()` on the value (as Servus did) silently converts `.Up(-3)` to `.Up(3)` — a bug masker.

### Null guard with `ArgumentNullException.ThrowIfNull`

Same as `HistogramTimerExtensions` — every method starts with `ArgumentNullException.ThrowIfNull(counter)`.

## Risks / Trade-offs

- **[Discoverability]** Consumers must know to add `using Terp.Diagnostics;` to see the extensions. → Same situation as `HistogramTimerExtensions`; mitigated by docs and examples.
- **[API surface growth]** 12 new public methods. → Minimal, all thin wrappers. No new types or concepts.
- **[Generic math requirement]** `INumber<T>` constraint is stricter than the BCL's `Counter<T>` which only requires `struct`. → In practice all numeric types used with counters implement `INumber<T>`. A counter of a custom struct that doesn't implement `INumber<T>` simply can't use `.Up()` and falls back to `.Add()`.
