namespace Terp.Threading;

/// <summary>
/// Provides asynchronous mutual exclusion via a scoped lock. Only one caller holds the lock at a time.
/// </summary>
/// <remarks>
/// The lock is not reentrant: acquiring the same <see cref="AsyncLock"/> twice on the same async call chain
/// will deadlock, the same as <see cref="SemaphoreSlim"/> with a count of one.
/// </remarks>
public sealed class AsyncLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Acquires the lock. The returned <see cref="AsyncLockScope"/> releases it on disposal.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the wait.</param>
    /// <returns>A scope that releases the lock when disposed.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public async ValueTask<AsyncLockScope> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new AsyncLockScope(_semaphore);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
