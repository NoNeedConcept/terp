using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;

namespace Terp.Akka.Tests;

public sealed class EntityRegionActorTests : TestKit
{
    public sealed record TestCommand(string EntityId, string Payload) : IWithEntityId;
    public sealed record EchoResponse(string EntityId, string Payload);

    public sealed class TestEntityActor : ReceiveActor
    {
        public TestEntityActor()
        {
            Receive<TestCommand>(msg => Sender.Tell(new EchoResponse(msg.EntityId, msg.Payload)));
        }
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    private IActorRef CreateRegion(EntityRegionOptions? options = null, TimeProvider? timeProvider = null)
    {
        options ??= new EntityRegionOptions();
        var props = Props.Create(() => new EntityRegionActor(
            (context, entityId) => context.ActorOf(Props.Create<TestEntityActor>(), entityId),
            EntityIdExtractor.Instance,
            options,
            "Test",
            timeProvider));

        return Sys.ActorOf(props, $"region-{Guid.NewGuid():N}");
    }

    [Fact(Timeout = 10000)]
    public async Task HandleMessage_FirstMessage_SpawnsEntityAndForwards()
    {
        var region = CreateRegion();

        region.Tell(new TestCommand("order-42", "hello"), TestActor);

        var response = await ExpectMsgAsync<EchoResponse>();
        Assert.Multiple(
            () => Assert.Equal("order-42", response.EntityId),
            () => Assert.Equal("hello", response.Payload));
    }

    [Fact(Timeout = 10000)]
    public async Task HandleMessage_SubsequentMessage_RoutesToExistingEntity()
    {
        var region = CreateRegion();

        region.Tell(new TestCommand("order-42", "first"), TestActor);
        await ExpectMsgAsync<EchoResponse>();

        region.Tell(new TestCommand("order-42", "second"), TestActor);
        var response = await ExpectMsgAsync<EchoResponse>();
        Assert.Equal("second", response.Payload);
    }

    [Fact(Timeout = 10000)]
    public async Task HandleMessage_MultipleEntities_RoutedCorrectly()
    {
        var region = CreateRegion();

        region.Tell(new TestCommand("a", "msg-a"), TestActor);
        region.Tell(new TestCommand("b", "msg-b"), TestActor);

        var responses = new List<EchoResponse>
        {
            await ExpectMsgAsync<EchoResponse>(),
            await ExpectMsgAsync<EchoResponse>()
        };

        Assert.Contains(responses, r => r.EntityId == "a" && r.Payload == "msg-a");
        Assert.Contains(responses, r => r.EntityId == "b" && r.Payload == "msg-b");
    }

    [Fact(Timeout = 10000)]
    public async Task HandleMessage_NullEntityId_IsUnhandled()
    {
        var region = CreateRegion();
        var probe = CreateTestProbe();
        Sys.EventStream.Subscribe(probe, typeof(global::Akka.Event.UnhandledMessage));

        region.Tell("plain-string");

        await probe.ExpectMsgAsync<global::Akka.Event.UnhandledMessage>();
    }

    [Fact(Timeout = 10000)]
    public async Task Passivation_IdleEntity_IsStopped()
    {
        var options = new EntityRegionOptions
        {
            PassivationTimeout = TimeSpan.FromMilliseconds(100),
            PassivationInterval = TimeSpan.FromMilliseconds(50)
        };
        var region = CreateRegion(options);

        region.Tell(new TestCommand("idle-1", "hi"), TestActor);
        await ExpectMsgAsync<EchoResponse>();

        var ct = TestContext.Current.CancellationToken;
        await Task.Delay(500, ct);

        region.Tell(new TestCommand("idle-1", "after"), TestActor);
        var response = await ExpectMsgAsync<EchoResponse>();
        Assert.Equal("after", response.Payload);
    }

    [Fact(Timeout = 10000)]
    public async Task MaxEntities_AtLimit_SendsToDeadLetters()
    {
        var options = new EntityRegionOptions { MaxEntities = 2 };
        var region = CreateRegion(options);
        var deadLetterProbe = CreateTestProbe();
        Sys.EventStream.Subscribe(deadLetterProbe, typeof(global::Akka.Event.DeadLetter));

        region.Tell(new TestCommand("a", "1"), TestActor);
        region.Tell(new TestCommand("b", "2"), TestActor);
        await ExpectMsgAsync<EchoResponse>(cancellationToken: TestContext.Current.CancellationToken);
        await ExpectMsgAsync<EchoResponse>(cancellationToken: TestContext.Current.CancellationToken);

        region.Tell(new TestCommand("c", "3"), TestActor);

        await deadLetterProbe.ExpectMsgAsync<global::Akka.Event.DeadLetter>(cancellationToken: TestContext.Current.CancellationToken);
    }
}
