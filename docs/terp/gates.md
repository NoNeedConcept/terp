---
title: Startup gates
description: Readiness checks that must succeed before the host reports started, and how they are retried and logged.
---

# Startup gates

A startup gate is a readiness check that must succeed before the host finishes starting, for example "the database accepts connections". Gates live in `Terp.Hosting.Gates` (package `Terp`).

```csharp
using Terp.Hosting.Gates;
```

## Define a gate

Implement `IStartupGate`. `CheckAsync` returns `true` when the gate is clear and `false` when it should be checked again later.

```csharp
public sealed class DatabaseGate(MyDb db) : IStartupGate
{
    public async Task<bool> CheckAsync(CancellationToken cancellationToken) =>
        await db.PingAsync(cancellationToken);
}
```

`Name` is optional. It identifies the gate in logs and exception messages and defaults to the name of the implementing type.

## Register a gate

Register through the service collection. There is no builder-level overload and there are no options.

```csharp
builder.Services.AddStartupGate<DatabaseGate>();
builder.Services.AddStartupGate("cache", ct => cache.PingAsync(ct));   // delegate gate with a name
```

`AddStartupGate<TGate>()` registers the type as a singleton, so its constructor dependencies come from DI. A scoped service cannot be a gate. Registering the same gate type twice adds it once. `AddStartupGate(name, check)` wraps the delegate in a gate called `name`; a blank name throws an `ArgumentException`.

Both calls also install the one hosted service that evaluates the gates. You can register gates inside any setup:

```csharp
internal sealed class GatesSetup : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context) =>
        services.AddStartupGate<DatabaseGate>();
}
```

::: warning Instance registration alone does nothing
`AddSingleton<IStartupGate>(instance)` registers a gate but does not install the hosted service that evaluates it. Combine it with at least one `AddStartupGate` call.
:::

## When gates run

The gate service implements `IHostedLifecycleService` and checks the gates in `StartingAsync`. That runs before the `StartAsync` of every hosted service, so before Kestrel and before the Akka system start. The host does not report started until all gates are clear.

::: info Only StartAsync is guaranteed
The ordering guarantee is relative to `StartAsync`. Another service's `StartingAsync` can run before or after the gates, depending on registration order.
:::

## Timing

The timing is fixed in the code and cannot be configured.

| Constant | Value |
|----------|-------|
| First check | immediately |
| Initial retry delay | 1 second |
| Delay growth | doubles per attempt |
| Maximum retry delay | 30 seconds |
| Time limit for one check | 10 seconds |

All open gates are checked in parallel. After a round with open gates the service waits (1 s, 2 s, 4 s, 8 s, 16 s, then 30 s) and checks the open gates again. A gate that returned `true` is not checked again. The delays come from `Terp.Resilience.Backoff` without jitter; see [Resilience](/utilities/resilience).

## What a gate may do

- Return `false`: the gate stays open and is retried.
- Throw an exception: logged as a warning with the gate name, treated as not clear, retried.
- Exceed 10 seconds: the token passed to `CheckAsync` is cancelled, a warning is logged and the gate counts as not clear. It is retried.
- Ignore the token: the service stops waiting for the task when the token is cancelled, either by the 10 second limit or by host start cancellation. The abandoned task keeps running in the background, so a gate should still honour the token and must not block synchronously.

## How the wait ends

The wait ends when every gate is clear. It also ends when the host start token is cancelled, for example by Ctrl+C or by `HostOptions.StartupTimeout`. The service then throws an `OperationCanceledException` whose message names the gates still open:

```text
Host startup was cancelled while startup gates were still open: DatabaseGate, cache.
```

For a slow dependency, set `HostOptions.StartupTimeout` to the longest wait you accept.

## Logging

The service logs through `ILogger<StartupGateHostedService>`; the category is `Terp.Hosting.Gates.StartupGateHostedService`.

| Level | Message |
|-------|---------|
| Warning | `Startup gates still open: {Gates}. Next retry in {Delay}.` with the names of the open gates only |
| Warning | `Startup gate {Gate} threw and is treated as not clear.` (with the exception) |
| Warning | `Startup gate {Gate} timed out after {Timeout} and is treated as not clear.` |
| Debug | `All startup gates are cleared.` |

::: info No logger registered
The logger is optional. Without logging services the gates still run and nothing is written.
:::

## Example

`src/Terp.Hosting.Example` registers a gate that needs two attempts. The worker starts only after the second check:

```csharp
var gate = new WarmupGate(requiredAttempts: 2);
services.AddSingleton(gate);
services.AddStartupGate("warmup", gate.CheckAsync);
```
