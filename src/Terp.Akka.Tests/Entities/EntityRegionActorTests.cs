using System.Diagnostics.Metrics;
using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using Microsoft.Extensions.Time.Testing;
using Terp.Akka.Entities;

namespace Terp.Akka.Tests.Entities;

public sealed class EntityRegionActorTests : TestKit
{
    public sealed record TestCommand(string EntityId, string Payload) : IWithEntityId;
    public sealed record EchoResponse(string EntityId, string Payload);
    public sealed record CrashCommand(string EntityId) : IWithEntityId;

    public sealed class TestEntityActor : ReceiveActor
    {
        public TestEntityActor()
        {
            Receive<TestCommand>(msg => Sender.Tell(new EchoResponse(msg.EntityId, msg.Payload)));
        }
    }

    public sealed class CrashableEntityActor : ReceiveActor
    {
        public CrashableEntityActor()
        {
            Receive<CrashCommand>(_ => Context.Stop(Self));
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

    private IActorRef CreateCrashableRegion(EntityRegionOptions? options = null)
    {
        options ??= new EntityRegionOptions();
        var props = Props.Create(() => new EntityRegionActor(
            (context, entityId) => context.ActorOf(Props.Create<CrashableEntityActor>(), entityId),
            EntityIdExtractor.Instance,
            options,
            "CrashTest"));

        return Sys.ActorOf(props, $"crash-region-{Guid.NewGuid():N}");
    }

    [Fact(Timeout = 10000)]
    public async Task HandleMessage_FirstMessage_SpawnsEntityAndForwards()
    {
        var region = CreateRegion();

        region.Tell(new TestCommand("order-42", "hello"), TestActor);

        var response = await ExpectMsgAsync<EchoResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Multiple(
            () => Assert.Equal("order-42", response.EntityId),
            () => Assert.Equal("hello", response.Payload));
    }

    [Fact(Timeout = 10000)]
    public async Task HandleMessage_SubsequentMessage_RoutesToExistingEntity()
    {
        var region = CreateRegion();

        region.Tell(new TestCommand("order-42", "first"), TestActor);
        await ExpectMsgAsync<EchoResponse>(cancellationToken: TestContext.Current.CancellationToken);

        region.Tell(new TestCommand("order-42", "second"), TestActor);
        var response = await ExpectMsgAsync<EchoResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("second", response.Payload);
    }

    [Fact(Timeout = 10000)]
    public async Task HandleMessage_MultipleEntities_RoutedCorrectly()
    {
        var region = CreateRegion();

        region.Tell(new TestCommand("a", "msg-a"), TestActor);
        region.Tell(new TestCommand("b", "msg-b"), TestActor);

        var ct = TestContext.Current.CancellationToken;
        var responses = new List<EchoResponse>
        {
            await ExpectMsgAsync<EchoResponse>(cancellationToken: ct),
            await ExpectMsgAsync<EchoResponse>(cancellationToken: ct)
        };

        Assert.Contains(responses, r => r is { EntityId: "a", Payload: "msg-a" });
        Assert.Contains(responses, r => r is { EntityId: "b", Payload: "msg-b" });
    }

    [Fact(Timeout = 10000)]
    public async Task HandleMessage_NullEntityId_IsUnhandled()
    {
        var region = CreateRegion();
        var probe = CreateTestProbe();
        Sys.EventStream.Subscribe(probe, typeof(global::Akka.Event.UnhandledMessage));

        region.Tell("plain-string");

        await probe.ExpectMsgAsync<global::Akka.Event.UnhandledMessage>(cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact(Timeout = 10000)]
    public async Task Passivation_IdleEntity_IsStopped()
    {
        var timeProvider = new FakeTimeProvider();
        var options = new EntityRegionOptions
        {
            PassivationTimeout = TimeSpan.FromMilliseconds(100),
            PassivationInterval = TimeSpan.FromMilliseconds(50)
        };
        var region = CreateRegion(options, timeProvider);
        var ct = TestContext.Current.CancellationToken;

        region.Tell(new TestCommand("idle-1", "hi"), TestActor);
        await ExpectMsgAsync<EchoResponse>(cancellationToken: ct);

        var entityRef = await Sys.ActorSelection(region.Path / "idle-1")
            .ResolveOne(TimeSpan.FromSeconds(3), ct);
        var probe = CreateTestProbe();
        probe.Watch(entityRef);

        timeProvider.Advance(TimeSpan.FromMilliseconds(200));
        await probe.ExpectTerminatedAsync(entityRef, cancellationToken: ct);

        region.Tell(new TestCommand("idle-1", "after"), TestActor);
        var afterResponse = await ExpectMsgAsync<EchoResponse>(cancellationToken: ct);
        Assert.Equal("after", afterResponse.Payload);
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

    [Fact(Timeout = 10000)]
    public async Task RestartLimit_WithinLimit_EntityRespawnsOnNextMessage()
    {
        var options = new EntityRegionOptions { MaxRestarts = 3, RestartWindow = TimeSpan.FromMinutes(1) };
        var region = CreateCrashableRegion(options);
        var ct = TestContext.Current.CancellationToken;

        region.Tell(new TestCommand("x", "first"), TestActor);
        await ExpectMsgAsync<EchoResponse>(cancellationToken: ct);

        var entityRef = await Sys.ActorSelection(region.Path / "x")
            .ResolveOne(TimeSpan.FromSeconds(3), ct);
        var probe = CreateTestProbe();
        probe.Watch(entityRef);

        region.Tell(new CrashCommand("x"));
        await probe.ExpectTerminatedAsync(entityRef, cancellationToken: ct);

        await AwaitAssertAsync(async () =>
        {
            region.Tell(new TestCommand("x", "after-crash"), TestActor);
            var response = await ExpectMsgAsync<EchoResponse>(TimeSpan.FromMilliseconds(300), cancellationToken: ct);
            Assert.Equal("after-crash", response.Payload);
        }, cancellationToken: ct);
    }

    [Fact(Timeout = 10000)]
    public async Task RestartLimit_ExceedsLimit_EntityPermanentlyStopped()
    {
        var options = new EntityRegionOptions { MaxRestarts = 2, RestartWindow = TimeSpan.FromMinutes(1) };
        var region = CreateCrashableRegion(options);
        var deadLetterProbe = CreateTestProbe();
        Sys.EventStream.Subscribe(deadLetterProbe, typeof(global::Akka.Event.DeadLetter));
        var ct = TestContext.Current.CancellationToken;

        region.Tell(new TestCommand("y", "init"), TestActor);
        await ExpectMsgAsync<EchoResponse>(cancellationToken: ct);

        for (var i = 0; i < 2; i++)
        {
            var entityRef = await Sys.ActorSelection(region.Path / "y")
                .ResolveOne(TimeSpan.FromSeconds(3), ct);
            var probe = CreateTestProbe();
            probe.Watch(entityRef);

            region.Tell(new CrashCommand("y"));
            await probe.ExpectTerminatedAsync(entityRef, cancellationToken: ct);

            if (i < 1)
            {
                await AwaitAssertAsync(async () =>
                {
                    region.Tell(new TestCommand("y", "alive"), TestActor);
                    await ExpectMsgAsync<EchoResponse>(TimeSpan.FromMilliseconds(300), cancellationToken: ct);
                }, cancellationToken: ct);
            }
        }

        await AwaitAssertAsync(async () =>
        {
            region.Tell(new TestCommand("y", "should-be-dead"), TestActor);
            await deadLetterProbe.ExpectMsgAsync<global::Akka.Event.DeadLetter>(
                TimeSpan.FromMilliseconds(300), cancellationToken: ct);
        }, cancellationToken: ct);
    }

    [Fact(Timeout = 10000)]
    public async Task Drain_NewMessages_SentToDeadLetters()
    {
        var region = CreateRegion();
        var deadLetterProbe = CreateTestProbe();
        Sys.EventStream.Subscribe(deadLetterProbe, typeof(global::Akka.Event.DeadLetter));
        var ct = TestContext.Current.CancellationToken;

        region.Tell(new TestCommand("a", "before-drain"), TestActor);
        await ExpectMsgAsync<EchoResponse>(cancellationToken: ct);

        region.Tell(EntityRegionActor.StartDrainCommand.Instance);
        region.Tell(new TestCommand("new-entity", "after-drain"), TestActor);
        await deadLetterProbe.ExpectMsgAsync<global::Akka.Event.DeadLetter>(cancellationToken: ct);
    }

    [Fact(Timeout = 10000)]
    public async Task Drain_ExistingEntities_ReceivePoisonPillAndRegionStops()
    {
        var region = CreateRegion();
        var ct = TestContext.Current.CancellationToken;
        var regionProbe = CreateTestProbe();

        region.Tell(new TestCommand("a", "alive"), TestActor);
        region.Tell(new TestCommand("b", "alive"), TestActor);
        await ExpectMsgAsync<EchoResponse>(cancellationToken: ct);
        await ExpectMsgAsync<EchoResponse>(cancellationToken: ct);

        regionProbe.Watch(region);
        region.Tell(EntityRegionActor.StartDrainCommand.Instance);

        await regionProbe.ExpectTerminatedAsync(region, cancellationToken: ct);
    }

    [Fact(Timeout = 10000)]
    public async Task Metrics_SpawnAndPassivation_CountersUpdated()
    {
        var activeDeltas = new List<int>();
        var passivationCount = 0L;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "Terp.Akka.EntityRegion")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<int>((instrument, value, _, _) =>
        {
            if (instrument.Name == "entity_region.active_entities")
            {
                activeDeltas.Add(value);
            }
        });
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name == "entity_region.passivations")
            {
                Interlocked.Add(ref passivationCount, value);
            }
        });
        listener.Start();

        var timeProvider = new FakeTimeProvider();
        var options = new EntityRegionOptions
        {
            PassivationTimeout = TimeSpan.FromMilliseconds(100),
            PassivationInterval = TimeSpan.FromMilliseconds(50)
        };
        var region = CreateRegion(options, timeProvider);
        var ct = TestContext.Current.CancellationToken;

        region.Tell(new TestCommand("m1", "hi"), TestActor);
        await ExpectMsgAsync<EchoResponse>(cancellationToken: ct);

        listener.RecordObservableInstruments();
        Assert.Contains(1, activeDeltas);

        var entityRef = await Sys.ActorSelection(region.Path / "m1")
            .ResolveOne(TimeSpan.FromSeconds(3), ct);
        var probe = CreateTestProbe();
        probe.Watch(entityRef);

        timeProvider.Advance(TimeSpan.FromMilliseconds(200));
        await probe.ExpectTerminatedAsync(entityRef, cancellationToken: ct);

        await AwaitAssertAsync(() =>
        {
            listener.RecordObservableInstruments();
            Assert.Contains(-1, activeDeltas);
            Assert.True(Interlocked.Read(ref passivationCount) >= 1);
        }, cancellationToken: ct);
    }
}
