using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Terp.Hosting.Gates;

namespace Terp.Tests.Hosting;

public sealed class StartupGateTests
{
    private sealed class OrderRecorder
    {
        public List<string> Log { get; } = [];
    }

    private sealed class RecordingService(OrderRecorder recorder) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            recorder.Log.Add("hosted-start");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class DiGate(OrderRecorder recorder) : IStartupGate
    {
        public Task<bool> CheckAsync(CancellationToken cancellationToken)
        {
            recorder.Log.Add("gate");
            return Task.FromResult(true);
        }
    }

    private sealed class InstanceGate : IStartupGate
    {
        public string Name => "instance";

        public int Calls { get; private set; }

        public Task<bool> CheckAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(++Calls >= 2);
        }
    }

    [Fact(Timeout = 10000)]
    public async Task StartAsync_AllGatesClear_HostStarts()
    {
        var calls = 0;
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddStartupGate("db", _ =>
        {
            calls++;
            return Task.FromResult(true);
        });
        using var host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, calls);
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Timeout = 10000)]
    public async Task StartAsync_GateNotClearAtFirst_RetriesUntilClear()
    {
        var calls = 0;
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddStartupGate("db", _ => Task.FromResult(++calls >= 2));
        using var host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, calls);
    }

    [Fact(Timeout = 10000)]
    public async Task StartAsync_InstanceRegisteredAsGate_IsEvaluated()
    {
        var gate = new InstanceGate();
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IStartupGate>(gate);
        builder.Services.AddStartupGate("other", _ => Task.FromResult(true));
        using var host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, gate.Calls);
    }

    [Fact(Timeout = 10000)]
    public async Task StartAsync_ThrowingGate_CountsAsNotReadyAndRetries()
    {
        var calls = 0;
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddStartupGate("flaky", _ =>
        {
            if (++calls < 2)
            {
                throw new InvalidOperationException("not yet");
            }

            return Task.FromResult(true);
        });
        using var host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, calls);
    }

    [Fact(Timeout = 10000)]
    public async Task StartAsync_Cancelled_AbortsWaitNamingOpenGate()
    {
        var checkedOnce = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddStartupGate("never-ready", _ =>
        {
            checkedOnce.TrySetResult();
            return Task.FromResult(false);
        });
        using var host = builder.Build();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        var start = host.StartAsync(cts.Token);
        await checkedOnce.Task.WaitAsync(TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);
        Assert.Contains("never-ready", ex.Message, StringComparison.Ordinal);
    }

    [Fact(Timeout = 10000)]
    public async Task StartAsync_GateIgnoresToken_CancelledStartDoesNotHang()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var neverCompletes = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddStartupGate("ignores-token", _ =>
        {
            entered.TrySetResult();
            return neverCompletes.Task;
        });
        using var host = builder.Build();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        var start = host.StartAsync(cts.Token);
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);
        Assert.Contains("ignores-token", ex.Message, StringComparison.Ordinal);
    }

    [Fact(Timeout = 10000)]
    public async Task StartAsync_GateIgnoresToken_DoesNotOutliveHostStartupTimeout()
    {
        var neverCompletes = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var builder = Host.CreateApplicationBuilder();
        builder.Services.Configure<HostOptions>(o => o.StartupTimeout = TimeSpan.FromMilliseconds(200));
        builder.Services.AddStartupGate("slow-db", _ => neverCompletes.Task);
        using var host = builder.Build();

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));

        Assert.Contains("slow-db", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddStartupGate_MultipleCalls_RegistersInfrastructureOnce()
    {
        var services = new ServiceCollection();
        services.AddStartupGate("a", _ => Task.FromResult(true));
        services.AddStartupGate("b", _ => Task.FromResult(true));
        services.AddStartupGate<DiGate>();
        services.AddStartupGate<DiGate>();

        Assert.Equal(1, services.Count(d => d.ServiceType == typeof(IHostedService)));
        Assert.Equal(3, services.Count(d => d.ServiceType == typeof(IStartupGate)));
    }

    [Fact(Timeout = 10000)]
    public async Task StartAsync_GateType_ResolvedFromDependencyInjection()
    {
        var recorder = new OrderRecorder();
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(recorder);
        builder.Services.AddStartupGate<DiGate>();
        using var host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["gate"], recorder.Log);
    }

    [Fact(Timeout = 10000)]
    public async Task StartAsync_GatesAndHostedService_GatesClearBeforeHostedServiceStarts()
    {
        var recorder = new OrderRecorder();
        var calls = 0;
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(recorder);
        builder.Services.AddHostedService<RecordingService>();
        builder.Services.AddStartupGate("db", _ =>
        {
            recorder.Log.Add("gate");
            return Task.FromResult(++calls >= 2);
        });
        using var host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["gate", "gate", "hosted-start"], recorder.Log);
    }

    [Fact]
    public void Name_TypedAndDelegateGates_UseTypeNameAndGivenName()
    {
        IStartupGate typed = new DiGate(new OrderRecorder());

        Assert.Equal(nameof(DiGate), typed.Name);
        Assert.Equal("named", new DelegateStartupGate("named", _ => Task.FromResult(true)).Name);
    }

    [Fact]
    public void AddStartupGate_InvalidArguments_Throw()
    {
        var services = new ServiceCollection();

        var servicesEx = Assert.Throws<ArgumentNullException>(() => StartupGateExtensions.AddStartupGate<DiGate>(null!));
        var checkEx = Assert.Throws<ArgumentNullException>(() => services.AddStartupGate("a", null!));
        var nameEx = Assert.Throws<ArgumentException>(() => services.AddStartupGate(" ", _ => Task.FromResult(true)));

        Assert.Multiple(
            () => Assert.Equal("services", servicesEx.ParamName),
            () => Assert.Equal("check", checkEx.ParamName),
            () => Assert.Equal("name", nameEx.ParamName));
    }

}
