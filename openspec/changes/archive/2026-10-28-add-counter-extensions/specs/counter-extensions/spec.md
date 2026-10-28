## ADDED Requirements

### Requirement: Counter Up extension methods

The `Terp.Diagnostics` namespace SHALL provide extension methods on `Counter<T>` (where `T : struct, INumber<T>`) named `Up` that call `counter.Add(T.One)` with the supplied tags.

Overloads:
```csharp
public static void Up<T>(this Counter<T> counter) where T : struct, INumber<T>;
public static void Up<T>(this Counter<T> counter, KeyValuePair<string, object?> tag) where T : struct, INumber<T>;
public static void Up<T>(this Counter<T> counter, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2) where T : struct, INumber<T>;
public static void Up<T>(this Counter<T> counter, ReadOnlySpan<KeyValuePair<string, object?>> tags) where T : struct, INumber<T>;
```

Each method MUST throw `ArgumentNullException` when `counter` is `null`.

#### Scenario: Increment counter without tags
- **WHEN** calling `counter.Up()` on a `Counter<long>`
- **THEN** the counter records a measurement of `1L` with no tags

#### Scenario: Increment counter with one tag
- **WHEN** calling `counter.Up(new KeyValuePair<string, object?>("api", "tmdb"))`
- **THEN** the counter records a measurement of `1L` with tag `api=tmdb`

#### Scenario: Increment counter with two tags
- **WHEN** calling `counter.Up(tag1, tag2)` with `api=tmdb` and `status=hit`
- **THEN** the counter records a measurement of `1L` with both tags

#### Scenario: Increment counter with span tags
- **WHEN** calling `counter.Up(tagsSpan)` with a `ReadOnlySpan` of 3 tags
- **THEN** the counter records a measurement of `1L` with all 3 tags

#### Scenario: Null counter throws
- **WHEN** calling `Up()` on a `null` `Counter<long>`
- **THEN** an `ArgumentNullException` is thrown with parameter name `counter`

### Requirement: UpDownCounter Up extension methods

The `Terp.Diagnostics` namespace SHALL provide extension methods on `UpDownCounter<T>` (where `T : struct, INumber<T>`) named `Up` that call `counter.Add(T.One)` with the supplied tags.

Overloads SHALL mirror the `Counter<T>.Up` overloads (0/1/2/Span tags). Each method MUST throw `ArgumentNullException` when `counter` is `null`.

#### Scenario: Increment up-down counter without tags
- **WHEN** calling `upDownCounter.Up()` on an `UpDownCounter<int>`
- **THEN** the counter records a measurement of `1` with no tags

#### Scenario: Null up-down counter throws on Up
- **WHEN** calling `Up()` on a `null` `UpDownCounter<int>`
- **THEN** an `ArgumentNullException` is thrown with parameter name `counter`

### Requirement: UpDownCounter Down extension methods

The `Terp.Diagnostics` namespace SHALL provide extension methods on `UpDownCounter<T>` (where `T : struct, INumber<T>`) named `Down` that call `counter.Add(-T.One)` with the supplied tags.

Overloads SHALL mirror the `Up` overloads (0/1/2/Span tags). Each method MUST throw `ArgumentNullException` when `counter` is `null`.

#### Scenario: Decrement up-down counter without tags
- **WHEN** calling `upDownCounter.Down()` on an `UpDownCounter<int>`
- **THEN** the counter records a measurement of `-1` with no tags

#### Scenario: Decrement up-down counter with one tag
- **WHEN** calling `upDownCounter.Down(new KeyValuePair<string, object?>("pool", "connections"))`
- **THEN** the counter records a measurement of `-1` with tag `pool=connections`

#### Scenario: Null up-down counter throws on Down
- **WHEN** calling `Down()` on a `null` `UpDownCounter<int>`
- **THEN** an `ArgumentNullException` is thrown with parameter name `counter`

### Requirement: No Down on monotonic Counter

The `CounterExtensions` class SHALL NOT provide any `Down` method on `Counter<T>`. Monotonic counters MUST only increase per the OpenTelemetry specification.

#### Scenario: Down is not available on Counter
- **WHEN** a developer writes `counter.Down()` where `counter` is `Counter<long>`
- **THEN** the code SHALL NOT compile — no such extension method exists

### Requirement: All extension methods live in a single static class

All 12 extension methods SHALL be defined in a single `public static class CounterExtensions` in the `Terp.Diagnostics` namespace, in the file `src/Terp/Diagnostics/CounterExtensions.cs`.

#### Scenario: Class structure
- **WHEN** inspecting the `CounterExtensions` class
- **THEN** it contains exactly 12 public static extension methods: 4 for `Counter<T>.Up`, 4 for `UpDownCounter<T>.Up`, 4 for `UpDownCounter<T>.Down`
