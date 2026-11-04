## Context

Njord and FunkArr both configure options inside `ISetup<IServiceCollection>` classes and validate them with hand-written `IValidateOptions<T>` classes (53 in Njord, 2 in FunkArr, plus 8 inline `.Validate(...)` rules in FunkArr's `FeedSetupContainer`). Every validator repeats the same shape: a `List<string>`, `if (...) failures.Add($"{section}:Prop ...")` per rule, and a closing `failures.Count == 0 ? Success : Fail(failures)`. Njord additionally wraps registration in `AddNjordOptions<T, V>` (Bind + `ValidateOnStart` + validator).

.NET 10 already ships `AddOptionsWithValidateOnStart<TOptions, TValidateOptions>()`, which returns an `OptionsBuilder<T>`, so `.Bind(section)` chains onto it. `Microsoft.Extensions.Options` 10.0.0 is already a transitive dependency of `Terp` (via the Diagnostics/Logging abstractions), so no new package arrives for consumers.

## Goals / Non-Goals

**Goals:**
- Remove the failure-list boilerplate from validators with a tiny, reflection-free collector.
- Make the section prefix a one-time input instead of a per-message string.
- Document one recommended options path (validator class, `Bind` at the call site) and migrate Njord/FunkArr onto it.

**Non-Goals:**
- No registration wrapper around `Bind`/`BindConfiguration`, no `ISetup` base class for options.
- No `IConfigSection`, no array-defaults support, no attribute/DataAnnotations integration, no test helpers.
- No fluent rule DSL, no expression trees.

## Decisions

**1. `ValidationFailures` as a small sealed class, not extension methods on `List<string>`.**
The section prefix has to live somewhere; extension methods would force the caller to pass it on every call, which is the duplication we are removing. A class holds the prefix once. Alternative considered: `failures.ToValidateResult()` only. Rejected because it leaves each rule at four lines.

Shape (namespace `Terp.Options`, package `Terp`):
```csharp
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
`Require` records `message` only when `condition` is `false`. Messages are prefixed as `"{Section}:{message}"`; the caller's message starts with the property name, matching what Njord and FunkArr already write. With a null or empty section there is no prefix. `ToResult()` returns `ValidateOptionsResult.Success` when empty, otherwise `ValidateOptionsResult.Fail(IEnumerable<string>)` with all failures in insertion order.

**2. Namespace `Terp.Options`.**
Confirmed with the user. It mirrors the folder `src/Terp/Options/` and keeps validation away from `Terp.Hosting`, which is about setups and gates.

**3. Registration stays with the Microsoft API at the call site.**
```csharp
services.AddOptionsWithValidateOnStart<FeedOptions, FeedOptionsValidator>()
        .Bind(context.Configuration.GetSection(FeedOptions.SectionName));
```
The configuration binding source generator intercepts `Bind` with a concrete type argument at the call site. A generic Terp wrapper (`AddValidatedOptions<T>`) would hand it an open `T` (`SYSLIB1104`) or force `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]` onto the wrapper, which breaks the `Terp.Hosting.Example` guarantee of 0 AOT warnings. Alternative considered: `OptionsBuilder<T>.WithValidator<V>()`. Rejected as redundant with the Microsoft overload.

**4. Validator class is the single recommended style.**
Reasons: testable without a host (`new XValidator().Validate(null, options)`), keeps setups as pure registration, one rule instead of an inline-versus-class decision, and 55 versus 8 in existing use. Inline `.Validate(...)` chains move into a validator class. With `Require` a rule is one line, so inline loses its only advantage.

**5. Trim/AOT safety.**
`ValidationFailures` has no reflection, no generics over unknown types and no dynamic code; it only builds a list and calls `ValidateOptionsResult.Fail`. It is verified by the existing trim analysis of `Terp` and does not touch `Terp.Hosting.Example`.

**6. Dependency.**
Add an explicit `PackageReference Include="Microsoft.Extensions.Options"` to `Terp.csproj` with `Version="[10.0.0,)"` in `Directory.Packages.props` (central package management, minimum version floor, no Renovate bump). The package is already restored transitively; making it explicit documents that `Terp` now compiles against it directly. `ISetup<T>` targets are not affected.

**7. Docs and migration placement.**
The docs page is `docs/terp/options.md`, titled "Options with setups", in English like all existing pages, with a sidebar entry in the `Terp` group of `docs/.vitepress/config.ts` next to Setups and Startup gates. `Migration.md` sits at the repository root as a working document for the consumer projects (English as well) and is not published through VitePress.

## Risks / Trade-offs

- **[Risk] The class is mutable and not thread-safe** → It is created per `Validate` call inside a validator and never shared; documented in XML docs.
- **[Risk] Two prefix conventions in the wild (Njord uses `{section}:Prop`, FunkArr `FunkArr:Feed:Interval`)** → Both are `"{section}:{message}"`; the section string is whatever the caller passes, so both stay valid.
- **[Risk] Public API addition requires a snapshot update** → Update `verify/CoreApiTests` verified file in the same change; it is additive, so a `feat:` commit and a minor bump.
- **[Trade-off] Savings are modest per validator (one-line rules, no closing block)** → Accepted; the value is consistency and the removed duplication across 55 classes, not a large new feature.
- **[Risk] The claim that generic wrappers are not intercepted by the binding generator is inferred from `SYSLIB1104`** → The docs page states the call-site pattern as the supported one without asserting more; an optional spike in `Terp.Hosting.Example` can confirm.

## Open Questions

- Whether to run a spike in `Terp.Hosting.Example` that confirms a generic wrapper around `Bind` is not intercepted by the binding generator. Not required for this change; the docs only state the supported call-site pattern.
