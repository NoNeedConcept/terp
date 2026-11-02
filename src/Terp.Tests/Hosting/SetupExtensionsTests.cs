using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terp.Hosting;

namespace Terp.Tests.Hosting;

public sealed class SetupExtensionsTests
{
    private sealed class RecordingSetup(string name, List<string> log) : ISetup<IHostApplicationBuilder>
    {
        public void Setup(IHostApplicationBuilder target, SetupContext context)
        {
            log.Add(name);
        }
    }

    private sealed class HostSetup(string name, List<string> log) : ISetup<IHost>
    {
        public void Setup(IHost target, SetupContext context)
        {
            log.Add(name);
        }
    }

    private sealed class ServiceSetup : ISetup<IServiceCollection>
    {
        public void Setup(IServiceCollection target, SetupContext context)
        {
            target.AddSingleton<Marker>();
        }
    }

    private sealed class Marker;

    private sealed class DefaultHostSetup : ISetup<IHost>
    {
        public static int Calls;

        public void Setup(IHost target, SetupContext context)
        {
            Calls++;
        }
    }

    [Fact]
    public void Add_ChainedBuilderSetups_RunInCallOrder()
    {
        List<string> log = [];
        var builder = Host.CreateApplicationBuilder();

        builder.Setups()
            .Add(new RecordingSetup("a", log))
            .Add(new RecordingSetup("b", log))
            .Add(new RecordingSetup("c", log));

        Assert.Equal(["a", "b", "c"], log);
    }

    [Fact]
    public void AddGeneric_BuilderSetupType_CreatesAndAppliesIt()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Setups().Add<ServiceSetup>();

        using var host = builder.Build();
        Assert.NotNull(host.Services.GetService<Marker>());
    }

    [Fact]
    public void Add_ChainedHostSetups_RunInCallOrder()
    {
        List<string> log = [];
        using var host = Host.CreateApplicationBuilder().Build();

        host.Setups()
            .Add(new HostSetup("first", log))
            .Add(new HostSetup("second", log));

        Assert.Equal(["first", "second"], log);
    }

    [Fact]
    public void AddGeneric_HostSetupType_CreatesAndAppliesIt()
    {
        var before = DefaultHostSetup.Calls;
        using var host = Host.CreateApplicationBuilder().Build();

        host.Setups().Add<DefaultHostSetup>();

        Assert.Equal(before + 1, DefaultHostSetup.Calls);
    }

    [Fact]
    public void Add_NullSetup_ThrowsArgumentNullException()
    {
        var builder = Host.CreateApplicationBuilder();
        using var host = builder.Build();

        var builderEx = Assert.Throws<ArgumentNullException>(() => builder.Setups().Add(null!));
        var hostEx = Assert.Throws<ArgumentNullException>(() => host.Setups().Add(null!));

        Assert.Multiple(
            () => Assert.Equal("setup", builderEx.ParamName),
            () => Assert.Equal("setup", hostEx.ParamName));
    }

    [Fact]
    public void Setups_NullBuilder_ThrowsArgumentNullException()
    {
        IHostApplicationBuilder builder = null!;

        var ex = Assert.Throws<ArgumentNullException>(() => builder.Setups());

        Assert.Equal("builder", ex.ParamName);
    }

    [Fact]
    public void Setups_NullApplication_ThrowsArgumentNullException()
    {
        IHost app = null!;

        var ex = Assert.Throws<ArgumentNullException>(() => app.Setups());

        Assert.Equal("app", ex.ParamName);
    }
}
