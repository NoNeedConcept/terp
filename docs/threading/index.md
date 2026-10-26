---
title: Threading
description: AsyncLock provides scoped asynchronous mutual exclusion over a SemaphoreSlim.
---

# Threading

`Terp.Threading` (package `Terp`) provides `AsyncLock`, a scoped asynchronous mutual exclusion primitive built on `SemaphoreSlim(1, 1)`.

```csharp
using Terp.Threading;
```

## AsyncLock

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

## Cancellation

Pass a `CancellationToken` to `AcquireAsync`. If the token is cancelled while waiting, it throws `OperationCanceledException`.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
using var scope = await _lock.AcquireAsync(cts.Token);
```

## Disposal

`AsyncLock` implements `IDisposable`. After disposal, `AcquireAsync` throws `ObjectDisposedException`.

## Not reentrant

`AsyncLock` is not reentrant. Calling `AcquireAsync` twice on the same async call chain deadlocks. This matches `SemaphoreSlim(1, 1)` behaviour and is by design.

## When to use it

Use `AsyncLock` when you need to serialize access to a shared resource in async code and a `lock` statement is not allowed. For actor-based code, prefer Akka's single-threaded actor model instead. For retry timing, see [Resilience](/resilience/).
