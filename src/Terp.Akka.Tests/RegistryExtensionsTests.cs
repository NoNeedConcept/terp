using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;

namespace Terp.Akka.Tests;

public sealed class RegistryExtensionsTests : TestKit
{
    public sealed class RegisteredKey;

    public sealed class UnregisteredKey;

    public sealed class LateKey;

    public sealed class EchoActor : ReceiveActor
    {
        public EchoActor()
        {
            Receive<string>(msg => Sender.Tell(msg));
        }
    }

    /// <summary>Runs a lookup inside the actor so it gets a real <see cref="IActorContext"/>.</summary>
    public sealed record Lookup(Func<IActorContext, object> Run);

    /// <summary>Starts an async lookup of <see cref="RegisteredKey"/> or <see cref="LateKey"/> from the actor context.</summary>
    public sealed record AsyncLookup(bool Late);

    public sealed class ContextActor : ReceiveActor
    {
        public ContextActor()
        {
            Receive<Lookup>(msg =>
            {
                try
                {
                    Sender.Tell(msg.Run(Context));
                }
                catch (Exception ex)
                {
                    Sender.Tell(new Status.Failure(ex));
                }
            });

            Receive<AsyncLookup>(msg =>
            {
                var task = msg.Late ? Context.GetActorAsync<LateKey>() : Context.GetActorAsync<RegisteredKey>();
                task.PipeTo(Sender);
            });
        }
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, registry) =>
        {
            registry.Register<RegisteredKey>(system.ActorOf(Props.Create<EchoActor>(), "registered"));
            registry.Register<ContextActor>(system.ActorOf(Props.Create<ContextActor>(), "ctx"));
        });
    }

    private Task<object> InContext(Func<IActorContext, object> run)
    {
        return Sys.GetActor<ContextActor>().Ask<object>(new Lookup(run), TestContext.Current.CancellationToken);
    }

    private IActorRef Registered => ActorRegistry.For(Sys).Get<RegisteredKey>();

    [Fact]
    public void GetRegistry_ActorSystem_ReturnsSystemsRegistry()
    {
        Assert.Same(ActorRegistry.For(Sys), Sys.GetRegistry());
    }

    [Fact]
    public void GetActor_SystemWithRegisteredActor_ReturnsRegisteredActor()
    {
        Assert.Equal(Registered, Sys.GetActor<RegisteredKey>());
    }

    [Fact]
    public void GetActor_SystemWithUnregisteredActor_ThrowsMissingActorRegistryEntryException()
    {
        Assert.Throws<MissingActorRegistryEntryException>(() => Sys.GetActor<UnregisteredKey>());
    }

    [Fact]
    public async Task GetActor_ContextWithRegisteredActor_ReturnsRegisteredActor()
    {
        var result = await InContext(ctx => ctx.GetActor<RegisteredKey>());

        Assert.Equal(Registered, result);
    }

    [Fact]
    public async Task GetActor_ContextWithUnregisteredActor_ThrowsMissingActorRegistryEntryException()
    {
        // Ask surfaces the Status.Failure sent back by the actor as the original exception.
        await Assert.ThrowsAsync<MissingActorRegistryEntryException>(() => InContext(ctx => ctx.GetActor<UnregisteredKey>()));
    }

    [Fact]
    public void TryGetActor_SystemWithRegisteredActor_ReturnsTrueAndActor()
    {
        Assert.True(Sys.TryGetActor<RegisteredKey>(out var actor));
        Assert.Equal(Registered, actor);
    }

    [Fact]
    public void TryGetActor_SystemWithUnregisteredActor_ReturnsFalseAndNobody()
    {
        Assert.False(Sys.TryGetActor<UnregisteredKey>(out var actor));
        Assert.Equal(ActorRefs.Nobody, actor);
    }

    [Fact]
    public async Task TryGetActor_ContextRegisteredAndUnregistered_FindsRegisteredOnly()
    {
        var result = await InContext(ctx =>
        {
            var found = ctx.TryGetActor<RegisteredKey>(out var actor);
            var missing = ctx.TryGetActor<UnregisteredKey>(out var none);
            return (found, actor, missing, none);
        });

        var (found, actor, missing, none) = ((bool, IActorRef, bool, IActorRef))result;
        Assert.True(found);
        Assert.Equal(Registered, actor);
        Assert.False(missing);
        Assert.Equal(ActorRefs.Nobody, none);
    }

    [Fact]
    public void TryGetActor_SystemByTypeRegisteredAndUnregistered_FindsRegisteredOnly()
    {
        Assert.True(Sys.TryGetActor(typeof(RegisteredKey), out var actor));
        Assert.Equal(Registered, actor);

        Assert.False(Sys.TryGetActor(typeof(UnregisteredKey), out var none));
        Assert.Equal(ActorRefs.Nobody, none);
    }

    [Fact]
    public async Task TryGetActor_ContextByTypeRegisteredAndUnregistered_FindsRegisteredOnly()
    {
        var result = await InContext(ctx =>
        {
            var found = ctx.TryGetActor(typeof(RegisteredKey), out var actor);
            var missing = ctx.TryGetActor(typeof(UnregisteredKey), out var none);
            return (found, actor, missing, none);
        });

        var (found, actor, missing, none) = ((bool, IActorRef, bool, IActorRef))result;
        Assert.True(found);
        Assert.Equal(Registered, actor);
        Assert.False(missing);
        Assert.Equal(ActorRefs.Nobody, none);
    }

    [Fact]
    public async Task GetActorAsync_SystemWithRegisteredActor_ReturnsRegisteredActor()
    {
        var actor = await Sys.GetActorAsync<RegisteredKey>();

        Assert.Equal(Registered, actor);
    }

    [Fact]
    public async Task GetActorAsync_ContextWithRegisteredActor_ReturnsRegisteredActor()
    {
        var actor = await Sys.GetActor<ContextActor>()
            .Ask<IActorRef>(new AsyncLookup(Late: false), TestContext.Current.CancellationToken);

        Assert.Equal(Registered, actor);
    }

    [Fact]
    public async Task GetActorAsync_SystemActorRegisteredLater_CompletesOnceRegistered()
    {
        var pending = Sys.GetActorAsync<LateKey>();
        Assert.False(pending.IsCompleted);

        var late = Sys.ActorOf(Props.Create<EchoActor>(), "late");
        ActorRegistry.For(Sys).Register<LateKey>(late);

        var actor = await pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(late, actor);
    }

    [Fact]
    public async Task GetActorAsync_ContextActorRegisteredLater_CompletesOnceRegistered()
    {
        var pending = Sys.GetActor<ContextActor>()
            .Ask<IActorRef>(new AsyncLookup(Late: true), TestContext.Current.CancellationToken);
        Assert.False(pending.IsCompleted);

        var late = Sys.ActorOf(Props.Create<EchoActor>(), "late-ctx");
        ActorRegistry.For(Sys).Register<LateKey>(late);

        var actor = await pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(late, actor);
    }

    [Fact]
    public void RegistryExtensions_NullContextSystemOrKey_ThrowsArgumentNullException()
    {
        var context = (IActorContext)null!;
        var system = (ActorSystem)null!;

        AssertParamName("context", () => context.GetActor<RegisteredKey>());
        AssertParamName("system", () => system.GetActor<RegisteredKey>());
        AssertParamName("context", () => context.TryGetActor<RegisteredKey>(out _));
        AssertParamName("system", () => system.TryGetActor<RegisteredKey>(out _));
        AssertParamName("context", () => context.TryGetActor(typeof(RegisteredKey), out _));
        AssertParamName("system", () => system.TryGetActor(typeof(RegisteredKey), out _));
        AssertParamName("key", () => Sys.TryGetActor(null!, out _));
        AssertParamName("context", () => { _ = context.GetActorAsync<RegisteredKey>(); });
        AssertParamName("system", () => { _ = system.GetActorAsync<RegisteredKey>(); });
        AssertParamName("system", () => system.GetRegistry());
    }

    private static void AssertParamName(string expected, Action act)
    {
        var ex = Assert.Throws<ArgumentNullException>(act);

        Assert.Equal(expected, ex.ParamName);
    }
}
