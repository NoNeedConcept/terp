## 1. Dependency

- [x] 1.1 Add `PackageVersion Include="Microsoft.Extensions.Options" Version="[10.0.0,)"` to `src/Directory.Packages.props` (floor, no Renovate bump)
- [x] 1.2 Add `<PackageReference Include="Microsoft.Extensions.Options" />` to `src/Terp/Terp.csproj` (no `Version=` attribute)

## 2. ValidationFailures

- [x] 2.1 Create `src/Terp/Options/ValidationFailures.cs` (namespace `Terp.Options`, `sealed`, XML docs on all members, `ConfigureAwait` not applicable) with constructor `(string? section = null)`, `Section`, `Count`, `Add`, `Require`, `ToResult`
- [x] 2.2 Implement the section prefix (`"{Section}:{message}"`, none for null/empty) and message validation (`ArgumentNullException.ThrowIfNull`, whitespace rejected via `ArgumentException.ThrowIfNullOrWhiteSpace`, also for a satisfied `Require`)
- [x] 2.3 Implement `ToResult()` returning `ValidateOptionsResult.Success` when empty, otherwise `ValidateOptionsResult.Fail` with failures in insertion order

## 3. Tests

- [x] 3.1 Create `src/Terp.Tests/Options/ValidationFailuresTests.cs` (`public sealed`, `Method_Scenario_ExpectedResult` names) covering: empty collector, prefix with and without section, `Add` count, null and whitespace message exceptions with `ParamName`, `Require` satisfied and violated, `Require(true, null)` throws, `ToResult` success, `ToResult` failures in order
- [x] 3.2 Use `Assert.Throws<T>` and assert `ParamName`, `Assert.Equal` on the exact failure list, no vacuous assertions

## 4. API snapshot

- [x] 4.1 Run the API tests, review the diff, rename the `.received.` file to `.verified.` for `CoreApiTests`, and confirm only `Terp.Options.ValidationFailures` was added
- [x] 4.2 Make sure no `.received.` file is left in the working tree

## 5. Docs

- [x] 5.1 Create `docs/terp/options.md` ("Options with setups", English, front matter like neighbouring pages) with the `AddOptionsWithValidateOnStart<T, V>().Bind(...)` pattern inside an `ISetup<IServiceCollection>`
- [x] 5.2 Add the AOT section: `EnableConfigurationBindingGenerator`, why `Bind` stays at the call site, `SYSLIB1104` for generic wrappers
- [x] 5.3 Add the validator-class section with a `ValidationFailures` example (`Require`, `ToResult`) and the "validator class is the standard, not inline" rule
- [x] 5.4 Add the array-defaults binder pitfall as a documented known behavior, no Terp feature
- [x] 5.5 Add the sidebar entry "Options" (`/terp/options`) to the `Terp` group in `docs/.vitepress/config.ts`
- [x] 5.6 Build the docs (`pnpm` in `docs/`) and check that the page and the sidebar entry render

## 6. Migration guide

- [x] 6.1 Create `Migration.md` at the repository root with the single rule: validator class next to the options type, never inline
- [x] 6.2 Add the Njord section: `AddNjordOptions<T, V>()` to `AddOptionsWithValidateOnStart<T, V>().Bind(...)` before/after, and the list of things that stay in Njord (`IConfigSection`, `IHasArrayDefaults`)
- [x] 6.3 Add the FunkArr section: inline `.Validate(...)` chain in `FeedSetupContainer` to a `FeedOptionsValidator` using `ValidationFailures`, and `ValidateOnStart` for all options (including `PostgresOptions`) through the `AddOptionsWithValidateOnStart` overload
- [x] 6.4 Add the validator boilerplate before/after (`List<string>` plus closing ternary to `ValidationFailures` plus `ToResult()`)

## 7. Verify

- [x] 7.1 `dotnet build src/Terp.slnx -p:ContinuousIntegrationBuild=true` passes without warnings
- [x] 7.2 `dotnet test --solution Terp.slnx` (from `src/`) passes
- [x] 7.3 `dotnet format Terp.slnx --verify-no-changes` passes
- [x] 7.4 Publish `Terp.Hosting.Example` with Native AOT and confirm 0 warnings (no new trim/AOT warnings from `Terp`) (verified in CI: `extra-ci.yml` aot-publish on PR #22, 0 warnings; the local publish failed only at the native link step because `vswhere.exe` is missing)
- [x] 7.5 Commit with a Conventional Commit (`feat(terp): add ValidationFailures for options validators`), single-line message, only when the user asks for it (committed as ff388b6)
