## 1. Build properties — scope AOT to Terp only

- [x] 1.1 Remove `IsAotCompatible` and `IsTrimmable` from `src/Directory.Build.props`
- [x] 1.2 Add `IsAotCompatible=true` and `IsTrimmable=true` to `src/Terp/Terp.csproj`

## 2. Consolidate Terp.Example

- [x] 2.1 Update `src/Terp.Example/Terp.Example.csproj`: add `PublishAot=true`, `IsAotCompatible=true`, `InvariantGlobalization=true`, add `Microsoft.Extensions.Hosting` package reference
- [x] 2.2 Rewrite `src/Terp.Example/Program.cs` as hosted app with `Host.CreateApplicationBuilder`, `builder.Setups()`, and `host.RunAsync()`
- [x] 2.3 Move and adapt `DemoSetup.cs` from `Terp.Hosting.Example` — setup targets for configuration, logging, services, metrics, builder
- [x] 2.4 Move and adapt `WorkerSetup.cs` — startup gate registration
- [x] 2.5 Move and adapt `WarmupGate.cs`
- [x] 2.6 Move and adapt `AppInfo.cs`
- [x] 2.7 Move and adapt `SetupTrace.cs`
- [x] 2.8 Move and adapt `ConsoleMetricsListener.cs` — extend to also listen for `http.client.request.duration`
- [x] 2.9 Rewrite `DemoWorker.cs` — integrate all Terp API demos: `TimerScope`, `TimeAsync`, `Counter.Up/Down`, `Backoff`, `PortFinder`, `TerpHttpMetricsHandler` (with `TcpListener` responder), `AsyncLock`, `DateTimeExtensions`
- [x] 2.10 Delete `src/Terp.Example/MeterPrinter.cs` (replaced by `ConsoleMetricsListener`)

## 3. Remove Terp.Hosting.Example

- [x] 3.1 Delete `src/Terp.Hosting.Example/` directory
- [x] 3.2 Remove `Terp.Hosting.Example` from `src/Terp.slnx`

## 4. Update CI

- [x] 4.1 Update `extra-ci.yml`: change publish path from `Terp.Hosting.Example` to `Terp.Example`, update comment

## 5. Update documentation

- [x] 5.1 Update `AGENTS.md` solution structure: remove `Terp.Hosting.Example`, note `Terp.Example` is the AOT proof
- [x] 5.2 Check `docs/` for references to `Terp.Hosting.Example` and update

## 6. Verify

- [x] 6.1 `dotnet build src/Terp.slnx -p:ContinuousIntegrationBuild=true` — zero errors, zero warnings
- [x] 6.2 `dotnet test --solution Terp.slnx` from `src/` — all tests pass
- [x] 6.3 `dotnet format src/Terp.slnx --verify-no-changes` — clean
- [x] 6.4 `dotnet publish src/Terp.Example/Terp.Example.csproj -c Release -p:ContinuousIntegrationBuild=true` — zero ILC warnings (Windows; Linux validated by CI)
- [x] 6.5 Run `Terp.Example` and verify console output covers all API demos
