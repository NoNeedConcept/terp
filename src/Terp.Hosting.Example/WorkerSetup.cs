using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Terp.Hosting.Gates;

namespace Terp.Hosting.Example;

internal sealed class WorkerSetup : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        SetupTrace.Add($"{nameof(WorkerSetup)}/{nameof(IServiceCollection)}");
        var gate = new WarmupGate(context.Configuration.GetValue("Demo:GateAttempts", 1));
        services.AddSingleton(gate);
        services.AddStartupGate("warmup", gate.CheckAsync);
    }
}
