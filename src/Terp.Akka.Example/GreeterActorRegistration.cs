using Akka.Hosting;
using Terp.Akka.Hosting;

namespace Terp.Akka.Example;

/// <summary>Registers the <see cref="GreeterActor"/> via <see cref="IActorRegistration"/>.</summary>
internal sealed class GreeterActorRegistration : IActorRegistration
{
    public void Configure(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, registry, resolver) =>
            registry.Register<GreeterActor>(system.ActorOf(resolver.Props<GreeterActor>(), "greeter")));
    }
}
