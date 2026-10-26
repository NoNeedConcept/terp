namespace Terp.Resilience;

/// <summary>
/// Factory for exponential <see cref="BackoffPolicy"/> instances.
/// </summary>
public static class Backoff
{
    /// <summary>
    /// Creates an exponential backoff policy: <c>initialDelay * multiplier^attempt</c>, capped at <paramref name="maxDelay"/>.
    /// </summary>
    /// <param name="initialDelay">The delay for attempt 0.</param>
    /// <param name="multiplier">The growth factor per attempt; must be at least 1.</param>
    /// <param name="maxDelay">The upper bound for any delay, or <see langword="null"/> for <see cref="TimeSpan.MaxValue"/>.</param>
    /// <param name="random">The random source for <see cref="BackoffPolicy.DelayWithJitter"/>, or <see langword="null"/> for <see cref="Random.Shared"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="multiplier"/> is less than 1.</exception>
    public static BackoffPolicy Create(TimeSpan initialDelay, double multiplier = 2.0, TimeSpan? maxDelay = null,
        Random? random = null)
    {
        return new BackoffPolicy(initialDelay, multiplier, maxDelay, random);
    }
}
