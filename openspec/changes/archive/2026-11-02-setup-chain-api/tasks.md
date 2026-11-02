## 1. Core Implementation

- [x] 1.1 Create `src/Terp/Hosting/SetupChain.cs` — `readonly struct SetupChain<TTarget>` with internal constructor, `Add<TSetup>()` and `Add(ISetup)` methods, dispatch delegate field
- [x] 1.2 Create `src/Terp/Hosting/SetupChainExtensions.cs` — `Setups<TBuilder>(this TBuilder)` and `Setups<TApplication>(this TApplication)` entry points with builder/app dispatch
- [x] 1.3 Delete `src/Terp/Hosting/SetupExtensions.cs`
- [x] 1.4 Delete `src/Terp/Hosting/ApplicationSetupExtensions.cs`
- [x] 1.5 Update XML doc references in `ISetup.cs` and `ISetup.TTarget.cs` to point to new types

## 2. Migrate Call Sites

- [x] 2.1 Migrate `src/Terp.Hosting.Example/Program.cs` — `builder.Setup<X>()` → `builder.Setups().Add<X>()`
- [x] 2.2 Migrate `src/Terp.Akka.Example/Program.cs` — `builder.Setup<X>()` → `builder.Setups().Add<X>()`
- [x] 2.3 Migrate `src/Terp.Akka.Tests/Hosting/ActorSystemSetupTests.cs` — all `builder.Setup(...)` calls
- [x] 2.4 Migrate `src/Terp.Akka.Tests/Hosting/ActorRegistrationExtensionsTests.cs` — all `builder.Setup(...)` calls

## 3. Migrate and Update Tests

- [x] 3.1 Rewrite `src/Terp.Tests/Hosting/SetupExtensionsTests.cs` — test chaining, single Add, instance Add, null checks, return type preservation for both builder and app chains
- [x] 3.2 Rewrite `src/Terp.Tests/Hosting/BuilderAndApplicationSetupTests.cs` — test builder sub-target dispatch and app dispatch via new API
- [x] 3.3 Update `src/Terp.Tests/Hosting/SetupDispatchTests.cs` — migrate dispatch order and error tests to new API

## 4. API Snapshots

- [x] 4.1 Update `src/Terp.API.Tests/verify/*.verified.txt` — removed `SetupExtensions`/`ApplicationSetupExtensions` methods, added `SetupChain<T>` and `SetupChainExtensions`

## 5. Verification

- [x] 5.1 `dotnet build src/Terp.slnx -p:ContinuousIntegrationBuild=true` — zero warnings
- [x] 5.2 `dotnet test --solution Terp.slnx` (from `src/`) — all tests pass
- [x] 5.3 `dotnet format src/Terp.slnx --verify-no-changes` — no formatting issues
- [x] 5.4 Verify `Terp.Hosting.Example` AOT publish produces 0 trim warnings (IL compilation clean, native linker fails due to local vswhere.exe issue — not a code problem)
