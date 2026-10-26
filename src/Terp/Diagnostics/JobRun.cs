using System.Diagnostics;

namespace Terp.Diagnostics;

/// <summary>
/// One run of a job started with <see cref="JobMetrics.Start()"/>. Call <see cref="Succeed"/> or <see cref="Fail"/>; the
/// first call records the run. Disposing a run without a result records it as <c>outcome=failure</c>,
/// <c>error.type=abandoned</c>, so an early return or cancellation never makes a run disappear from the counts.
/// </summary>
/// <remarks>
/// A class, so it is safe to pass around: there is exactly one completion state per run, and the first result wins even
/// when the run is shared between threads. It costs one small allocation per run, which is irrelevant at job cadence.
/// </remarks>
public sealed class JobRun : IDisposable
{
    private readonly JobMetrics _owner;
    private readonly long _start;
    private TagList _tags;
    private int _completed;

    internal JobRun(JobMetrics owner, long start, in TagList tags)
    {
        _owner = owner;
        _start = start;
        _tags = tags;
    }

    /// <summary>Records a successful run and updates the last-success timestamp. Ignored if the run is already completed.</summary>
    public void Succeed()
    {
        Complete(true, null);
    }

    /// <summary>Records a failed run; the last-success timestamp is not touched. Ignored if the run is already completed.</summary>
    /// <param name="exception">An optional exception whose full type name becomes the <c>error.type</c> tag (<c>unknown</c> when <see langword="null"/>).</param>
    public void Fail(Exception? exception = null)
    {
        Complete(false, exception?.GetType().FullName ?? "unknown");
    }

    /// <summary>Records the run as abandoned (a failure) if neither <see cref="Succeed"/> nor <see cref="Fail"/> was called.</summary>
    public void Dispose()
    {
        Complete(false, JobMetrics.AbandonedErrorType);
    }

    private void Complete(bool success, string? errorType)
    {
        if (Interlocked.Exchange(ref _completed, 1) != 0)
        {
            return;
        }

        _owner.Complete(_start, ref _tags, success, errorType);
    }
}
