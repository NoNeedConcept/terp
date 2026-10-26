using Akka.Actor;
using Akka.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terp.Hosting;

namespace Terp.Akka.Tests;

public sealed class ActorSystemSetupTests
{
    public sealed class TestActorSystemSetup(string name = "test-system") : ActorSystemSetup
    {
        protected override string ActorSystemName => name;

        protected override void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider serviceProvider)
        {
        }
    }

    [Fact(Timeout = 30000)]
    public async Task Setup_ValidSetup_RegistersActorSystemAvailableOnStart()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Setup(new TestActorSystemSetup());
        using var host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal("test-system", host.Services.GetRequiredService<ActorSystem>().Name);
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Setup_SecondActorSystemSetup_ThrowsInvalidOperationException()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Setup(new TestActorSystemSetup("first"));

        var ex = Assert.Throws<InvalidOperationException>(
            () => builder.Setup(new TestActorSystemSetup("second")));

        Assert.Contains("second", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Setup_SameContainerAppliedTwice_ThrowsInvalidOperationException()
    {
        var builder = Host.CreateApplicationBuilder();
        var container = new TestActorSystemSetup();
        builder.Setup(container);

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Setup(container));

        Assert.NotEmpty(ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("-leading-dash")]
    [InlineData("_leading-underscore")]
    [InlineData("has space")]
    [InlineData("has/slash")]
    [InlineData("dot.ted")]
    [InlineData("umlaut-ä")]
    public void Setup_InvalidActorSystemName_ThrowsArgumentException(string? name)
    {
        var builder = Host.CreateApplicationBuilder();

        var ex = Assert.ThrowsAny<ArgumentException>(() => builder.Setup(new TestActorSystemSetup(name!)));

        Assert.Equal("name", ex.ParamName);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("1abc")]
    [InlineData("my-system_2")]
    public void Setup_ValidActorSystemName_DoesNotThrow(string name)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Setup(new TestActorSystemSetup(name));
    }

    [Fact]
    public void Setup_NullServices_ThrowsArgumentNullException()
    {
        var environment = Host.CreateApplicationBuilder().Environment;

        var ex = Assert.Throws<ArgumentNullException>(() => new TestActorSystemSetup().Setup(null!, new SetupContext(new ConfigurationBuilder().Build(), environment)));

        Assert.Equal("services", ex.ParamName);
    }

    private sealed class EchoActor : ReceiveActor
    {
        public EchoActor()
        {
            ReceiveAny(m => Sender.Tell(m));
        }
    }

    private sealed class EchoMarker;

    private sealed class ActorRegisteringSetup : ActorSystemSetup
    {
        public IServiceProvider? Provider { get; private set; }

        protected override string ActorSystemName => "building";

        protected override void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider serviceProvider)
        {
            Provider = serviceProvider;
            builder.WithActors((system, registry, _) =>
                registry.Register<EchoMarker>(system.ActorOf(Props.Create(() => new EchoActor()), "echo")));
        }
    }

    [Fact(Timeout = 30000)]
    public async Task Setup_BuildSystem_ReceivesTheHostProviderAndConfiguresTheSystem()
    {
        var setup = new ActorRegisteringSetup();
        var builder = Host.CreateApplicationBuilder();
        builder.Setup(setup);
        using var host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal("echo", host.Services.GetRequiredService<IActorRegistry>().Get<EchoMarker>().Path.Name),
            () => Assert.NotNull(setup.Provider));
        await host.StopAsync(TestContext.Current.CancellationToken);
    }
}
