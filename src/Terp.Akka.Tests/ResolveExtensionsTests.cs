using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;

namespace Terp.Akka.Tests;

public sealed class ResolveExtensionsTests : TestKit
{
    public sealed class PlainActor : ReceiveActor
    {
    }

    public sealed class TestChildActor : ReceiveActor
    {
        public TestChildActor()
        {
            Receive<string>(msg => Sender.Tell($"echo:{msg}"));
        }
    }

    public sealed class TestParentActor : ReceiveActor
    {
        public TestParentActor()
        {
            var child = Context.ResolveChildActor<TestChildActor>("configured-child",
                props => props.WithDispatcher("akka.actor.default-dispatcher"));

            Receive<string>(child.Forward);
        }
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [Fact]
    public void ResolveActor_PlainActor_ReturnsActorRef()
    {
        var actorRef = Sys.ResolveActor<PlainActor>();
        Assert.NotEqual(Nobody.Instance, actorRef);
    }

    [Fact]
    public void ResolveActor_ConfigureCallback_ReturnsActorRef()
    {
        var actorRef = Sys.ResolveActor<TestChildActor>(configure: props => props.WithDispatcher("akka.actor.default-dispatcher"));

        Assert.NotEqual(Nobody.Instance, actorRef);
    }

    [Fact]
    public async Task ResolveChildActor_ConfigureCallback_DeliversMessagesToChild()
    {
        var parent = Sys.ResolveActor<TestParentActor>();

        var reply = await parent.Ask<string>("ping", TestContext.Current.CancellationToken);

        Assert.Equal("echo:ping", reply);
    }

    [Fact]
    public void ResolveActor_NullSystemContextOrConfigure_ThrowsArgumentNullException()
    {
        AssertParamName("system", () => ((ActorSystem)null!).ResolveActor<PlainActor>());
        AssertParamName("system", () => ((ActorSystem)null!).ResolveActor<PlainActor>("name"));
        AssertParamName("configure", () => Sys.ResolveActor<PlainActor>("name", (Func<Props, Props>)null!));
        AssertParamName("configure", () => Sys.ResolveActor<PlainActor>((Func<Props, Props>)null!));
        AssertParamName("context", () => ((IActorContext)null!).ResolveActor<PlainActor>());
        AssertParamName("context", () => ((IActorContext)null!).ResolveActor<PlainActor>("name"));
        AssertParamName("context", () => ((IActorContext)null!).ResolveChildActor<PlainActor>("name"));
        AssertParamName("context", () => ((IActorContext)null!).ResolveChildActor<PlainActor>("name", props => props));
    }

    private static void AssertParamName(string expected, Action act)
    {
        var ex = Assert.Throws<ArgumentNullException>(act);

        Assert.Equal(expected, ex.ParamName);
    }
}
