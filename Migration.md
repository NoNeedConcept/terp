# Migration: options validation with Terp

Working notes for moving Njord and FunkArr onto one options path. Nothing here is required by Terp itself. See `docs/terp/options.md` for the reference.

Requires a Terp version that contains `Terp.Options.ValidationFailures`.

## The rule

Options are validated by an `IValidateOptions<T>` class placed next to the options type. Not inline, not in the setup.

- Registration: `AddOptionsWithValidateOnStart<T, TValidator>().Bind(section)`, or `AddOptionsWithValidateOnStart<T>().Bind(section)` when there is no validator.
- Validator body: `ValidationFailures` with the section name once, `Require` for single rules, `Add` inside loops and `if` blocks, `ToResult()` as the closing statement.
- `Bind(...)` always stays at the call site in the setup, so the configuration binding generator sees the concrete type (Native AOT).

Why a class and not inline: it is testable without a host (`new XValidator().Validate(null, options)`), the setup stays pure registration, and there is one style instead of a per-case choice. In the two projects it is already the dominant style (55 validator classes against one inline block).

## 1. Registration

### Njord: `AddNjordOptions<T, TValidator>()`

Before:

```csharp
services.AddNjordOptions<ForecastOptions, ForecastOptionsValidator>();
```

After:

```csharp
services
    .AddOptionsWithValidateOnStart<ForecastOptions, ForecastOptionsValidator>()
    .BindConfiguration(IConfigSection.SectionOf<ForecastOptions>());
```

In a setup that has a `SetupContext`, `Bind(context.Configuration.GetSection(...))` is equivalent and the preferred form, because it needs no `IConfiguration` lookup from the container.

Options without a validator:

```csharp
// before
services.AddNjordOptions<PersistenceOptions>();
// after
services
    .AddOptionsWithValidateOnStart<PersistenceOptions>()
    .BindConfiguration(IConfigSection.SectionOf<PersistenceOptions>());
```

What `AddNjordOptions` did that stays in Njord:

| Behavior | Where it goes |
| --- | --- |
| `IConfigSection` and its `SectionName` | Stays in Njord. |
| `IHasArrayDefaults` + `PostConfigure(o => o.ApplyDefaults())` | Stays in Njord (about 20 alert config types). Call `PostConfigure` on the returned builder or keep a small local helper. Terp has no feature for it; see the array section in `docs/terp/options.md`. |

Njord may keep `AddNjordOptions` as a thin local helper for `IConfigSection` and the array defaults. Its `BindConfiguration<T>` call then sits in a generic method, which is reflection-based and warns under trimming and Native AOT. Njord is a normal ASP.NET Core app today, so that is acceptable there; an AOT app must call `Bind` at the call site instead.

### FunkArr: three-line pattern

Before (`CoreSetupContainer`):

```csharp
services
    .AddOptions<RoutingOptions>()
    .Bind(context.Configuration.GetSection(RoutingOptions.SectionName))
    .ValidateOnStart();

services.AddSingleton<IValidateOptions<RoutingOptions>, RoutingOptionsValidator>();
```

After:

```csharp
services
    .AddOptionsWithValidateOnStart<RoutingOptions, RoutingOptionsValidator>()
    .Bind(context.Configuration.GetSection(RoutingOptions.SectionName));
```

Same for `DownloadOptions` (`DownloadSetupContainer`).

`PostgresOptions` is bound without `ValidateOnStart` today. Decide whether that is intended. If not:

```csharp
services
    .AddOptionsWithValidateOnStart<PostgresOptions>()
    .Bind(context.Configuration.GetSection(PostgresOptions.SectionName));
```

## 2. Inline validation to a validator class

Applies to `FeedSetupContainer` in FunkArr.

Before:

```csharp
services
    .AddOptions<FeedOptions>()
    .Bind(context.Configuration.GetSection(FeedOptions.SectionName))
    .Validate(o => o.Interval > TimeSpan.Zero, "FunkArr:Feed:Interval must be positive")
    .Validate(o => o.PageSize is > 0 and <= 1000, "FunkArr:Feed:PageSize must be between 1 and 1000")
    .Validate(o => o.MaxPages > 0, "FunkArr:Feed:MaxPages must be positive")
    .Validate(o => o.MaxCatchUp > TimeSpan.Zero, "FunkArr:Feed:MaxCatchUp must be positive")
    .Validate(o => o.MaxConcurrency > 0, "FunkArr:Feed:MaxConcurrency must be positive")
    .Validate(o => o.MinConfidence is >= 0 and <= 1, "FunkArr:Feed:MinConfidence must be between 0 and 1")
    .Validate(o => o.Retention > TimeSpan.Zero, "FunkArr:Feed:Retention must be positive")
    .Validate(o => o.InitialLookback > TimeSpan.Zero, "FunkArr:Feed:InitialLookback must be positive")
    .ValidateOnStart();
```

After, in the setup:

```csharp
services
    .AddOptionsWithValidateOnStart<FeedOptions, FeedOptionsValidator>()
    .Bind(context.Configuration.GetSection(FeedOptions.SectionName));
```

After, new file `FunkArr.Core/FeedOptionsValidator.cs`:

```csharp
using Microsoft.Extensions.Options;
using Terp.Options;

namespace FunkArr.Core;

public sealed class FeedOptionsValidator : IValidateOptions<FeedOptions>
{
    public ValidateOptionsResult Validate(string? name, FeedOptions options)
    {
        var failures = new ValidationFailures(FeedOptions.SectionName);

        failures.Require(options.Interval > TimeSpan.Zero, "Interval must be positive");
        failures.Require(options.PageSize is > 0 and <= 1000, "PageSize must be between 1 and 1000");
        failures.Require(options.MaxPages > 0, "MaxPages must be positive");
        failures.Require(options.MaxCatchUp > TimeSpan.Zero, "MaxCatchUp must be positive");
        failures.Require(options.MaxConcurrency > 0, "MaxConcurrency must be positive");
        failures.Require(options.MinConfidence is >= 0 and <= 1, "MinConfidence must be between 0 and 1");
        failures.Require(options.Retention > TimeSpan.Zero, "Retention must be positive");
        failures.Require(options.InitialLookback > TimeSpan.Zero, "InitialLookback must be positive");

        return failures.ToResult();
    }
}
```

The messages stay identical (`FunkArr:Feed:Interval must be positive`), because the section prefix is added by `ValidationFailures`. Add a `FeedOptionsValidatorTests` next to the existing `DownloadOptionsValidatorTests`.

## 3. Validator boilerplate

Applies to every validator that builds a `List<string>`.

Before (`DownloadOptionsValidator`):

```csharp
var failures = new List<string>();

for (var i = 0; i < options.DownloadSchedule.Count; i++)
{
    var slot = options.DownloadSchedule[i];
    if (slot.Start == slot.End)
    {
        failures.Add($"DownloadSchedule[{i}]: Start and End must differ (both are {slot.Start}).");
    }
}

return failures.Count > 0
    ? ValidateOptionsResult.Fail(failures)
    : ValidateOptionsResult.Success;
```

After:

```csharp
var failures = new ValidationFailures(DownloadOptions.SectionName);

for (var i = 0; i < options.DownloadSchedule.Count; i++)
{
    var slot = options.DownloadSchedule[i];
    failures.Require(slot.Start != slot.End, $"DownloadSchedule[{i}]: Start and End must differ (both are {slot.Start}).");
}

return failures.ToResult();
```

Before (Njord `HeatConfigValidator`, section prefix typed in every message):

```csharp
var section = IConfigSection.SectionOf<HeatConfig>();
var failures = new List<string>();
if (ThresholdValidation.Violation($"{section}:Thresholds", options.Thresholds, ScanDirection.Max) is { } failure)
{
    failures.Add(failure);
}

if (options.WindowHours < 1)
{
    failures.Add($"{section}:WindowHours must be at least 1, got {options.WindowHours}.");
}

return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
```

After:

```csharp
var failures = new ValidationFailures(IConfigSection.SectionOf<HeatConfig>());
if (ThresholdValidation.Violation("Thresholds", options.Thresholds, ScanDirection.Max) is { } failure)
{
    failures.Add(failure);
}

failures.Require(options.WindowHours >= 1, $"WindowHours must be at least 1, got {options.WindowHours}.");

return failures.ToResult();
```

Helpers that build their own path (like `ThresholdValidation.Violation(path, ...)`) now receive the bare property name, because the prefix comes from `ValidationFailures`. Check each helper call so a message does not end up with the section twice.

Messages that were returned as a single `ValidateOptionsResult.Fail("...")` (for example in `NjordOptionsValidator`) can stay as they are; migrate them only when they are touched.

## 4. Order of work

1. Add the Terp version that contains `ValidationFailures` to both projects.
2. Convert registrations (section 1). Behavior is unchanged except that `ValidateOnStart` is always on.
3. Move `FeedSetupContainer` to `FeedOptionsValidator` (section 2).
4. Convert validator bodies (section 3) opportunistically, file by file. The old and the new style can live side by side, so no big-bang change is needed.

## Not covered

- Array defaults (`IHasArrayDefaults`) and `IConfigSection` remain Njord-internal.
- Attribute-based validation is not used in either project for options. `[OptionsValidator]` can be used for new, purely attribute-shaped options and registers through the same `AddOptionsWithValidateOnStart<T, TValidator>()` call.
- Cross-options validators such as `PollingOptionsValidator`, which read other `IOptions<>` and catch `OptionsValidationException`, are unchanged by this migration.
