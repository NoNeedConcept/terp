## ADDED Requirements

### Requirement: ValidationFailures collector

The `Terp.Options` namespace of the `Terp` package SHALL provide a public `sealed class ValidationFailures` that collects validation failure messages for an options validator and maps them to a `Microsoft.Extensions.Options.ValidateOptionsResult`.

```csharp
namespace Terp.Options;

public sealed class ValidationFailures
{
    public ValidationFailures(string? section = null);
    public string? Section { get; }
    public int Count { get; }
    public void Add(string message);
    public void Require(bool condition, string message);
    public ValidateOptionsResult ToResult();
}
```

The type MUST NOT use reflection, expression trees or dynamic code. The type is not thread-safe and is intended to be created per `Validate` call.

#### Scenario: New collector is empty
- **WHEN** a `ValidationFailures` is created
- **THEN** `Count` is `0` and `Section` equals the constructor argument

### Requirement: Section prefix

When `Section` is neither `null` nor empty, every recorded failure SHALL be stored as `"{Section}:{message}"`. When `Section` is `null` or empty, failures SHALL be stored unchanged.

#### Scenario: Message is prefixed with the section
- **WHEN** `new ValidationFailures("FunkArr:Feed")` receives `Add("Interval must be positive")`
- **THEN** the resulting failure is `"FunkArr:Feed:Interval must be positive"`

#### Scenario: No prefix without a section
- **WHEN** `new ValidationFailures()` receives `Add("Interval must be positive")`
- **THEN** the resulting failure is `"Interval must be positive"`

### Requirement: Add records a failure

`Add(string message)` SHALL record `message` as a failure and increase `Count` by one. It MUST throw `ArgumentNullException` when `message` is `null` and `ArgumentException` when `message` is empty or whitespace.

#### Scenario: Add increases the count
- **WHEN** `Add("a")` and `Add("b")` are called
- **THEN** `Count` is `2`

#### Scenario: Null message throws
- **WHEN** `Add(null!)` is called
- **THEN** an `ArgumentNullException` with `ParamName` `message` is thrown

#### Scenario: Whitespace message throws
- **WHEN** `Add("  ")` is called
- **THEN** an `ArgumentException` with `ParamName` `message` is thrown

### Requirement: Require records a failure only when the condition is false

`Require(bool condition, string message)` SHALL record `message` as a failure exactly when `condition` is `false`, and SHALL do nothing when `condition` is `true`. It MUST validate `message` like `Add`, including when `condition` is `true`.

#### Scenario: Satisfied condition records nothing
- **WHEN** `Require(true, "x")` is called
- **THEN** `Count` is `0`

#### Scenario: Violated condition records the message
- **WHEN** `Require(false, "x")` is called
- **THEN** `Count` is `1` and the failure ends with `"x"`

#### Scenario: Null message is rejected even for a satisfied condition
- **WHEN** `Require(true, null!)` is called
- **THEN** an `ArgumentNullException` with `ParamName` `message` is thrown

### Requirement: ToResult maps to ValidateOptionsResult

`ToResult()` SHALL return `ValidateOptionsResult.Success` when no failure was recorded. Otherwise it SHALL return `ValidateOptionsResult.Fail` carrying all recorded failures in insertion order, so that `Failed` is `true` and `Failures` contains every message.

#### Scenario: No failures yields success
- **WHEN** `ToResult()` is called on an empty collector
- **THEN** the result is `ValidateOptionsResult.Success`

#### Scenario: Failures yield a failed result in order
- **WHEN** `Add("a")` then `Add("b")` were called with section `"S"`
- **THEN** `ToResult()` is failed and `Failures` equals `["S:a", "S:b"]`

### Requirement: Public API snapshot

The `Terp.API.Tests` snapshot of the `Terp` package SHALL include `Terp.Options.ValidationFailures` with exactly the members listed above. The change MUST be additive and MUST NOT alter other public members.

#### Scenario: Snapshot shows only the new type
- **WHEN** the API snapshot test runs after this change
- **THEN** the only difference to the previous verified file is the added `Terp.Options.ValidationFailures` type
