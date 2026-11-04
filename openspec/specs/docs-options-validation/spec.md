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
