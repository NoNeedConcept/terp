using Microsoft.Extensions.Hosting;

namespace Terp.Tests.Hosting;

/// <summary>Stands in for an application type such as <c>WebApplication</c> that implements <see cref="IHost"/>.</summary>
public sealed class FakeApp(IHost inner) : IHost
{
    public IServiceProvider Services => inner.Services;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return inner.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return inner.StopAsync(cancellationToken);
    }

    public void Dispose()
    {
        inner.Dispose();
    }
}
