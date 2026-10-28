using Akka.Hosting;

namespace Terp.Akka;

/// <summary>
/// Allows a library to declare its actor topology without the host knowing the actor types directly.
/// Register implementations via <see cref="ActorRegistrationExtensions.AddActorRegistration{T}"/> and
/// dispatch them in <see cref="ActorSystemSetup.BuildSystem"/> with
/// <see cref="ActorRegistrationExtensions.ApplyRegistrations"/>.
/// </summary>
public interface IActorRegistration
{
    /// <summary>Configures actors on the builder.</summary>
    /// <param name="builder">The Akka configuration builder.</param>
    /// <param name="provider">The application's service provider.</param>
    void Configure(AkkaConfigurationBuilder builder, IServiceProvider provider);
}
