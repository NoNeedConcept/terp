using Terp.Threading;

namespace Terp.Tests.Threading;

public sealed class AsyncLockTests
{
    [Fact]
    public async Task AcquireAsync_Uncontended_ReturnsSynchronously()
    {
        using var sut = new AsyncLock();

        var task = sut.AcquireAsync(TestContext.Current.CancellationToken);

        Assert.True(task.IsCompleted);
        var scope = await task;
        scope.Dispose();
    }

    [Fact]
    public async Task AcquireAsync_Contended_WaitsUntilReleased()
    {
        using var sut = new AsyncLock();
        var ct = TestContext.Current.CancellationToken;

        var first = await sut.AcquireAsync(ct);
        var secondTask = sut.AcquireAsync(ct);

        Assert.False(secondTask.IsCompleted);

        first.Dispose();

        var second = await secondTask.AsTask().WaitAsync(TimeSpan.FromSeconds(5), ct);
        second.Dispose();
    }

    [Fact]
    public async Task AcquireAsync_Cancelled_ThrowsOperationCanceledException()
    {
        using var sut = new AsyncLock();
        using var cts = new CancellationTokenSource();

        var first = await sut.AcquireAsync(TestContext.Current.CancellationToken);
        var secondTask = sut.AcquireAsync(cts.Token);

        Assert.False(secondTask.IsCompleted);
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await secondTask.AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

        first.Dispose();
    }

    [Fact]
    public async Task AcquireAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var sut = new AsyncLock();
        sut.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await sut.AcquireAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Dispose_ReleasesScope()
    {
        using var sut = new AsyncLock();
        var ct = TestContext.Current.CancellationToken;

        var scope1 = await sut.AcquireAsync(ct);
        var waitTask = sut.AcquireAsync(ct);

        scope1.Dispose();

        var scope2 = await waitTask.AsTask().WaitAsync(TimeSpan.FromSeconds(5), ct);
        scope2.Dispose();
    }

    [Fact]
    public async Task MutualExclusion_SequentialAccess()
    {
        using var sut = new AsyncLock();
        var ct = TestContext.Current.CancellationToken;
        var counter = 0;
        var maxConcurrent = 0;

        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            using var scope = await sut.AcquireAsync(ct);
            var current = Interlocked.Increment(ref counter);
            if (current > maxConcurrent)
            {
                Interlocked.Exchange(ref maxConcurrent, current);
            }

            await Task.Yield();
            Interlocked.Decrement(ref counter);
        });

        await Task.WhenAll(tasks);
        Assert.Equal(1, maxConcurrent);
    }
}
