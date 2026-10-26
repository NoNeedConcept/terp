using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Terp.Diagnostics;

/// <summary>
/// Measures the time between <see cref="HistogramTimerExtensions.StartTimer(Histogram{double}, TimeProvider?)"/> and
/// <see cref="Dispose"/> and records it in seconds on a <see cref="Histogram{T}"/>. Allocation-free.
/// </summary>
/// <remarks>
/// <para>
/// Every measurement carries the tag <c>outcome</c> with the value <c>success</c>, or <c>failure</c> after
/// <see cref="Fail"/> was called (the same values as <see cref="JobMetrics"/>). The tag is always present so dashboards can group and filter on it without
/// handling a missing label.
/// </para>
/// <para>
/// This is a mutable struct (a <c>readonly struct</c> cannot carry the outcome without a heap allocation).
/// Use it directly in a <c>using</c> statement or declaration, where the compiler passes the variable by reference.
/// Do not copy it: <see cref="Fail"/> and <see cref="Dispose"/> act on the copy they are called on, so a copied scope
/// that is disposed twice records twice. Do not box it either. Disposing the same variable more than once is a no-op.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var timer = histogram.StartTimer(new("api", "tmdb"));
/// try { await CallAsync(); }
/// catch { timer.Fail(); throw; }
/// </code>
/// </example>
public struct TimerScope : IDisposable
{
    internal const string OutcomeTag = "outcome";
    internal const string SuccessOutcome = "success";
    internal const string FailureOutcome = "failure";

    private readonly Histogram<double>? _histogram;
    private readonly TimeProvider? _timeProvider;
    private readonly long _start;
    private TagList _tags;
    private bool _failed;
    private bool _disposed;

    internal TimerScope(Histogram<double> histogram, TimeProvider timeProvider, in TagList tags)
    {
        _histogram = histogram;
        _timeProvider = timeProvider;
        _tags = tags;
        _start = timeProvider.GetTimestamp();
    }

    /// <summary>Marks the measurement as failed; <see cref="Dispose"/> then records <c>outcome=failure</c>. Has no effect after disposal.</summary>
    public void Fail()
    {
        _failed = true;
    }

    /// <summary>Records the elapsed seconds once. Further calls and calls on a default instance do nothing.</summary>
    public void Dispose()
    {
        if (_histogram is null || _timeProvider is null || _disposed)
        {
            return;
        }

        _disposed = true;
        var seconds = _timeProvider.GetElapsedTime(_start).TotalSeconds;
        _tags.Add(OutcomeTag, _failed ? FailureOutcome : SuccessOutcome);
        _histogram.Record(seconds, _tags);
    }
}
