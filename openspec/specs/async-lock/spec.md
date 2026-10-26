## ADDED Requirements

### Requirement: AsyncLock provides mutual exclusion

`AsyncLock` in package `Terp`, namespace `Terp.Threading`, SHALL provide asynchronous mutual exclusion
via `AcquireAsync`. Only one caller SHALL hold the lock at a time. The lock is released when the
returned `AsyncLockScope` is disposed.

```csharp
public sealed class AsyncLock
{
    public ValueTask<AsyncLockScope> AcquireAsync(CancellationToken cancellationToken = default);
}

public readonly struct AsyncLockScope : IDisposable
{
    public void Dispose();
}
```

#### Scenario: Uncontended acquire returns synchronously

- **WHEN** `AcquireAsync` is called and the lock is free
- **THEN** it returns a completed `ValueTask<AsyncLockScope>` without allocating a `Task`

#### Scenario: Contended acquire waits

- **WHEN** `AcquireAsync` is called while another caller holds the lock
- **THEN** it returns an incomplete `ValueTask` that completes when the holder disposes the scope

#### Scenario: Dispose releases the lock

- **WHEN** the `AsyncLockScope` is disposed
- **THEN** the next waiting caller's `AcquireAsync` completes

#### Scenario: Cancellation throws OperationCanceledException

- **WHEN** `AcquireAsync` is waiting and the `CancellationToken` is cancelled
- **THEN** the `ValueTask` throws `OperationCanceledException`

### Requirement: AsyncLock is not reentrant

Acquiring the same `AsyncLock` instance twice on the same async call chain SHALL deadlock.
This is by design (same as `SemaphoreSlim(1,1)`). The behavior is documented in XML docs.

#### Scenario: Reentrant acquire deadlocks

- **WHEN** a caller holds the lock and calls `AcquireAsync` again on the same `AsyncLock`
- **THEN** the second `AcquireAsync` never completes (deadlock)

### Requirement: AsyncLock is disposable

`AsyncLock` SHALL implement `IDisposable` and dispose the underlying `SemaphoreSlim`.
After disposal, `AcquireAsync` SHALL throw `ObjectDisposedException`.

#### Scenario: Acquire after dispose throws

- **WHEN** `AcquireAsync` is called on a disposed `AsyncLock`
- **THEN** it throws `ObjectDisposedException`
