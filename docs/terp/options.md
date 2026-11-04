---
title: Options with setups
description: Register and validate options inside a setup, keep binding Native AOT safe, and collect validation failures with ValidationFailures.
---

# Options with setups

Terp does not wrap the options registration. `Microsoft.Extensions.Options` already covers it, and a wrapper would cost Native AOT safety (see below). What Terp adds is `ValidationFailures` in `Terp.Options` (package `Terp`), a small collector for the validators themselves.

```csharp
using Terp.Options;
```

## Register options in a setup

Register inside an `ISetup<IServiceCollection>`. `AddOptionsWithValidateOnStart<TOptions, TValidator>()` registers the validator and validates when the host starts. `Bind` chains onto the returned `OptionsBuilder<TOptions>`.

```csharp
internal sealed class FeedSetup : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context) =>
        services
            .AddOptionsWithValidateOnStart<FeedOptions, FeedOptionsValidator>()
            .Bind(context.Configuration.GetSection(FeedOptions.SectionName));
}
```

Options that have no validator use `AddOptionsWithValidateOnStart<TOptions>()` the same way, so no options type silently skips validation at start.

## Keep binding Native AOT safe

The default configuration binder uses reflection. Under Native AOT and trimming, use the configuration binding source generator:

```xml
<PropertyGroup>
    <EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>
</PropertyGroup>
```

It is enabled automatically for AOT web apps and when `PublishTrimmed` is set. The generator replaces calls to `Bind`, `BindConfiguration`, `Configure<T>(section)` and `Get<T>()` at compile time, and it needs the concrete options type at the call site.

::: warning Call `Bind` where the type is known
Do not hide `Bind` in your own generic helper such as `AddValidatedOptions<T>(...)`. Inside the helper `T` is an open type parameter, so the generator cannot determine the target type (`SYSLIB1104`) and the reflection-based binder stays in place, which produces `IL2026` and `IL3050` under Native AOT. Keep the `Bind(...)` call in the setup, as shown above. That is also why Terp ships no such helper.
:::

## Validate with a validator class

Validate options with an `IValidateOptions<T>` class placed next to the options type. Do not chain inline `.Validate(...)` calls in the setup. A class can be tested without a host, keeps the setup limited to registration and grows from one rule to many without changing style.

`ValidationFailures` removes the boilerplate: the section prefix is passed once, each rule is one line and the closing block is `ToResult()`.

```csharp
public sealed class FeedOptionsValidator : IValidateOptions<FeedOptions>
{
    public ValidateOptionsResult Validate(string? name, FeedOptions options)
    {
        var failures = new ValidationFailures(FeedOptions.SectionName);

        failures.Require(options.Interval > TimeSpan.Zero, "Interval must be positive");
        failures.Require(options.PageSize is > 0 and <= 1000, "PageSize must be between 1 and 1000");
        failures.Require(options.MaxPages > 0, "MaxPages must be positive");

        return failures.ToResult();
    }
}
```

| Member | Behavior |
| --- | --- |
| `new ValidationFailures(section)` | Every failure is stored as `"{section}:{message}"`. A `null` or empty section adds no prefix. |
| `Require(condition, message)` | Records `message` when `condition` is `false`. |
| `Add(message)` | Records `message` unconditionally, for rules that need a loop or an `if` block. |
| `Count` | Number of recorded failures. |
| `ToResult()` | `ValidateOptionsResult.Success` when empty, otherwise a failed result with all failures in insertion order. |

A blank `message` throws an `ArgumentException`, and a `null` one an `ArgumentNullException`, even when the condition holds.

Rules that depend on several properties or on collections stay plain code and use `Add`:

```csharp
for (var i = 0; i < options.Schedule.Count; i++)
{
    if (options.Schedule[i].Start == options.Schedule[i].End)
    {
        failures.Add($"Schedule[{i}]: Start and End must differ");
    }
}
```

Test the validator directly. `Failures` of the result lists every message:

```csharp
var result = new FeedOptionsValidator().Validate(null, new FeedOptions { Interval = TimeSpan.Zero });

Assert.True(result.Failed);
Assert.Contains("Feed:Interval must be positive", result.Failures);
```

## Arrays and collections keep their defaults

When an options property already holds items, for example `public double[] Thresholds { get; set; } = [1, 2, 3];`, binding adds the configured items to the existing ones instead of replacing them. Terp has no feature for this. If you want configuration to win over a default, leave the property empty and apply the default afterwards, for example with `PostConfigure`:

```csharp
services.PostConfigure<AlertOptions>(o =>
{
    if (o.Thresholds is { Length: 0 })
    {
        o.Thresholds = [1, 2, 3];
    }
});
```

## Source-generated validators

`[OptionsValidator]` on a `partial` class generates the `IValidateOptions<T>` implementation from data annotation attributes at compile time and is Native AOT compatible. It plugs into the same registration as a hand-written validator, as `TValidator`. Choose one owner per validator: a class either uses the generator or implements `Validate` itself, not both.
