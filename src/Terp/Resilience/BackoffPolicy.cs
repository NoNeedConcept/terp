namespace Terp.Resilience;

/// <summary>
/// Calculates exponential backoff delays. Immutable; thread-safe when its random source is.
/// </summary>
public sealed class BackoffPolicy
{
    private readonly TimeSpan _initialDelay;
    private readonly double _multiplier;
    private readonly TimeSpan _maxDelay;
    private readonly Random _random;

    internal BackoffPolicy(TimeSpan initialDelay, double multiplier, TimeSpan? maxDelay, Random? random)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(multiplier, 1.0);

        _initialDelay = initialDelay;
        _multiplier = multiplier;
        _maxDelay = maxDelay ?? TimeSpan.MaxValue;
        _random = random ?? Random.Shared;
    }

    /// <summary>
    /// Returns the delay for the given attempt, capped at the maximum delay. Does not overflow for large attempts.
    /// </summary>
    /// <param name="attempt">The zero-based attempt number.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="attempt"/> is negative.</exception>
    public TimeSpan Delay(int attempt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(attempt);

        var delayMs = _initialDelay.TotalMilliseconds * Math.Pow(_multiplier, attempt);

        if (double.IsInfinity(delayMs) || delayMs > _maxDelay.TotalMilliseconds)
        {
            return _maxDelay;
        }

        return TimeSpan.FromMilliseconds(delayMs);
    }

    /// <summary>
    /// Returns <see cref="Delay"/> scaled by a random factor in [0.75, 1.25), never exceeding the maximum delay.
    /// </summary>
    /// <param name="attempt">The zero-based attempt number.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="attempt"/> is negative.</exception>
    public TimeSpan DelayWithJitter(int attempt)
    {
        var baseDelay = Delay(attempt);
        var jitter = _random.NextDouble() * 0.5 + 0.75;
        var jitteredMs = baseDelay.TotalMilliseconds * jitter;

        return jitteredMs >= _maxDelay.TotalMilliseconds ? _maxDelay : TimeSpan.FromMilliseconds(jitteredMs);
    }
}
