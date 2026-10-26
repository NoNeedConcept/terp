using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Terp.Hosting.Gates;

/// <summary>
/// Registers startup gates on an <see cref="IServiceCollection"/> (for a host builder use <c>builder.Services.AddStartupGate...</c>).
/// </summary>
/// <remarks>
/// <para>
/// Every <see cref="IStartupGate"/> registered in the container is evaluated by one internal hosted service in
/// <see cref="IHostedLifecycleService.StartingAsync"/>. That runs before the <c>StartAsync</c> of any hosted service
/// (a web server, an Akka actor system, ...). Relative to other <see cref="IHostedLifecycleService"/> implementations the
/// <c>StartingAsync</c> order equals registration order, so the first <c>AddStartupGate</c> call decides the position.
/// </para>
/// <para>
/// All open gates are checked concurrently, then re-checked with exponential backoff (1 second, doubling, capped at 30 seconds)
/// until every gate is clear.
/// Once a gate has returned <see langword="true"/> it is remembered as clear and is not checked again in later rounds,
/// so a flapping gate cannot keep the host from starting. A gate that throws, takes longer than
/// 10 seconds for one check or returns <see langword="false"/> counts as not clear; the names of the
/// open gates are logged. A gate that ignores its <see cref="CancellationToken"/> is abandoned on cancellation.
/// </para>
/// <para>
/// There is no retry limit of its own: the wait ends when the host start token is cancelled, which includes
/// <c>HostOptions.StartupTimeout</c> (30 seconds by default, so raise it for slow dependencies).
/// The resulting <see cref="OperationCanceledException"/> names the gates that were still open.
/// Gates are singletons; scoped services cannot be gates. To register a ready-made instance use
/// <c>services.AddSingleton&lt;IStartupGate&gt;(instance)</c> in addition to one of the methods below,
/// which installs the evaluating hosted service.
/// </para>
/// </remarks>
public static class StartupGateExtensions
{
    /// <summary>
    /// Adds a gate resolved from dependency injection as a singleton, so it may take constructor dependencies.
    /// Adding the same <typeparamref name="TGate"/> twice has no further effect.
    /// </summary>
    /// <typeparam name="TGate">The gate type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddStartupGate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGate>(
        this IServiceCollection services)
        where TGate : class, IStartupGate
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupGate, TGate>());
        return AddInfrastructure(services);
    }

    /// <summary>Adds a gate implemented by a delegate.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the gate, used in logs and exception messages.</param>
    /// <param name="check">The check; returns <see langword="true"/> when clear.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="check"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/>, empty or whitespace.</exception>
    public static IServiceCollection AddStartupGate(this IServiceCollection services, string name,
        Func<CancellationToken, Task<bool>> check)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(check);

        services.AddSingleton<IStartupGate>(new DelegateStartupGate(name, check));
        return AddInfrastructure(services);
    }

    private static IServiceCollection AddInfrastructure(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, StartupGateHostedService>());
        return services;
    }
}
