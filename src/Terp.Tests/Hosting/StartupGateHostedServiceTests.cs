using Microsoft.Extensions.Logging;

using Terp.Hosting.Gates;

namespace Terp.Tests.Hosting;

public sealed class StartupGateHostedServiceTests
{
    private sealed class FuncGate(string name, Func<CancellationToken, Task<bool>> check) : IStartupGate
    {
        public string Name { get; } = name;

        public Task<bool> CheckAsync(CancellationToken cancellationToken)
        {
            return check(cancellationToken);
        }
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private static StartupGateHostedService Create(IEnumerable<IStartupGate> gates, TimeSpan? checkTimeout = null,
        List<TimeSpan>? delays = null, ILogger? logger = null, Action<CancellationToken>? onDelay = null)
    {
        return new StartupGateHostedService(gates, logger, (delay, token) =>
        {
            delays?.Add(delay);
            onDelay?.Invoke(token);
            return Task.CompletedTask;
        }, checkTimeout ?? TimeSpan.FromSeconds(10));
    }

    [Fact(Timeout = 5000)]
    public async Task StartingAsync_RepeatedFailures_DelayGrowsExponentiallyUpToCap()
    {
        List<TimeSpan> delays = [];
        var remainingFailures = 6;
        var gate = new FuncGate("g", _ => Task.FromResult(remainingFailures-- <= 0));
        var service = Create([gate], delays: delays);

        await service.StartingAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(30)
            ],
            delays);
    }

    [Fact(Timeout = 5000)]
    public async Task StartingAsync_ClearedGate_IsNotCheckedAgain()
    {
        int firstCalls = 0, secondCalls = 0;
        var first = new FuncGate("first", _ => Task.FromResult(++firstCalls == 1));
        var second = new FuncGate("second", _ => Task.FromResult(++secondCalls >= 3));
        var service = Create([first, second]);

        await service.StartingAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, firstCalls);
        Assert.Equal(3, secondCalls);
    }

    [Fact(Timeout = 5000)]
    public async Task StartingAsync_OpenAndClearedGates_WarningNamesOnlyOpenGate()
    {
        var logger = new CapturingLogger();
        var attempts = 0;
        var ready = new FuncGate("ready-gate", _ => Task.FromResult(true));
        var waiting = new FuncGate("waiting-gate", _ => Task.FromResult(++attempts >= 2));
        var service = Create([ready, waiting], logger: logger);

        await service.StartingAsync(TestContext.Current.CancellationToken);

        var warning = Assert.Single(logger.Messages, m => m.Contains("still open", StringComparison.Ordinal));
        Assert.Contains("waiting-gate", warning, StringComparison.Ordinal);
        Assert.DoesNotContain("ready-gate", warning, StringComparison.Ordinal);
    }

    [Fact(Timeout = 5000)]
    public async Task StartingAsync_CancelledWhileGatesOpen_ThrowsNamingOpenGates()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var open = new FuncGate("open-gate", _ => Task.FromResult(false));
        var clear = new FuncGate("clear-gate", _ => Task.FromResult(true));
        var service = Create([open, clear], onDelay: token =>
        {
            cts.Cancel();
            token.ThrowIfCancellationRequested();
        });

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.StartingAsync(cts.Token));

        Assert.Contains("open-gate", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("clear-gate", ex.Message, StringComparison.Ordinal);
        Assert.IsAssignableFrom<OperationCanceledException>(ex.InnerException);
    }

    [Fact(Timeout = 5000)]
    public async Task StartingAsync_HangingCheckWithTimeout_TreatsAsNotClearAndRetries()
    {
        var logger = new CapturingLogger();
        var neverCompletes = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var gate = new FuncGate("hangs-once", _ => ++calls == 1 ? neverCompletes.Task : Task.FromResult(true));
        var service = Create([gate], TimeSpan.FromMilliseconds(50), logger: logger);

        await service.StartingAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, calls);
        Assert.Contains(logger.Messages, m => m.Contains("timed out", StringComparison.Ordinal) && m.Contains("hangs-once", StringComparison.Ordinal));
    }

    [Fact(Timeout = 5000)]
    public async Task StartingAsync_ThrowingGate_LogsGateName()
    {
        var logger = new CapturingLogger();
        var calls = 0;
        var gate = new FuncGate("thrower", _ => ++calls == 1 ? throw new InvalidOperationException("boom") : Task.FromResult(true));
        var service = Create([gate], logger: logger);

        await service.StartingAsync(TestContext.Current.CancellationToken);

        Assert.Contains(logger.Messages, m => m.Contains("thrower", StringComparison.Ordinal) && m.Contains("threw", StringComparison.Ordinal));
    }

    [Fact(Timeout = 5000)]
    public async Task StartingAsync_NoGates_CompletesImmediately()
    {
        var service = Create([]);

        await service.StartingAsync(TestContext.Current.CancellationToken);
    }
}
