using System.Diagnostics;

namespace Terp.Diagnostics;

/// <summary>
/// Convenience extensions for <see cref="JobMetrics"/> that wrap the <see cref="JobRun"/>
/// start–succeed/fail–dispose ceremony in a single async call.
/// </summary>
public static class JobMetricsExtensions
{
    /// <summary>
    /// Starts a run, executes <paramref name="action"/>, records success on completion or failure on exception,
    /// and returns the result. The run is always completed (never abandoned).
    /// </summary>
    /// <typeparam name="T">The return type of the action.</typeparam>
    /// <param name="job">The job metrics instance.</param>
    /// <param name="action">The async work to run.</param>
    /// <param name="tags">Extra tags applied to all instruments.</param>
    /// <param name="cancellationToken">Passed to <paramref name="action"/>.</param>
    /// <returns>The result of <paramref name="action"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="job"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    public static async Task<T> RunAsync<T>(this JobMetrics job,
        Func<CancellationToken, Task<T>> action,
        TagList tags = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(action);

        using var run = StartRun(job, tags);
        try
        {
            var result = await action(cancellationToken).ConfigureAwait(false);
            run.Succeed();
            return result;
        }
        catch (Exception ex)
        {
            run.Fail(ex);
            throw;
        }
    }

    /// <summary>
    /// Starts a run, executes <paramref name="action"/>, records success on completion or failure on exception.
    /// The run is always completed (never abandoned).
    /// </summary>
    /// <param name="job">The job metrics instance.</param>
    /// <param name="action">The async work to run.</param>
    /// <param name="tags">Extra tags applied to all instruments.</param>
    /// <param name="cancellationToken">Passed to <paramref name="action"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="job"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    public static async Task RunAsync(this JobMetrics job,
        Func<CancellationToken, Task> action,
        TagList tags = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(action);

        using var run = StartRun(job, tags);
        try
        {
            await action(cancellationToken).ConfigureAwait(false);
            run.Succeed();
        }
        catch (Exception ex)
        {
            run.Fail(ex);
            throw;
        }
    }

    private static JobRun StartRun(JobMetrics job, in TagList tags)
    {
        return tags.Count switch
        {
            0 => job.Start(),
            1 => job.Start(tags[0]),
            2 => job.Start(tags[0], tags[1]),
            _ => job.Start(TagsToArray(tags)),
        };
    }

    private static KeyValuePair<string, object?>[] TagsToArray(in TagList tags)
    {
        var array = new KeyValuePair<string, object?>[tags.Count];
        for (var i = 0; i < tags.Count; i++)
        {
            array[i] = tags[i];
        }

        return array;
    }
}
