using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;

namespace Terp.Akka.Tests;

public sealed class ContextExtensionsTests : TestKit
{
    public sealed record Forward(int Value);

    public sealed record Tell(int Value);

    public sealed record TellMissing;

    public sealed record ForwardMissing;

    public sealed record GetChildQuery(string Name);

    public sealed record ChildAnswer(int Value, bool SenderIsParent);

    public sealed class TestParentActor : ReceiveActor
    {
        public TestParentActor(IActorRef probe)
        {
            Context.ResolveChildActor<TestChildActor>("hans");

            Receive<Forward>(msg => Context.ChildForward("hans", msg));
            Receive<Tell>(msg => Context.ChildTell("hans", msg));
            Receive<TellMissing>(msg => Sender.Tell(Context.ChildTell("nobody", msg)));
            Receive<ForwardMissing>(msg => Sender.Tell(Context.ChildForward("nobody", msg)));
            Receive<GetChildQuery>(msg => Sender.Tell(Context.GetChild(msg.Name).Match(c => (object)c, () => "none")));

            // Answers to ChildTell arrive here because the child replies to its sender, the parent.
            Receive<ChildAnswer>(msg => probe.Tell(msg));
        }
    }

    public sealed class TestChildActor : ReceiveActor
    {
        public TestChildActor()
        {
            Receive<Forward>(msg => Sender.Tell(new ChildAnswer(msg.Value, Sender.Equals(Context.Parent))));
            Receive<Tell>(msg => Sender.Tell(new ChildAnswer(msg.Value, Sender.Equals(Context.Parent))));
        }
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [Fact(Timeout = 10000)]
    public async Task ChildForward_ExistingChild_PreservesOriginalSender()
    {
        var parent = Sys.ResolveActor<TestParentActor>(ActorRefs.Nobody);

        // Ask's temp actor is the original sender; the child must answer it directly, not the parent.
        var answer = await parent.Ask<ChildAnswer>(new Forward(555), TestContext.Current.CancellationToken);

        Assert.Equal(555, answer.Value);
        Assert.False(answer.SenderIsParent);
    }

    [Fact]
    public void ChildTell_ExistingChild_UsesParentAsSender()
    {
        var probe = CreateTestProbe();
        var parent = Sys.ResolveActor<TestParentActor>(probe.Ref);

        // The external sender is a different probe, ChildTell must still use the parent (Self) as sender.
        var external = CreateTestProbe();
        parent.Tell(new Tell(7), external.Ref);

        probe.ExpectMsg(new ChildAnswer(7, SenderIsParent: true), cancellationToken: TestContext.Current.CancellationToken);
        external.ExpectNoMsg(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);
    }

    [Fact(Timeout = 10000)]
    public async Task ChildTellAndChildForward_MissingChild_ReturnFalse()
    {
        var parent = Sys.ResolveActor<TestParentActor>(ActorRefs.Nobody);

        Assert.False(await parent.Ask<bool>(new TellMissing(), TestContext.Current.CancellationToken));
        Assert.False(await parent.Ask<bool>(new ForwardMissing(), TestContext.Current.CancellationToken));
    }

    [Fact(Timeout = 10000)]
    public async Task GetChild_ExistingAndMissingChild_ReturnsChildOrNone()
    {
        var parent = Sys.ResolveActor<TestParentActor>(ActorRefs.Nobody);

        var existing = await parent.Ask<object>(new GetChildQuery("hans"), TestContext.Current.CancellationToken);
        var missing = await parent.Ask<object>(new GetChildQuery("nobody"), TestContext.Current.CancellationToken);

        var child = Assert.IsAssignableFrom<IActorRef>(existing);
        Assert.Equal("hans", child.Path.Name);
        Assert.Equal("none", missing);
    }

    [Fact]
    public void GetChildChildTellChildForward_NullContext_ThrowsArgumentNullException()
    {
        var context = (IActorContext)null!;

        AssertParamName("context", () => context.GetChild("hans"));
        AssertParamName("context", () => context.ChildTell("hans", "message"));
        AssertParamName("context", () => context.ChildForward("hans", "message"));
    }

    private static void AssertParamName(string expected, Action act)
    {
        var ex = Assert.Throws<ArgumentNullException>(act);

        Assert.Equal(expected, ex.ParamName);
    }
}
