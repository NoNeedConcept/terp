using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Terp.Diagnostics;

/// <summary>
/// The standard instruments for a recurring job or batch run: run count by outcome, duration, and the Unix time of the
/// last success (for "seconds since last success" alerts, e.g. <c>time() - max(name_last_success_timestamp_seconds)</c>).
/// </summary>
/// <remarks>
/// Instruments (for <c>name</c> = <c>"myapp.cleanup"</c>):
/// <c>myapp.cleanup.runs</c> (<c>Counter&lt;long&gt;</c>, unit <c>{run}</c>, tag <c>outcome</c> = <c>success</c> | <c>failure</c>),
/// <c>myapp.cleanup.duration</c> (<c>Histogram&lt;double&gt;</c>, unit <c>s</c>, same tag) and
/// <c>myapp.cleanup.last_success_timestamp</c> (<c>Gauge&lt;double&gt;</c>, unit <c>s</c>, Unix seconds, no outcome tag).
/// A failed run adds <c>error.type</c> (exception type name, or <c>abandoned</c>). The gauge has no value until the
/// first success of the process, so a dashboard needs to handle a missing series after a restart.
/// </remarks>
public sealed class JobMetrics
{
    internal const string OutcomeTag = "outcome";
    internal const string ErrorTypeTag = "error.type";
    internal const string SuccessOutcome = "success";
    internal const string FailureOutcome = "failure";
    internal const string AbandonedErrorType = "abandoned";

    private readonly Counter<long> _runs;
    private readonly Histogram<double> _duration;
    private readonly Gauge<double> _lastSuccess;
    private readonly TimeProvider _timeProvider;

    private JobMetrics(Meter meter, string name, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _runs = meter.CreateCounter<long>($"{name}.runs", "{run}", "Job runs by outcome.");
        _duration = meter.CreateHistogram<double>($"{name}.duration", "s", "Job run duration.");
        _lastSuccess = meter.CreateGauge<double>($"{name}.last_success_timestamp", "s", "Unix time of the last successful run.");
    }

    /// <summary>Creates the three instruments on <paramref name="meter"/>.</summary>
    /// <param name="meter">The caller's meter; it owns the instruments' lifetime.</param>
    /// <param name="name">The dotted instrument prefix, e.g. <c>myapp.cleanup</c>.</param>
    /// <param name="timeProvider">The time source; <see cref="TimeProvider.System"/> when <see langword="null"/>.</param>
    /// <returns>The job metrics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="meter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null, empty or whitespace.</exception>
    public static JobMetrics Create(Meter meter, string name, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(meter);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new JobMetrics(meter, name, timeProvider ?? TimeProvider.System);
    }

    /// <summary>Starts a run without extra tags.</summary>
    /// <returns>The run; call <see cref="JobRun.Succeed"/> or <see cref="JobRun.Fail(Exception?)"/> and dispose it.</returns>
    public JobRun Start()
    {
        return new JobRun(this, _timeProvider.GetTimestamp(), default);
    }

    /// <summary>Starts a run with one extra tag applied to all three instruments.</summary>
    /// <param name="tag">The tag.</param>
    /// <returns>The run.</returns>
    public JobRun Start(KeyValuePair<string, object?> tag)
    {
        var tags = new TagList { tag };
        return new JobRun(this, _timeProvider.GetTimestamp(), tags);
    }

    /// <summary>Starts a run with two extra tags applied to all three instruments.</summary>
    /// <param name="tag1">The first tag.</param>
    /// <param name="tag2">The second tag.</param>
    /// <returns>The run.</returns>
    public JobRun Start(KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
    {
        var tags = new TagList { tag1, tag2 };
        return new JobRun(this, _timeProvider.GetTimestamp(), tags);
    }

    /// <summary>Starts a run with any number of extra tags applied to all three instruments.</summary>
    /// <param name="tags">The tags (copied).</param>
    /// <returns>The run.</returns>
    public JobRun Start(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        return new JobRun(this, _timeProvider.GetTimestamp(), new TagList(tags));
    }

    internal void Complete(long start, ref TagList tags, bool success, string? errorType)
    {
        var seconds = _timeProvider.GetElapsedTime(start).TotalSeconds;
        if (success)
        {
            _lastSuccess.Record(_timeProvider.GetUtcNow().ToUnixTimeMilliseconds() / 1000.0, tags);
        }

        tags.Add(OutcomeTag, success ? SuccessOutcome : FailureOutcome);
        if (errorType is not null)
        {
            tags.Add(ErrorTypeTag, errorType);
        }

        _runs.Add(1, tags);
        _duration.Record(seconds, tags);
    }
}
