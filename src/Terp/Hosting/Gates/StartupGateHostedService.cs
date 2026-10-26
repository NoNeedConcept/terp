using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Terp.Resilience;

namespace Terp.Hosting.Gates;

/// <summary>
/// Blocks host start in <see cref="StartingAsync"/> until every registered <see cref="IStartupGate"/> is clear.
/// </summary>
internal sealed class StartupGateHostedService : IHostedLifecycleService
{
    private static readonly TimeSpan _initialDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan _maxDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _defaultCheckTimeout = TimeSpan.FromSeconds(10);

    private readonly IEnumerable<IStartupGate> _gates;
    private readonly ILogger _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly TimeSpan _checkTimeout;

    public StartupGateHostedService(IEnumerable<IStartupGate> gates, ILogger<StartupGateHostedService>? logger = null)
        : this(gates, logger, static (delay, token) => Task.Delay(delay, token), _defaultCheckTimeout)
    {
    }

    /// <summary>Test seam: the wait between rounds and the per-check timeout can be replaced.</summary>
    internal StartupGateHostedService(IEnumerable<IStartupGate> gates, ILogger? logger,
        Func<TimeSpan, CancellationToken, Task> delay, TimeSpan checkTimeout)
    {
        _gates = gates;
        _logger = logger ?? NullLogger.Instance;
        _delay = delay;
        _checkTimeout = checkTimeout;
    }

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        var gates = _gates.ToArray();
        var cleared = new bool[gates.Length];
        var backoff = Backoff.Create(_initialDelay, 2.0, _maxDelay);

        try
        {
            for (var attempt = 0; ; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var open = Enumerable.Range(0, gates.Length).Where(i => !cleared[i]).ToArray();
                var results = await Task.WhenAll(
                    open.Select(i => CheckAsync(gates[i], cancellationToken))).ConfigureAwait(false);
                for (var k = 0; k < open.Length; k++)
                {
                    cleared[open[k]] = results[k];
                }

                if (cleared.All(static c => c))
                {
                    _logger.LogDebug("All startup gates are cleared.");
                    return;
                }

                var delay = backoff.Delay(attempt);
                _logger.LogWarning("Startup gates still open: {Gates}. Next retry in {Delay}.",
                    OpenNames(gates, cleared), delay);
                await _delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                $"Host startup was cancelled while startup gates were still open: {OpenNames(gates, cleared)}.",
                ex, cancellationToken);
        }
    }

    private static string OpenNames(IStartupGate[] gates, bool[] cleared)
    {
        return string.Join(", ", gates.Where((_, i) => !cleared[i]).Select(static g => g.Name));
    }

    private async Task<bool> CheckAsync(IStartupGate gate, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_checkTimeout);
        var token = timeoutSource.Token;
        try
        {
            return await gate.CheckAsync(token).WaitAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            _logger.LogWarning("Startup gate {Gate} timed out after {Timeout} and is treated as not clear.", gate.Name, _checkTimeout);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Startup gate {Gate} threw and is treated as not clear.", gate.Name);
            return false;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
