# docs-options-validation Specification

## Purpose
TBD - created by archiving change add-options-validation-helpers. Update Purpose after archive.
## Requirements

### Requirement: Options docs page

The documentation site SHALL contain the page `docs/terp/options.md`, titled "Options with setups", linked from the `Terp` group of the sidebar in `docs/.vitepress/config.ts`.

The page MUST cover:
- the recommended registration `services.AddOptionsWithValidateOnStart<TOptions, TValidator>().Bind(section)` inside an `ISetup<IServiceCollection>`;
- why `Bind` stays at the call site for Native AOT (configuration binding source generator, `EnableConfigurationBindingGenerator`, `SYSLIB1104` for generic wrappers);
- the validator class (`IValidateOptions<T>`) as the standard way to validate, with `ValidationFailures` and a code example;
- the array-defaults binder pitfall (binding appends to default array values), documented as a known behavior without a Terp feature.

#### Scenario: Page is reachable from the sidebar
- **WHEN** the docs site is built
- **THEN** the sidebar of the `Terp` group lists "Options" and links to `/terp/options`

#### Scenario: Page documents the AOT-safe pattern
- **WHEN** a reader opens the page
- **THEN** it shows `AddOptionsWithValidateOnStart<T, V>().Bind(...)` and explains why a generic wrapper around `Bind` is not recommended

#### Scenario: Page documents ValidationFailures
- **WHEN** a reader opens the page
- **THEN** it contains a validator example using `ValidationFailures` with `Require` and `ToResult`

### Requirement: Migration guide for consumer projects

The repository root SHALL contain `Migration.md` describing, for Njord and FunkArr, how to move to the recommended options path. It MUST contain before/after examples for:
- `AddNjordOptions<T, TValidator>()` replaced by `AddOptionsWithValidateOnStart<T, TValidator>().Bind(...)` (section name supplied at the call site);
- inline `.Validate(o => ..., "message")` chains replaced by a validator class;
- hand-rolled `List<string>` failure handling replaced by `ValidationFailures`.

It MUST state the single recommended rule: options are validated by an `IValidateOptions<T>` validator class placed next to the options type, never inline, and it MUST list the out-of-scope items that stay in the consuming projects (array defaults, `IConfigSection`).

#### Scenario: Njord registration migration is documented
- **WHEN** a reader looks up `AddNjordOptions` in `Migration.md`
- **THEN** a before/after pair shows the Microsoft overload with `.Bind(...)` as replacement

#### Scenario: Inline validation migration is documented
- **WHEN** a reader looks up inline `.Validate(` in `Migration.md`
- **THEN** a before/after pair shows the same rules in a validator class using `ValidationFailures`

#### Scenario: Single rule is stated
- **WHEN** a reader looks for the validation guideline in `Migration.md`
- **THEN** it states that a validator class is the standard and inline validation is not used
