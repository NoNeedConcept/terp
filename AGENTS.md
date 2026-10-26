# AGENTS.md

## Project

Terp – a monorepo of small .NET foundation libraries by [NoNeedConcept](https://github.com/NoNeedConcept).
Two NuGet packages (`Terp`, `Terp.Akka`), independently versioned, released via release-please.
Documentation at [terp.noneedconcept.org](https://terp.noneedconcept.org).

## Solution structure

```
src/
  Terp.slnx
  Terp/                      # Foundation helpers incl. host setups and startup gates (NuGet package, folder Hosting = namespace Terp.Hosting)
  Terp.Akka/                 # Akka.NET helpers on Akka.Hosting (NuGet package)
  Terp.Tests/ Terp.Akka.Tests/  # Unit tests
  Terp.API.Tests/            # Public-API snapshot tests (Verify + PublicApiGenerator)
  Terp.Example/ Terp.Hosting.Example/ Terp.Akka.Example/  # Runnable demo apps (IsPackable=false, not shipped); Terp.Hosting.Example requires Native AOT (PublishAot)
```

Namespaces: `Terp` has `Terp.Diagnostics` (metrics helpers: `TimerScope`, `TimeAsync`/`HistogramTimerExtensions`, `TerpHttpMetricsHandler`), `Terp.Resilience`, `Terp.Network`,
`Terp.Threading` (`AsyncLock`, `AsyncLockScope`),
`Terp.Hosting` (`ISetup`, `ISetup<T>`, `SetupContext`, `SetupExtensions`, `ApplicationSetupExtensions`) and `Terp.Hosting.Gates` (`IStartupGate`, `AddStartupGate`).
A typical host needs `using Terp.Hosting;` plus `Terp.Hosting.Gates` at most.
`Terp.Akka` holds the Akka setup, actor helpers and `EntityRegionActor` (child-per-entity routing with passivation, `Passivate`, `IEntityMessageExtractor`, `WithEntityRegion<TKey>`).
There are exactly two packages (`Terp` <- `Terp.Akka`); new functionality goes into one of them.

## Build & test

All commands run from repo root:

```powershell
dotnet build src/Terp.slnx   # add -p:ContinuousIntegrationBuild=true to treat warnings as errors like CI
dotnet test --solution Terp.slnx   # run from src/ (global.json selects Microsoft.Testing.Platform)
```

Always use the solution file, never individual csproj files.

## Guardrails

- **Central Package Management.** All package versions live in
  `src/Directory.Packages.props`. Do not add `Version=` attributes in csproj files.
- **Independent versions per package.** release-please runs in manifest mode with one release PR per package (`separate-pull-requests`). Versions live only in `.release-please-manifest.json`; CI passes them to the build as `/p:TerpVersion=...` / `/p:TerpAkkaVersion=...`, which `src/Directory.Build.props` maps to each project. Never write a version into a csproj or props file; a new package needs one mapping block there.
- **Dependency-light.** `Terp` depends on the Microsoft.Extensions
  *abstractions* only (Hosting.Abstractions, Logging.Abstractions; DI.Abstractions
  transitively). No ASP.NET Core reference, no `FrameworkReference`, no concrete
  Microsoft.Extensions.Hosting in library code (it is a test-only package). Do not add
  NuGet packages without good reason; these are libraries consumed by others.
- **Hosting.** Startup gates are registered only via `services.AddStartupGate...` or `AddSingleton<IStartupGate>`; there is no builder-level overload, no options and no lifecycle-hook API (use `IHostApplicationLifetime` / `IHostedLifecycleService`). `ActorSystemSetup` allows one actor system per host.
- **Conventional Commits** are enforced by commitlint.
- **Minimum versions.** Library dependencies in `src/Directory.Packages.props` use
  `[x.y.z,)` floors; Renovate must not raise them. Raise a floor only when the code
  needs a newer API. A `ProjectReference` between Terp packages packs as `>=` the
  referenced version. That version is evaluated at pack time of the dependent package (injected from the manifest), already published versions are
  never changed, and a release must publish dependencies first (`release.yml` pushes in the order Terp, then Terp.Akka).
- **Commit scope decides the release.** release-please attributes commits by path
  (`src/Terp`, `src/Terp.Akka`); changes only in shared files such as `src/Directory.*.props` trigger no release.
- **API snapshot tests.** `src/Terp.API.Tests` snapshots the public API of `Terp` and
  `Terp.Akka` in `verify/*.verified.txt`. If an API test fails after an intentional public
  API change, review the diff and update the snapshot (rename the `.received.` file to
  `.verified.`). API-breaking changes need a `feat!:` / `BREAKING CHANGE` commit so
  release-please bumps the major version. Never commit `.received.` files.

## Setups (namespace Terp.Hosting, in Terp)

- `ISetup` is a marker; `ISetup<in TTarget>` has one method, always `Setup(TTarget target, SetupContext context)`. A class implements it once per target.
  The target is what may be changed, `SetupContext` is read-only (`Configuration`, `Environment`). Classes are named `XxxSetup`.
- Which `ISetup<T>` for what: `IConfigurationManager` (configuration sources), `ILoggingBuilder`, `IServiceCollection`, `IMetricsBuilder`,
  `IHostApplicationBuilder` / `WebApplicationBuilder` (full builder access, explicit exception), `WebApplication` / any `IHost` (after `Build()`, `app.Setup(...)`).
- Ordering: `builder.Setup(s)` runs immediately, in call order. Inside one setup the targets run in a fixed internal order: configuration, logging, services, metrics, builder.
  There are no public phase numbers, no discovery (`SetupAll*`, source generator) and no `[SetupOrder]`/`[SetupIgnore]`; setups are listed explicitly. Discovery, a generator and
  the target registry (`AddSetupTarget`) exist on branch `archive/generator` and can return if a real need appears.
- Dispatch is pure pattern matching (`setup is ISetup<IServiceCollection>` ...; the builder target is `ISetup<TBuilder>` with the static receiver type, so contravariance makes
  `ISetup<IHostApplicationBuilder>` fit every builder). It is reflection-free and trim/AOT-safe; do not add reflection. `Terp.Hosting.Example` publishes with Native AOT and 0 warnings to prove it.
  A setup matching no target throws `InvalidOperationException`; a mismatching extra builder target next to a matching one cannot be detected without reflection and is ignored (tested).
- Startup gates (`Terp.Hosting.Gates`): `IStartupGate`, `AddStartupGate<T>()`, `AddStartupGate(name, check)`; one internal hosted service polls the open gates in `StartingAsync` with `Terp.Resilience.Backoff`
  and fixed constants (1 s initial, 30 s max, 10 s per check). No options.
- `ActorSystemSetup` (Terp.Akka, abstract, `ISetup<IServiceCollection>`) owns the one actor system: abstract `ActorSystemName` (validated eagerly) and `BuildSystem(AkkaConfigurationBuilder, IServiceProvider)`.
  Domain actors are added in `BuildSystem`; there are no contributor setups.

## C# conventions

`.editorconfig` is authoritative; violations are build warnings, and CI (`GITHUB_ACTIONS` /
`ContinuousIntegrationBuild`) turns all warnings into errors. Fix warnings locally with
`dotnet format Terp.slnx` and check with `dotnet format Terp.slnx --verify-no-changes`.

- File-scoped namespaces; the namespace matches the folder. One public type per file, file name = type name.
- Types are `sealed` by default (CA1852); helpers without state are `static` classes (CA1052); the namespace matches the folder (IDE0130).
- Always braces, Allman style, 4 spaces, LF, UTF-8 without BOM.
- Private fields are `_camelCase`; constants are PascalCase.
- Primary constructors only for pure forwarders.
- DTOs and options are immutable records / `init` properties; a mutable class only for `IOptions`-bound configuration.
- `ArgumentNullException.ThrowIfNull` on all public parameters. Nullable stays on; `!` only with a justification.
- XML docs on all public API (`summary`, `param`, `exception`); `<inheritdoc />` for implementations.
- No `async void`, `.Result` or `.Wait()`; `CancellationToken` is the last parameter and is passed through.
- `ConfigureAwait(false)` in library code (CA2007), not in tests or examples.
- Prefer collection expressions (`[]`).
- Tests: class `XxxTests`, `public sealed`, file name = class name; methods named `Method_Scenario_ExpectedResult` (PascalCase, FunkArr style, e.g. `PostResourceAsync_SuccessResponse_ReturnsIdAndName`);
  async tests use `TestContext.Current.CancellationToken`. API snapshot classes are `CoreApiTests` / `AkkaApiTests` (abbreviations longer than two letters use .NET casing).
- No comments that restate the code.
- Warnings are errors in CI. Suppress only in `.editorconfig`, with a reason, never with `#pragma`.
- Do not add analyzer NuGet packages; the SDK analyzers (`AnalysisLevel` `latest-recommended`) are enough.
- `TimeProvider` everywhere; never `DateTime.Now` or `DateTime.UtcNow` directly.
- Enum-to-string: explicit `switch` with `ArgumentOutOfRangeException` default, not `ToString().ToLower()`.

## Akka conventions

Adopted from FunkArr and Njord; apply when writing actors in `Terp.Akka` or consumer projects.

- **No `ContinueWith`** — use `PipeTo` with `success:` and `failure:` lambdas.
- **No `Status.Failure`** — use project-owned `XxxFailed(Exception Cause)` records.
- **No `EventStream`** — use explicit actor references; `EventStream.Subscribe` only in tests.
- **No `IActorRef` constructor params** — resolve via `Context.GetActor<T>()` / `TryGetActor<T>()`.
- **Actor naming**: `*Manager` (singleton/coordinator), `*Worker` (per-entity/per-request), `*Actor` (everything else).
- **Actor state**: separate `<Actor>State.cs` file with immutable record, `Apply(event)` returns new state; actor is thin plumbing.
  State is never sent raw — use `GetSnapshot()` → response record.
- **Persistence**: `Persist(evt, callback)` for journal writes; `SaveSnapshot` interval-based (`LastSequenceNr % N == 0`), never after
  every event; snapshot type is `Persisted*State`, not raw state.
- **Passivation**: entity sends `Context.Parent.Tell(Passivate.Instance)` (or `Passivate(customStopMessage)`) to its parent region.

## Message naming

- Commands: `VerbNoun` with `VerbNounCompleted` / `VerbNounFailed` response pair, one file.
- Queries: `QueryNoun` with `NounResult` / `NounFailed` response pair, one file.
- Events: past tense (`OrderPlaced`, `ItemShipped`), in the persistence project.
- `*Failed` always carries `Exception Cause`.

## Metrics conventions

- One `static` class `Telemetry` per domain with `internal static readonly Meter` and instruments.
- Meter name: `<Project>.<Domain>` (e.g. `FunkArr.Search`). Instrument names: `snake_case`.
- Tags: short lowercase, bounded cardinality — never unbounded IDs.
- Instrument at the operation boundary (where it completes), not at the call site.
- Use `histogram.TimeAsync(...)` for async timing; `histogram.StartTimer(...)` for sync or cross-message timing.

## Test assertion conventions

- No `!.` on nullable access in tests: `Assert.NotNull(value)` first, then access members.
- `var item = Assert.Single(collection)` instead of `collection[0]` for single-element collections; assert the count before indexing several elements.
- Assert the exact computed value (content, count, text), not just non-null/non-empty; no vacuous assertion after a value is already proven.
- Exceptions: `var ex = Assert.Throws<T>(...)` / `await Assert.ThrowsAsync<T>(...)`, then assert `ex.ParamName` / `ex.Message`, not only the type.
- `Assert.Multiple` for grouped assertions on the same object; `Assert.Equal(expected, actual)` instead of `Assert.True(a == b)`.
- `Assert.IsType<T>(obj, exactMatch: false)` for interface/base-type conformance instead of an unchecked cast.
- No `Thread.Sleep` or arbitrary delay to wait for async state: use a bounded wait with a timeout (`TaskCompletionSource` + `WaitAsync`, `AwaitAssert`, `ExpectMsg`).
- Prefer `Theory` for small input/output sweeps; separate `[Fact]`s when each case needs a large distinct fixture.
- `FakeTimeProvider` or fixed timestamp in time-dependent tests, never `DateTime.UtcNow`.
- Actor tests: collaborators as `CreateTestProbe()` registered via `ActorRegistry.For(Sys).Register<T>(probe)`.
- State records are tested as plain unit tests, separate from actor message-routing tests.

## CI

- `TreatWarningsAsErrors` is enabled in `src/Directory.Build.props` when `ContinuousIntegrationBuild` is `true`
  (set automatically on GitHub Actions). Reproduce locally with `-p:ContinuousIntegrationBuild=true`
  (use `-p:`, not `/p:`, in Git Bash).
- `EnforceCodeStyleInBuild` is on, so code-style rules from `.editorconfig` (IDE*) fail the CI build too.
- Test and example projects also generate the XML doc file (needed for IDE0005); CS1591 is silenced there in `.editorconfig`.

## Release setup (one-time)

Publishing uses NuGet trusted publishing (OIDC), the same as Signal.Bot and WebSocket.Rx. The publish job lives in `release.yml` itself
(the OIDC `job_workflow_ref` must match the policy), so no API key is stored.

1. GitHub: create the environment `nuget` (Settings, Environments) and the repository secret `NUGET_USER` (the NuGet.org user name that owns the policy).
2. NuGet.org: create a trusted publishing policy for the owner account with repository `st0o0/terp`, workflow `release.yml`, environment `nuget`.
   Do this for both package IDs `Terp` and `Terp.Akka`. If NuGet.org does not allow a policy before the package ID exists, the first push of each package needs a
   one-off API key instead: add a repository secret `NUGET_API_KEY` (scope Push, glob `Terp*`) and temporarily pass it as `--api-key` in the `Push released packages to NuGet`
   step in place of the `NuGet/login` output; switch back once the policies exist.
3. GitHub: allow Actions to create pull requests (Settings, Actions, General, Workflow permissions) so release-please can open its PRs.
4. If `main` is protected, the release-please PRs must be mergeable by you (required checks `dotnet` and `commits` run on them like on any PR).

### First release (0.1.0)

`release-please-config.json` pins `"release-as": "0.1.0"` on both packages, so the first release PRs propose 0.1.0 regardless of the commit history.

1. Merge the `Terp` release PR first. The `versions` job in `release.yml` fails if `Terp.Akka` is released while `Terp` is still `0.0.0` in the manifest,
   because `Terp.Akka` packs `Terp >= <manifest version>`.
2. Then merge the `Terp.Akka` release PR.
3. Afterwards remove both `"release-as"` lines from `release-please-config.json` (commit `ci: remove release-as after first release`). Otherwise release-please keeps proposing 0.1.0.

Symbols are embedded in the assemblies (`DebugType` embedded, SourceLink via `PublishRepositoryUrl`/`EmbedUntrackedSources`), so there is no separate `.snupkg`.
The package README is the root `README.md`, packed by `src/Directory.Build.props` for every packable project.

## Brand

Colour: Mud `#C98B2B`, Mud deep `#8F5F14`, Ink `#1A1813`, Paper `#F3EFE6`.
Type: Geist (headings/body), Geist Mono (code). Both bundled with the docs site.
The full CI design kit (mark, NuGet icons, favicons, README headers, social preview) lives in `docs/brand/`;
see `docs/brand/BRAND.md` for usage rules. Each NuGet package has its own icon in its project directory.
