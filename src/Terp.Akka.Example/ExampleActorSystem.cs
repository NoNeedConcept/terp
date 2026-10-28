using Akka.Hosting;

namespace Terp.Akka.Example;

internal sealed class ExampleActorSystem : ActorSystemSetup
{
    protected override string ActorSystemName => "terp-example";

    protected override void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider serviceProvider)
    {
        builder
            .ConfigureLoggers(l =>
            {
                l.ClearLoggers();
                l.AddLoggerFactory(); // route Akka logs through Microsoft.Extensions.Logging
            })
            .ApplyRegistrations(serviceProvider);
    }
}
