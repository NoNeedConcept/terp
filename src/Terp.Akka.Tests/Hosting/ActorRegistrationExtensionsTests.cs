using Akka.Actor;
using Akka.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terp.Akka.Hosting;
using Terp.Hosting;

namespace Terp.Akka.Tests;

public sealed class ActorRegistrationExtensionsTests
{
    private sealed class MarkerA;

    private sealed class MarkerB;

    private sealed class EchoActor : ReceiveActor
    {
        public EchoActor()
        {
            ReceiveAny(m => Sender.Tell(m));
        }
    }

    private sealed class RegistrationA : IActorRegistration
    {
        public void Configure(AkkaConfigurationBuilder builder, IServiceProvider provider)
        {
            builder.WithActors((system, registry, resolver) =>
                registry.Register<MarkerA>(system.ActorOf(resolver.Props<EchoActor>(), "a")));
        }
    }

    private sealed class RegistrationB : IActorRegistration
    {
        public void Configure(AkkaConfigurationBuilder builder, IServiceProvider provider)
        {
            builder.WithActors((system, registry, resolver) =>
                registry.Register<MarkerB>(system.ActorOf(resolver.Props<EchoActor>(), "b")));
        }
    }

    private sealed class OrderTrackingRegistration(List<string> log, string label) : IActorRegistration
    {
        public void Configure(AkkaConfigurationBuilder builder, IServiceProvider provider)
        {
            log.Add(label);
        }
    }

    private sealed class TestActorSystemSetup(Action<AkkaConfigurationBuilder, IServiceProvider> configure) : ActorSystemSetup
    {
        protected override string ActorSystemName => "reg-test";

        protected override void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider serviceProvider)
        {
            configure(builder, serviceProvider);
        }
    }

    [Fact]
    public void AddActorRegistration_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddActorRegistration<RegistrationA>());

        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddActorRegistration_AddsDescriptor()
    {
        var services = new ServiceCollection();

        services.AddActorRegistration<RegistrationA>();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IActorRegistration));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(RegistrationA), descriptor.ImplementationType);
    }

    [Fact]
    public void AddActorRegistration_MultipleRegistrations_AddsAll()
    {
        var services = new ServiceCollection();

        services.AddActorRegistration<RegistrationA>();
        services.AddActorRegistration<RegistrationB>();

        Assert.Equal(2, services.Count(d => d.ServiceType == typeof(IActorRegistration)));
    }

    [Fact(Timeout = 30000)]
    public async Task ApplyRegistrations_SingleRegistration_ConfiguresActor()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddActorRegistration<RegistrationA>();
        builder.Setup(new TestActorSystemSetup((b, p) => b.ApplyRegistrations(p)));
        using var host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal("a", host.Services.GetRequiredService<IActorRegistry>().Get<MarkerA>().Path.Name);
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Timeout = 30000)]
    public async Task ApplyRegistrations_MultipleRegistrations_ConfiguresBoth()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddActorRegistration<RegistrationA>();
        builder.Services.AddActorRegistration<RegistrationB>();
        builder.Setup(new TestActorSystemSetup((b, p) => b.ApplyRegistrations(p)));
        using var host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal("a", host.Services.GetRequiredService<IActorRegistry>().Get<MarkerA>().Path.Name),
            () => Assert.Equal("b", host.Services.GetRequiredService<IActorRegistry>().Get<MarkerB>().Path.Name));
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void ApplyRegistrations_RespectsRegistrationOrder()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IActorRegistration>(new OrderTrackingRegistration(log, "first"));
        services.AddSingleton<IActorRegistration>(new OrderTrackingRegistration(log, "second"));
        services.AddSingleton<IActorRegistration>(new OrderTrackingRegistration(log, "third"));
        var provider = services.BuildServiceProvider();
        var builder = new AkkaConfigurationBuilder(services, "order-test");

        builder.ApplyRegistrations(provider);

        Assert.Equal(["first", "second", "third"], log);
    }

    [Fact]
    public void ApplyRegistrations_NoRegistrations_IsNoOp()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var builder = new AkkaConfigurationBuilder(services, "noop-test");

        var result = builder.ApplyRegistrations(provider);

        Assert.Same(builder, result);
    }

    [Fact]
    public void ApplyRegistrations_NullBuilder_ThrowsArgumentNullException()
    {
        AkkaConfigurationBuilder builder = null!;
        var provider = new ServiceCollection().BuildServiceProvider();

        var ex = Assert.Throws<ArgumentNullException>(() => builder.ApplyRegistrations(provider));

        Assert.Equal("builder", ex.ParamName);
    }

    [Fact]
    public void ApplyRegistrations_NullProvider_ThrowsArgumentNullException()
    {
        var builder = new AkkaConfigurationBuilder(new ServiceCollection(), "null-test");

        var ex = Assert.Throws<ArgumentNullException>(() => builder.ApplyRegistrations(null!));

        Assert.Equal("provider", ex.ParamName);
    }
}
