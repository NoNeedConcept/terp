## Why

Both njord and funkarr repeat the same Akka.Streams supervision pattern: wrap `ActorAttributes.CreateSupervisionStrategy(...)` with a resuming decider, optionally with logging and transient exception classification. This boilerplate appears in 6+ call sites across those projects. Extracting it into `Terp.Akka` as fluent extensions eliminates the duplication and gives a tested, consistent default.

Commit type: `feat`, public API, additive (non-breaking)

## What Changes

- Add `StreamSupervisionExtensions` static class in namespace `Terp.Akka` with extensions on `Source<TOut, TMat>` and `Flow<TIn, TOut, TMat>`:
  - `.ResumeOnError()` — silent resume (replaces `Deciders.ResumingDecider` wrapping boilerplate)
  - `.ResumeOnError(ILoggingAdapter log)` — logs with transient/unexpected distinction, then resumes
- Internal helper for `AggregateException` unwrapping (from Njord)
- Internal transient exception classification: `AskTimeoutException`, `TaskCanceledException`, `OperationCanceledException`, `TimeoutException`, `HttpRequestException`
- No new NuGet dependency — `Akka.Streams` is already transitive via `Akka.Cluster.Sharding`

## Capabilities

### New Capabilities

- `stream-resume`: Stream supervision resume extensions that wrap Akka.Streams supervision strategy configuration into fluent `.ResumeOnError()` calls with optional logging and transient exception awareness.

### Modified Capabilities

_None._

## Impact

- New file: `src/Terp.Akka/StreamSupervisionExtensions.cs`
- New test file: `src/Terp.Akka.Tests/StreamSupervisionExtensionsTests.cs`
- API snapshot update: `src/Terp.API.Tests/verify/AkkaApiTests.*.verified.txt`
- Docs: new section under Terp.Akka docs
