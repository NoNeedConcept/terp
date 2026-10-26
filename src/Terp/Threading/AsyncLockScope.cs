namespace Terp.Threading;

/// <summary>
/// The scope returned by <see cref="AsyncLock.AcquireAsync"/>. Disposing it releases the lock.
/// </summary>
public readonly struct AsyncLockScope : IDisposable
{
    private readonly SemaphoreSlim _semaphore;

    internal AsyncLockScope(SemaphoreSlim semaphore)
    {
        _semaphore = semaphore;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _semaphore.Release();
    }
}
