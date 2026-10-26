using Akka.Actor;
using Akka.Pattern;

namespace Terp.Akka;

/// <summary>
/// Extensions for wrapping <see cref="Props"/> in a <see cref="BackoffSupervisor"/>.
/// </summary>
public static class BackoffExtensions
{
    /// <summary>
    /// Wraps the child in a backoff supervisor that restarts it with exponential backoff when it fails.
    /// Defaults to 3s minimum, 30s maximum, a random factor of 0.2 and unlimited retries.
    /// </summary>
    /// <param name="childProps">The props of the supervised child.</param>
    /// <param name="childName">The name of the supervised child.</param>
    /// <param name="minBackoff">The minimum backoff, or <see langword="null"/> for 3 seconds.</param>
    /// <param name="maxBackoff">The maximum backoff, or <see langword="null"/> for 30 seconds.</param>
    /// <param name="randomFactor">The random jitter added to the backoff, as a fraction.</param>
    /// <param name="configure">Optional; adjusts the <see cref="BackoffOptions"/> before the supervisor props are created.</param>
    /// <returns>The props of the backoff supervisor.</returns>
    public static Props BackoffOnFailure(this Props childProps, string childName,
        TimeSpan? minBackoff = null, TimeSpan? maxBackoff = null, double randomFactor = 0.2,
        Func<BackoffOptions, BackoffOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(childProps);
        ArgumentNullException.ThrowIfNull(childName);

        var options = Backoff.OnFailure(childProps, childName,
            minBackoff ?? TimeSpan.FromSeconds(3), maxBackoff ?? TimeSpan.FromSeconds(30), randomFactor, maxNrOfRetries: -1);
        if (configure is not null)
        {
            options = configure(options);
        }

        return BackoffSupervisor.Props(options);
    }

    /// <summary>
    /// Wraps the child in a backoff supervisor that restarts it with exponential backoff when it stops.
    /// Defaults to 3s minimum, 30s maximum, a random factor of 0.2 and unlimited retries.
    /// </summary>
    /// <param name="childProps">The props of the supervised child.</param>
    /// <param name="childName">The name of the supervised child.</param>
    /// <param name="minBackoff">The minimum backoff, or <see langword="null"/> for 3 seconds.</param>
    /// <param name="maxBackoff">The maximum backoff, or <see langword="null"/> for 30 seconds.</param>
    /// <param name="randomFactor">The random jitter added to the backoff, as a fraction.</param>
    /// <param name="configure">Optional; adjusts the <see cref="BackoffOptions"/> before the supervisor props are created.</param>
    /// <returns>The props of the backoff supervisor.</returns>
    public static Props BackoffOnStop(this Props childProps, string childName,
        TimeSpan? minBackoff = null, TimeSpan? maxBackoff = null, double randomFactor = 0.2,
        Func<BackoffOptions, BackoffOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(childProps);
        ArgumentNullException.ThrowIfNull(childName);

        var options = Backoff.OnStop(childProps, childName,
            minBackoff ?? TimeSpan.FromSeconds(3), maxBackoff ?? TimeSpan.FromSeconds(30), randomFactor, maxNrOfRetries: -1);
        if (configure is not null)
        {
            options = configure(options);
        }

        return BackoffSupervisor.Props(options);
    }
}
