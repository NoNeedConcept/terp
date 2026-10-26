using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terp.Hosting;

namespace Terp.Tests.Hosting;

public sealed class BuilderAndApplicationSetupTests
{
    private sealed class FakeBuilderSetup : ISetup<FakeBuilder>
    {
        public FakeBuilder? Received { get; private set; }

        public void Setup(FakeBuilder target, SetupContext context)
        {
            Received = target;
        }
    }

    private sealed class AnyBuilderSetup : ISetup<IHostApplicationBuilder>
    {
        public IHostApplicationBuilder? Received { get; private set; }

        public void Setup(IHostApplicationBuilder target, SetupContext context)
        {
            Received = target;
        }
    }

    private sealed class ServicesAndFakeBuilderSetup(List<string> log) : ISetup<IServiceCollection>, ISetup<FakeBuilder>
    {
        public void Setup(IServiceCollection target, SetupContext context)
        {
            log.Add("services");
        }

        public void Setup(FakeBuilder target, SetupContext context)
        {
            log.Add("fakebuilder");
        }
    }

    private sealed class FakeAppSetup : ISetup<FakeApp>
    {
        public static int Calls;

        public static SetupContext? LastContext;

        public void Setup(FakeApp target, SetupContext context)
        {
            Calls++;
            LastContext = context;
        }
    }

    private sealed class AnyHostSetup : ISetup<IHost>
    {
        public SetupContext? Context { get; private set; }

        public void Setup(IHost target, SetupContext context)
        {
            Context = context;
        }
    }

    private static FakeApp BuildFakeApp()
    {
        return new FakeApp(Host.CreateApplicationBuilder().Build());
    }

    [Fact]
    public void Setup_SetupForAnyBuilder_RunsWithAnotherBuilderTypeAndReceivesIt()
    {
        var builder = new FakeBuilder(Host.CreateApplicationBuilder());
        var setup = new AnyBuilderSetup();

        builder.Setup(setup);

        Assert.Same(builder, setup.Received);
    }

    [Fact]
    public void Setup_SetupForConcreteBuilder_ReceivesTheConcreteBuilder()
    {
        var builder = new FakeBuilder(Host.CreateApplicationBuilder());
        var setup = new FakeBuilderSetup();

        builder.Setup(setup);

        Assert.Same(builder, setup.Received);
    }

    [Fact]
    public void Setup_SetupForOtherBuilderType_ThrowsInvalidOperationExceptionNamingBothTypes()
    {
        var builder = Host.CreateApplicationBuilder();

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Setup(new FakeBuilderSetup()));

        Assert.Multiple(
            () => Assert.Contains(typeof(FakeBuilderSetup).FullName!, ex.Message, StringComparison.Ordinal),
            () => Assert.Contains(typeof(HostApplicationBuilder).FullName!, ex.Message, StringComparison.Ordinal));
    }

    [Fact]
    public void Setup_OtherBuilderTypeAlongsideBuiltInTarget_IgnoresTheMismatchingBuilderTarget()
    {
        List<string> log = [];
        var builder = Host.CreateApplicationBuilder();

        builder.Setup(new ServicesAndFakeBuilderSetup(log));

        Assert.Equal(["services"], log);
    }

    [Fact]
    public void Setup_ApplicationSetupForConcreteApplication_ReturnsTheApplicationWithConfigurationInContext()
    {
        var before = FakeAppSetup.Calls;
        using var app = BuildFakeApp();

        var returned = app.Setup(new FakeAppSetup());

        Assert.Multiple(
            () => Assert.Same(app, returned),
            () => Assert.Equal(before + 1, FakeAppSetup.Calls),
            () => Assert.Same(app.Services.GetRequiredService<IConfiguration>(), FakeAppSetup.LastContext?.Configuration));
    }

    [Fact]
    public void Setup_ApplicationSetupByTypeArguments_CreatesAndAppliesIt()
    {
        var before = FakeAppSetup.Calls;
        using var app = BuildFakeApp();

        var returned = app.Setup<FakeApp, FakeAppSetup>();

        Assert.Same(app, returned);
        Assert.Equal(before + 1, FakeAppSetup.Calls);
    }

    [Fact]
    public void Setup_SetupForAnyHost_RunsOnAConcreteApplication()
    {
        using var app = BuildFakeApp();
        var setup = new AnyHostSetup();

        app.Setup(setup);

        Assert.NotNull(setup.Context?.Configuration);
    }

    [Fact]
    public void Setup_ApplicationSetupForOtherApplicationType_ThrowsInvalidOperationExceptionNamingBothTypes()
    {
        using var host = Host.CreateApplicationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(() => host.Setup(new FakeAppSetup()));

        Assert.Multiple(
            () => Assert.Contains(typeof(FakeAppSetup).FullName!, ex.Message, StringComparison.Ordinal),
            () => Assert.Contains($"ISetup<{typeof(IHost).FullName}>", ex.Message, StringComparison.Ordinal));
    }
}
