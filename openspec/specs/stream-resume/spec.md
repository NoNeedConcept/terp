# stream-resume Specification

## Purpose
TBD - created by archiving change stream-resume-extensions. Update Purpose after archive.
## Requirements
### Requirement: Silent resume extension on Source

The `Terp.Akka` package (namespace `Terp.Akka`) SHALL provide a `ResumeOnError<TOut, TMat>` extension method on `Akka.Streams.Dsl.Source<TOut, TMat>` that returns `Source<TOut, TMat>` with a supervision strategy that resumes on any exception.

```csharp
using Terp.Akka;

Source.From(items).ResumeOnError()
```

#### Scenario: Source element throws

- **WHEN** a `Source` stage throws any exception and `.ResumeOnError()` has been applied
- **THEN** the failing element is dropped and the stream continues processing subsequent elements

#### Scenario: Return type preserves materialized value

- **WHEN** `.ResumeOnError()` is called on a `Source<TOut, TMat>`
- **THEN** the return type SHALL be `Source<TOut, TMat>` (same materialized value type)

### Requirement: Silent resume extension on Flow

The `Terp.Akka` package SHALL provide a `ResumeOnError<TIn, TOut, TMat>` extension method on `Akka.Streams.Dsl.Flow<TIn, TOut, TMat>` that returns `Flow<TIn, TOut, TMat>` with a supervision strategy that resumes on any exception.

```csharp
using Terp.Akka;

Flow.Create<int>().Select(x => x * 2).ResumeOnError()
```

#### Scenario: Flow element throws

- **WHEN** a `Flow` stage throws any exception and `.ResumeOnError()` has been applied
- **THEN** the failing element is dropped and the stream continues processing subsequent elements

#### Scenario: Return type preserves materialized value

- **WHEN** `.ResumeOnError()` is called on a `Flow<TIn, TOut, TMat>`
- **THEN** the return type SHALL be `Flow<TIn, TOut, TMat>` (same materialized value type)

### Requirement: Logging resume extension on Source

The `Terp.Akka` package SHALL provide a `ResumeOnError<TOut, TMat>` overload on `Source<TOut, TMat>` that accepts an `Akka.Event.ILoggingAdapter` and logs each exception before resuming.

```csharp
using Terp.Akka;

source.ResumeOnError(log)
```

The method SHALL throw `ArgumentNullException` when `log` is `null`.

#### Scenario: Transient exception logs as warning

- **WHEN** a source stage throws a transient exception (`AskTimeoutException`, `TaskCanceledException`, `OperationCanceledException`, `TimeoutException`, or `HttpRequestException`) and `.ResumeOnError(log)` has been applied
- **THEN** the exception SHALL be logged at `Warning` level and the stream SHALL resume

#### Scenario: Non-transient exception logs as error

- **WHEN** a source stage throws a non-transient exception and `.ResumeOnError(log)` has been applied
- **THEN** the exception SHALL be logged at `Error` level and the stream SHALL resume

#### Scenario: Wrapped AggregateException is unwrapped

- **WHEN** the thrown exception is an `AggregateException` with a single inner exception
- **THEN** the inner exception SHALL be used for transient classification and logging (recursively unwrapping nested single-inner `AggregateException`)

#### Scenario: Null log throws

- **WHEN** `.ResumeOnError(null!)` is called
- **THEN** an `ArgumentNullException` SHALL be thrown with parameter name `log`

### Requirement: Logging resume extension on Flow

The `Terp.Akka` package SHALL provide a `ResumeOnError<TIn, TOut, TMat>` overload on `Flow<TIn, TOut, TMat>` that accepts an `ILoggingAdapter` and logs each exception before resuming. Behaviour is identical to the Source overload.

#### Scenario: Transient exception logs as warning

- **WHEN** a flow stage throws a transient exception and `.ResumeOnError(log)` has been applied
- **THEN** the exception SHALL be logged at `Warning` level and the stream SHALL resume

#### Scenario: Non-transient exception logs as error

- **WHEN** a flow stage throws a non-transient exception and `.ResumeOnError(log)` has been applied
- **THEN** the exception SHALL be logged at `Error` level and the stream SHALL resume

#### Scenario: Null log throws

- **WHEN** `.ResumeOnError(null!)` is called
- **THEN** an `ArgumentNullException` SHALL be thrown with parameter name `log`

### Requirement: API snapshot

The public API of `Terp.Akka` SHALL include the `StreamSupervisionExtensions` class with four public extension methods. The API snapshot test (`AkkaApiTests`) MUST pass after updating the verified file.

#### Scenario: Snapshot includes new extensions

- **WHEN** the API snapshot is generated for `Terp.Akka`
- **THEN** it SHALL contain `StreamSupervisionExtensions` with `ResumeOnError` overloads for `Source` and `Flow` (parameterless and `ILoggingAdapter`)

