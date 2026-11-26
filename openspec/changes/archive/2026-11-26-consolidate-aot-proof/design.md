## Context

Terp has two example projects exercising the `Terp` package:

- **`Terp.Example`** — top-level statements, no host, demos `TimerScope`, `TimeAsync`, `Backoff`, `PortFinder`, `TerpHttpMetricsHandler`, `DateTimeExtensions`. Uses `HttpListener` (not AOT-safe on Linux). No `PublishAot`.
- **`Terp.Hosting.Example`** — hosted app with `PublishAot=true`, demos `ISetup<T>`, startup gates, metrics listener. This is the only AOT proof; CI publishes it in `extra-ci.yml`.

`IsAotCompatible=true` and `IsTrimmable=true` are set globally in `Directory.Build.props`, but `Terp.Akka` depends on Akka.NET which is not trim/AOT-safe, causing IL2091 errors.

## Goals / Non-Goals

**Goals:**
- One `Terp.Example` that exercises **every public Terp API** as a hosted app with `PublishAot=true`
- Correctly scope `IsAotCompatible` / `IsTrimmable` to `Terp` only
- Keep `Terp.Akka` annotations (`[DynamicallyAccessedMembers]`) as forward preparation
- CI validates AOT via the consolidated example

**Non-Goals:**
- Making `Terp.Akka` AOT-compatible (blocked on Akka.NET upstream)
- Adding ASP.NET Core or Kestrel to the example (dependency-light principle)
- Changing any public API

## Decisions

### 1. HttpListener replacement for TerpHttpMetricsHandler demo

**Problem:** `HttpListener` is not AOT-safe on Linux (`PlatformNotSupportedException`). The old example used it to show `TerpHttpMetricsHandler` enrichment tags.

**Decision:** Use `SocketsHttpHandler` with a raw TCP listener (`TcpListener`) that returns a minimal HTTP 200 response. `TcpListener` and `Socket` are fully AOT-compatible. This is a demo app, not production code — a 3-line TCP responder is perfectly adequate to demonstrate that the handler adds `api` and `status_class` tags.

**Alternative considered:** Skip the HTTP demo entirely. Rejected because we want to exercise *all* Terp APIs cleanly in the example.

### 2. Example as hosted BackgroundService

**Decision:** The example is a hosted app (`Host.CreateApplicationBuilder`) with a single `BackgroundService` that runs all demos sequentially then calls `StopApplication()`. This follows the existing `Terp.Hosting.Example` pattern.

Setups exercise:
- `ISetup<IConfigurationManager>` — in-memory config
- `ISetup<ILoggingBuilder>` — console logging
- `ISetup<IServiceCollection>` — singletons, startup gate
- `ISetup<IMetricsBuilder>` — metrics listener
- `ISetup<IHostApplicationBuilder>` — hosted service registration

The worker then demos the non-hosting APIs: `TimerScope`, `TimeAsync`, `Counter.Up/Down`, `Backoff`, `PortFinder`, `TerpHttpMetricsHandler`, `AsyncLock`, `DateTimeExtensions`, `ValidationFailures`.

### 3. IsAotCompatible / IsTrimmable scoping

**Decision:** Remove from `Directory.Build.props`, add to `Terp.csproj` only. `Terp.Akka.csproj` gets nothing (default `false`). Test and example projects inherit nothing — the example sets its own `PublishAot=true` and `IsAotCompatible=true`.

### 4. InvariantGlobalization

**Decision:** Keep `InvariantGlobalization=true` on `Terp.Example` as the old `Terp.Hosting.Example` had it. This is standard for minimal AOT apps and reduces binary size.

## Risks / Trade-offs

- **[TcpListener demo is low-fidelity]** → Acceptable for a demo; the real handler behaviour is proven by unit tests.
- **[Terp.Akka loses trimmer analysis warnings]** → The `[DynamicallyAccessedMembers]` annotation stays, and unit tests cover the DI registration. When Akka.NET adds trim support, re-enable `IsAotCompatible` on `Terp.Akka` and the warnings will re-emerge if needed.
- **[Removing Terp.Hosting.Example may break doc links]** → Check `docs/` for references and update. The AGENTS.md solution structure section must be updated.
