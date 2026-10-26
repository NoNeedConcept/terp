using Akka.Actor;

namespace Terp.Akka.Example;

/// <summary>Created via <c>GreetingActors</c> and the DI-aware <c>Props</c>, so the <see cref="IGreeter"/> comes from DI.</summary>
internal sealed class GreeterActor : ReceiveActor
{
    public GreeterActor(IGreeter greeter) =>
        Receive<Greet>(m => Sender.Tell(new Done(greeter.Greet(m.Name))));
}
