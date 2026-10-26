using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Terp.Diagnostics;

/// <summary>Extensions to time code blocks into a <see cref="Histogram{T}"/> of seconds.</summary>
public static class HistogramTimerExtensions
{
    /// <summary>Starts a timer without extra tags.</summary>
    /// <param name="histogram">The histogram receiving the elapsed seconds.</param>
    /// <param name="timeProvider">The time source; <see cref="TimeProvider.System"/> when <see langword="null"/>.</param>
    /// <returns>A scope that records on <see cref="TimerScope.Dispose"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="histogram"/> is <see langword="null"/>.</exception>
    public static TimerScope StartTimer(this Histogram<double> histogram, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(histogram);
        return new TimerScope(histogram, timeProvider ?? TimeProvider.System, default);
    }

    /// <summary>Starts a timer with one tag.</summary>
    /// <param name="histogram">The histogram receiving the elapsed seconds.</param>
    /// <param name="tag">A tag added to the measurement.</param>
    /// <param name="timeProvider">The time source; <see cref="TimeProvider.System"/> when <see langword="null"/>.</param>
    /// <returns>A scope that records on <see cref="TimerScope.Dispose"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="histogram"/> is <see langword="null"/>.</exception>
    public static TimerScope StartTimer(this Histogram<double> histogram, KeyValuePair<string, object?> tag, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(histogram);
        var tags = new TagList { tag };
        return new TimerScope(histogram, timeProvider ?? TimeProvider.System, tags);
    }

    /// <summary>Starts a timer with two tags.</summary>
    /// <param name="histogram">The histogram receiving the elapsed seconds.</param>
    /// <param name="tag1">The first tag.</param>
    /// <param name="tag2">The second tag.</param>
    /// <param name="timeProvider">The time source; <see cref="TimeProvider.System"/> when <see langword="null"/>.</param>
    /// <returns>A scope that records on <see cref="TimerScope.Dispose"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="histogram"/> is <see langword="null"/>.</exception>
    public static TimerScope StartTimer(this Histogram<double> histogram, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(histogram);
        var tags = new TagList { tag1, tag2 };
        return new TimerScope(histogram, timeProvider ?? TimeProvider.System, tags);
    }

    /// <summary>Starts a timer with any number of tags (copied; up to eight stay allocation-free).</summary>
    /// <param name="histogram">The histogram receiving the elapsed seconds.</param>
    /// <param name="tags">Tags added to the measurement.</param>
    /// <param name="timeProvider">The time source; <see cref="TimeProvider.System"/> when <see langword="null"/>.</param>
    /// <returns>A scope that records on <see cref="TimerScope.Dispose"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="histogram"/> is <see langword="null"/>.</exception>
    public static TimerScope StartTimer(this Histogram<double> histogram, ReadOnlySpan<KeyValuePair<string, object?>> tags, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(histogram);
        return new TimerScope(histogram, timeProvider ?? TimeProvider.System, new TagList(tags));
    }
}
