using Akka.Actor;
using Akka.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Terp.Hosting;

namespace Terp.Akka.Hosting;

/// <summary>
/// Base class for the setup that owns the one <c>ActorSystem</c> of a host: it registers it via Akka.Hosting under
/// <see cref="ActorSystemName"/> and configures it in <see cref="BuildSystem"/>. Only one actor system can be registered per service collection.
/// </summary>
public abstract class ActorSystemSetup : ISetup<IServiceCollection>
{
    /// <summary>Gets the name of the actor system: letters, digits, '-' and '_', starting with a letter or digit.</summary>
    protected abstract string ActorSystemName { get; }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><see cref="ActorSystemName"/> is null, empty, whitespace, does not start with a letter or digit, or contains characters other than letters, digits, '-' and '_'.</exception>
    /// <exception cref="InvalidOperationException">An <c>ActorSystem</c> is already registered; a second registration would silently replace the first.</exception>
    public void Setup(IServiceCollection services, SetupContext context)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(context);

        var name = ActorSystemName;
        ValidateName(name);

        if (services.Any(static d => d.ServiceType == typeof(ActorSystem)))
        {
            throw new InvalidOperationException(
                $"An ActorSystem is already registered; '{name}' cannot be added. Register exactly one ActorSystemSetup.");
        }

        services.AddAkka(name, BuildSystem);
    }

    /// <summary>Configures the actor system.</summary>
    /// <param name="builder">The Akka configuration builder.</param>
    /// <param name="serviceProvider">The application's service provider.</param>
    protected abstract void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider serviceProvider);

    private static void ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The actor system name must not be null, empty or whitespace.", nameof(name));
        }

        if (!char.IsAsciiLetterOrDigit(name[0]) || !name.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            throw new ArgumentException(
                $"Invalid actor system name '{name}': only letters, digits, '-' and '_' are allowed, and it must start with a letter or digit.",
                nameof(name));
        }
    }
}
