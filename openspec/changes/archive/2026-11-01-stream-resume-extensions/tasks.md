## 1. Implementation

- [ ] 1.1 Create `src/Terp.Akka/StreamSupervisionExtensions.cs` with four public extension methods (`ResumeOnError` on `Source` and `Flow`, parameterless and `ILoggingAdapter` overloads) plus internal `Unwrap` and `IsTransient` helpers. Follow `BackoffExtensions.cs` style: static class, XML docs, `ArgumentNullException.ThrowIfNull` on `log` parameter.

## 2. Tests

- [ ] 2.1 Create `src/Terp.Akka.Tests/StreamSupervisionExtensionsTests.cs` covering all spec scenarios: silent resume on Source/Flow, logging resume with transient vs non-transient distinction, AggregateException unwrapping, null-log ArgumentNullException, and materialized value preservation.

## 3. API snapshot

- [ ] 3.1 Run API snapshot tests, review the `.received.` diff for `Terp.Akka`, and rename to `.verified.txt` to accept the new `StreamSupervisionExtensions` surface.

## 4. Verify

- [ ] 4.1 Run full build and test suite: `dotnet build src/Terp.slnx -p:ContinuousIntegrationBuild=true` and `dotnet test --solution Terp.slnx` (from `src/`). Fix any warnings or failures.
- [ ] 4.2 Run `dotnet format src/Terp.slnx --verify-no-changes` to confirm code style compliance.
- [ ] 4.3 Commit with `feat(akka): add stream supervision resume extensions` (conventional commit scope: `akka`).
