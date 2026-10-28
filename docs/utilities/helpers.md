---
title: Helpers
description: Small utilities in the Terp package — PortFinder, DateTimeExtensions and AsyncLock.
---

# Helpers

Small, self-contained utilities in the `Terp` package.

## PortFinder

`Terp.Network` contains one helper, `PortFinder`.

```csharp
using Terp.Network;
```

`PortFinder.FindFreeLocalPort()` binds a TCP socket to port 0 on all IPv4 interfaces, reads the port the operating system assigned, closes the socket and returns the port number.

```csharp
var port = PortFinder.FindFreeLocalPort();
var prefix = $"http://localhost:{port}/";
```

This is useful in tests that start a local server, as `src/Terp.Example` does with an `HttpListener`.

::: warning The port is free only for now
The socket is closed before the method returns. Another process can take the port before you bind it. Bind soon, and retry with a new port if binding fails.
:::

The method checks IPv4 only (`AddressFamily.InterNetwork`).

## DateTimeExtensions

`DateTimeExtensions` in the namespace `Terp` adds predicates to `DateTime`.

```csharp
using Terp;
```

| Method | Result |
|--------|--------|
| `IsToday(timeProvider?)` | the value falls on the current local calendar day of the provider's time zone |
| `IsInFuture(timeProvider?)` | the value is later than now |
| `IsPast(timeProvider?)` | the value is earlier than now |
| `IsBetween(lower, upper)` | the value lies within the inclusive range; the bounds may come in either order |
| `IsWeekend()` | Saturday or Sunday |
| `IsWorkday()` | not `IsWeekend()` |

`IsWorkday` knows weekends only. It does not know public holidays.

### Time source

Methods that need the current time take an optional `TimeProvider`. The default is `TimeProvider.System`. Pass a fake provider in tests.

```csharp
var provider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero));   // Microsoft.Extensions.TimeProvider.Testing

var due = new DateTime(2026, 3, 2, 18, 0, 0, DateTimeKind.Utc);
var soon = due.IsInFuture(provider);
```

### DateTimeKind

The methods that compare with the current time honour `DateTime.Kind`:

| Kind | Interpreted as |
|------|----------------|
| `Utc` | UTC |
| `Local` | local time of the system (`ToUniversalTime()`) |
| `Unspecified` | local time of `timeProvider.LocalTimeZone` |

`IsInFuture` and `IsPast` convert the value to UTC using these rules and compare it with `GetUtcNow()`. Both comparisons are strict, so a value equal to now is neither in the future nor in the past.

`IsToday` converts `Utc` values to the provider's local time zone. It compares other kinds as wall-clock time. It then compares the date with `GetLocalNow().Date`.

::: info Unspecified values
A `DateTime` read from a database often has kind `Unspecified`. Terp treats it as local time of the provider's time zone. Set the kind when you know it is UTC.
:::

`IsBetween`, `IsWeekend` and `IsWorkday` look at the value as given and ignore the kind.

```csharp
var start = new DateTime(2026, 3, 1);
var end = new DateTime(2026, 3, 31);
var inMarch = new DateTime(2026, 3, 15).IsBetween(end, start);   // true, bounds in either order
var weekend = new DateTime(2026, 3, 7).IsWeekend();             // true, a Saturday
```

## AsyncLock

`Terp.Threading` provides `AsyncLock`, a scoped asynchronous mutual exclusion primitive built on `SemaphoreSlim(1, 1)`.

```csharp
using Terp.Threading;
```

`AsyncLock` wraps a `SemaphoreSlim` in a `using`-friendly scope. Only one caller holds the lock at a time.

```csharp
private readonly AsyncLock _lock = new();

public async Task UpdateAsync(CancellationToken ct)
{
    using var scope = await _lock.AcquireAsync(ct);
    // critical section — only one caller at a time
    await SaveAsync(ct);
}
```

`AcquireAsync` returns a `ValueTask<AsyncLockScope>`. When the lock is free it completes synchronously without allocating a `Task`. The returned `AsyncLockScope` is a readonly struct; disposing it releases the lock.

### Cancellation

Pass a `CancellationToken` to `AcquireAsync`. If the token is cancelled while waiting, it throws `OperationCanceledException`.

### Disposal

`AsyncLock` implements `IDisposable`. After disposal, `AcquireAsync` throws `ObjectDisposedException`.

### Not reentrant

`AsyncLock` is not reentrant. Calling `AcquireAsync` twice on the same async call chain deadlocks. This matches `SemaphoreSlim(1, 1)` behaviour and is by design.

For actor-based code, prefer Akka's single-threaded actor model instead. For retry timing, see [Resilience](/utilities/resilience).
