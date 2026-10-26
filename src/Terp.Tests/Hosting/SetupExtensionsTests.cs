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
    public void Setup_ChainedBuilderSetups_RunInCallOrderAndReturnSameBuilder()
    {
        List<string> log = [];
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        HostApplicationBuilder returned = builder
            .Setup(new RecordingSetup("a", log))
            .Setup(new RecordingSetup("b", log))
            .Setup(new RecordingSetup("c", log));

        Assert.Same(builder, returned);
        Assert.Equal(["a", "b", "c"], log);
    }

    [Fact]
    public void SetupGeneric_BuilderSetupType_CreatesAndAppliesIt()
    {
        var builder = Host.CreateApplicationBuilder();

        var returned = builder.Setup<ServiceSetup>();

        Assert.Same(builder, returned);
        using var host = builder.Build();
        Assert.NotNull(host.Services.GetService<Marker>());
    }

    [Fact]
    public void Setup_ChainedHostSetups_RunInCallOrderAndReturnSameHost()
    {
        List<string> log = [];
        using var host = Host.CreateApplicationBuilder().Build();

        var returned = host
            .Setup(new HostSetup("first", log))
            .Setup(new HostSetup("second", log));

        Assert.Same(host, returned);
        Assert.Equal(["first", "second"], log);
    }

    [Fact]
    public void SetupGeneric_HostSetupType_CreatesAndAppliesIt()
    {
        var before = DefaultHostSetup.Calls;
        using var host = Host.CreateApplicationBuilder().Build();

        var returned = host.Setup<DefaultHostSetup>();

        Assert.Same(host, returned);
        Assert.Equal(before + 1, DefaultHostSetup.Calls);
    }

    [Fact]
    public void Setup_NullSetup_ThrowsArgumentNullException()
    {
        var builder = Host.CreateApplicationBuilder();
        using var host = builder.Build();

        var builderEx = Assert.Throws<ArgumentNullException>(() => builder.Setup((ISetup)null!));
        var hostEx = Assert.Throws<ArgumentNullException>(() => host.Setup((ISetup)null!));

        Assert.Multiple(
            () => Assert.Equal("setup", builderEx.ParamName),
            () => Assert.Equal("setup", hostEx.ParamName));
    }
}
