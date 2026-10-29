using Akka.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Terp.Akka.Hosting;

/// <summary>
/// Extension methods for registering and applying <see cref="IActorRegistration"/> instances.
/// </summary>
public static class ActorRegistrationExtensions
{
    /// <summary>
    /// Registers <typeparamref name="T"/> as a singleton <see cref="IActorRegistration"/>.
    /// </summary>
    /// <typeparam name="T">An <see cref="IActorRegistration"/> implementation with a parameterless constructor.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddActorRegistration<T>(this IServiceCollection services)
        where T : class, IActorRegistration, new()
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IActorRegistration, T>();
        return services;
    }

    /// <summary>
    /// Resolves all <see cref="IActorRegistration"/> services and calls <see cref="IActorRegistration.Configure"/>
    /// on each, in DI registration order.
    /// </summary>
    /// <param name="builder">The Akka configuration builder.</param>
    /// <param name="provider">The application's service provider.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="provider"/> is <see langword="null"/>.</exception>
    public static AkkaConfigurationBuilder ApplyRegistrations(
        this AkkaConfigurationBuilder builder,
        IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(provider);

        foreach (var registration in provider.GetServices<IActorRegistration>())
        {
            registration.Configure(builder, provider);
        }

        return builder;
    }
}
