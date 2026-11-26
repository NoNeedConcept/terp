## ADDED Requirements

### Requirement: Consolidated Terp.Example exercises all Terp public APIs

The `Terp.Example` project SHALL be a hosted console application (`Host.CreateApplicationBuilder`) that demonstrates every public API namespace in the `Terp` package. It SHALL reference only `Terp` and `Microsoft.Extensions.Hosting`.

#### Scenario: Example covers Terp.Hosting setups
- **WHEN** the example application starts
- **THEN** it uses `builder.Setups().Add<T>()` to register at least one `ISetup<T>` implementation that targets `IConfigurationManager`, `ILoggingBuilder`, `IServiceCollection`, `IMetricsBuilder`, and `IHostApplicationBuilder`

#### Scenario: Example covers startup gates
- **WHEN** the example application starts
- **THEN** it registers at least one startup gate via `services.AddStartupGate(name, check)` that simulates a dependency becoming ready after multiple attempts

#### Scenario: Example covers Terp.Diagnostics
- **WHEN** the example worker runs
- **THEN** it demonstrates `TimerScope` (via `histogram.StartTimer`), `TimeAsync`, and `Counter.Up`/`Counter.Down` extensions

#### Scenario: Example covers TerpHttpMetricsHandler
- **WHEN** the example worker runs
- **THEN** it sends an HTTP request through a `TerpHttpMetricsHandler` and the `api` and `status_class` tags appear on the BCL `http.client.request.duration` metric

#### Scenario: Example covers Terp.Network
- **WHEN** the example worker runs
- **THEN** it calls `PortFinder.FindFreeLocalPort()` and uses the returned port

#### Scenario: Example covers Terp.Resilience
- **WHEN** the example worker runs
- **THEN** it creates a `Backoff` and prints computed delays for multiple attempts

#### Scenario: Example covers Terp.Threading
- **WHEN** the example worker runs
- **THEN** it acquires and releases an `AsyncLock` via `AcquireAsync`

#### Scenario: Example covers DateTimeExtensions
- **WHEN** the example worker runs
- **THEN** it calls `IsWorkday()`, `IsWeekend()`, and `IsInFuture()` on a `DateTime` value

### Requirement: Terp.Example publishes with Native AOT

The `Terp.Example` project SHALL set `PublishAot=true` and `IsAotCompatible=true` in its csproj. It SHALL compile and publish with zero ILC trim/AOT warnings when `ContinuousIntegrationBuild=true`.

#### Scenario: CI validates AOT publish
- **WHEN** `extra-ci.yml` runs on a pull request
- **THEN** it executes `dotnet publish src/Terp.Example/Terp.Example.csproj -c Release -p:ContinuousIntegrationBuild=true` and the step succeeds with zero warnings

#### Scenario: Example uses InvariantGlobalization
- **WHEN** the example is published
- **THEN** `InvariantGlobalization=true` is set in the csproj to minimise binary size

### Requirement: IsAotCompatible scoped to Terp package only

`IsAotCompatible=true` and `IsTrimmable=true` SHALL be declared in `Terp.csproj`, not in `Directory.Build.props`. `Terp.Akka` SHALL NOT declare `IsAotCompatible` or `IsTrimmable` (defaults to `false`).

#### Scenario: Terp.Akka builds without IL20xx errors
- **WHEN** `dotnet build src/Terp.slnx -p:ContinuousIntegrationBuild=true` runs
- **THEN** the build succeeds with zero IL20xx trimmer analysis errors from `Terp.Akka`

#### Scenario: Terp.Akka retains DynamicallyAccessedMembers annotations
- **WHEN** `AddActorRegistration<T>` is inspected
- **THEN** the `T` parameter carries `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]` as forward preparation for Akka.NET AOT support

### Requirement: Terp.Hosting.Example is removed

The `Terp.Hosting.Example` project SHALL be deleted. All references in `Terp.slnx`, `extra-ci.yml`, `AGENTS.md`, and documentation SHALL be removed or updated.

#### Scenario: Solution no longer contains Terp.Hosting.Example
- **WHEN** `Terp.slnx` is loaded
- **THEN** it does not reference `Terp.Hosting.Example`

#### Scenario: Documentation reflects new structure
- **WHEN** `AGENTS.md` is read
- **THEN** it lists `Terp.Example` as the AOT proof and does not mention `Terp.Hosting.Example`
