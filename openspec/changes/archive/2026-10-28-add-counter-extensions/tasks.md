## 1. Implementation

- [x] 1.1 Create `src/Terp/Diagnostics/CounterExtensions.cs` with 12 extension methods (4× `Counter<T>.Up`, 4× `UpDownCounter<T>.Up`, 4× `UpDownCounter<T>.Down`), each with `ArgumentNullException.ThrowIfNull`, XML docs, and `INumber<T>` constraint

## 2. Tests

- [x] 2.1 Create `src/Terp.Tests/Diagnostics/CounterExtensionsTests.cs` — test `Counter<T>.Up()` with 0/1/2/Span tags, verify recorded measurement value and tags via `MetricCollector`
- [x] 2.2 Add `UpDownCounter<T>.Up()` tests — same tag arity variants, verify measurement is `+1`
- [x] 2.3 Add `UpDownCounter<T>.Down()` tests — same tag arity variants, verify measurement is `-1`
- [x] 2.4 Add null-guard tests for all three groups (`Counter.Up`, `UpDownCounter.Up`, `UpDownCounter.Down`) — verify `ArgumentNullException` with correct parameter name

## 3. API Snapshot

- [x] 3.1 Run API tests to generate `.received.` snapshot, review the diff, rename to `.verified.txt` to accept the new public surface

## 4. Verify

- [x] 4.1 `dotnet build src/Terp.slnx -p:ContinuousIntegrationBuild=true` — zero warnings
- [x] 4.2 `dotnet test --solution Terp.slnx` (from `src/`) — all tests pass
- [x] 4.3 `dotnet format src/Terp.slnx --verify-no-changes` — code style clean
- [x] 4.4 Commit with `feat(diagnostics): add CounterExtensions with Up/Down for Counter and UpDownCounter` Commit with `feat(diagnostics): add CounterExtensions with Up/Down for Counter and UpDownCounter`
