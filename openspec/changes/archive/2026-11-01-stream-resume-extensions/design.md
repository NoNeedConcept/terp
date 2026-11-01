## Context

Akka.Streams provides `ActorAttributes.CreateSupervisionStrategy(Decider)` to control what happens when a stream stage throws. The most common strategy in our projects (njord, funkarr) is "resume": drop the failing element and continue. This requires verbose boilerplate at every call site:

```csharp
.WithAttributes(ActorAttributes.CreateSupervisionStrategy(Deciders.ResumingDecider))
// or with logging:
.WithAttributes(ActorAttributes.CreateSupervisionStrategy(ex => { log.Warning(...); return Directive.Resume; }))
```

Terp.Akka already follows the pattern of thin extension classes (`BackoffExtensions`, `ContextExtensions`, `RegistryExtensions`) that wrap Akka boilerplate into fluent calls. Stream supervision fits the same mould.

## Goals / Non-Goals

**Goals:**

- Provide `.ResumeOnError()` extensions on `Source<,>` and `Flow<,,>` that eliminate the `WithAttributes(ActorAttributes.CreateSupervisionStrategy(...))` boilerplate
- Consolidate the transient-aware logging decider pattern from njord into a reusable, tested helper
- Stay trim/AOT-safe — pure static methods, no reflection, no code generation

**Non-Goals:**

- Configurable transient exception sets (v1 uses a fixed set; extensibility can come later if needed)
- Extensions on `SubFlow`, `GraphDsl`, or `Sink` (not used in existing code)
- `Restart` or `Stop` directive variants (only `Resume` is needed today)
- An `ISetup<T>` target — this is a stream-level concern, not a host-level one

## Decisions

### 1. Extension methods on `Source<,>` and `Flow<,,>` returning the same stream type

**Why:** Mirrors how `.WithAttributes()` works — returns the same type so it chains. Extending `Source` and `Flow` covers all usage sites in njord and funkarr. `SubFlow` could be added later but has no known use case.

**Alternative:** A static factory `StreamSupervision.ResumeAttributes(log)` returning `Attributes`. Rejected because the extension reads better at the call site and hides the `WithAttributes` wrapping.

### 2. Two overloads: parameterless and `ILoggingAdapter`

**Why:** Matches the two patterns found in practice. The parameterless form replaces `Deciders.ResumingDecider` (4 call sites in funkarr). The logging form replaces `StreamSupervision.LoggingDecider(log)` (2 call sites in njord, 1 in funkarr).

**Alternative:** A single overload with `ILoggingAdapter? log = null`. Rejected — the parameterless overload avoids allocating a closure when logging is not wanted.

### 3. Fixed transient exception set, internal

**Why:** The set (`AskTimeoutException`, `TaskCanceledException`, `OperationCanceledException`, `TimeoutException`, `HttpRequestException`) matches njord's `IsTransient` exactly and covers all realistic transient failures in actor stream pipelines. Making it public or configurable adds API surface with no known consumer.

### 4. AggregateException unwrap

**Why:** Akka wraps stage failures in `AggregateException` in some cases (e.g. `SelectAsync` with faulted tasks). Without unwrapping, transient classification would miss wrapped exceptions. Njord already does this.

### 5. Namespace `Terp.Akka`, file `StreamSupervisionExtensions.cs`

**Why:** Same namespace and naming convention as `BackoffExtensions`, `ContextExtensions`, `RegistryExtensions`. One public static class per file, file name = type name.

## Risks / Trade-offs

- **Fixed transient set may not cover all consumers** → Mitigation: the set matches real usage across two production projects. Consumers needing a custom set can still use `WithAttributes` directly. A future overload with `Func<Exception, bool>` is additive.
- **Akka.Streams is a transitive dependency** → If Akka packaging changes upstream and Streams becomes a separate opt-in, the extension methods would cause compile errors. Mitigation: extremely unlikely given Akka.Streams is bundled with Akka.Cluster.Sharding, which Terp.Akka already depends on directly.
