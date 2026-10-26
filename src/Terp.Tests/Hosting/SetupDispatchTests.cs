using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Terp.Hosting;

namespace Terp.Tests.Hosting;

public sealed class SetupDispatchTests
{
    private sealed class AllTargetsSetup(string name, List<string> log)
        : ISetup<IHostApplicationBuilder>, ISetup<IMetricsBuilder>, ISetup<IServiceCollection>, ISetup<ILoggingBuilder>, ISetup<IConfigurationManager>
    {
        public void Setup(IHostApplicationBuilder target, SetupContext context)
        {
            log.Add($"{name}.builder");
        }

        public void Setup(IMetricsBuilder target, SetupContext context)
        {
            log.Add($"{name}.metrics");
        }

        public void Setup(IServiceCollection target, SetupContext context)
        {
            log.Add($"{name}.services");
        }

        public void Setup(ILoggingBuilder target, SetupContext context)
        {
            log.Add($"{name}.logging");
        }

        public void Setup(IConfigurationManager target, SetupContext context)
        {
            log.Add($"{name}.configuration");
        }
    }

    private sealed class ConfigurationAndServicesSetup(string name, List<string> log) : ISetup<IServiceCollection>, ISetup<IConfigurationManager>
    {
        public void Setup(IServiceCollection target, SetupContext context)
        {
            log.Add($"{name}.services");
        }

        public void Setup(IConfigurationManager target, SetupContext context)
        {
            log.Add($"{name}.configuration");
        }
    }

    private sealed class ServicesOnlySetup : ISetup<IServiceCollection>
    {
        public IServiceCollection? Services { get; private set; }

        public SetupContext? Context { get; private set; }

        public void Setup(IServiceCollection target, SetupContext context)
        {
            Services = target;
            Context = context;
        }
    }

    private sealed class MarkerOnlySetup : ISetup;

    private sealed class SpySetup : ISetup<IConfigurationManager>, ISetup<ILoggingBuilder>, ISetup<IMetricsBuilder>
    {
        public IConfigurationManager? Configuration { get; private set; }

        public ILoggingBuilder? Logging { get; private set; }

        public IMetricsBuilder? Metrics { get; private set; }

        public void Setup(IConfigurationManager target, SetupContext context)
        {
            Configuration = target;
        }

        public void Setup(ILoggingBuilder target, SetupContext context)
        {
            Logging = target;
        }

        public void Setup(IMetricsBuilder target, SetupContext context)
        {
            Metrics = target;
        }
    }

    [Fact]
    public void Setup_SetupWithAllTargets_RunsTargetsByPhase()
    {
        List<string> log = [];
        var builder = Host.CreateApplicationBuilder();

        builder.Setup(new AllTargetsSetup("c", log));

        Assert.Equal(["c.configuration", "c.logging", "c.services", "c.metrics", "c.builder"], log);
    }

    [Fact]
    public void Setup_DirectCalls_RunImmediatelyInCallOrderInsteadOfPhaseMajor()
    {
        List<string> log = [];
        var builder = Host.CreateApplicationBuilder();

        builder
            .Setup(new ConfigurationAndServicesSetup("a", log))
            .Setup(new ConfigurationAndServicesSetup("b", log));

        Assert.Equal(["a.configuration", "a.services", "b.configuration", "b.services"], log);
    }

    [Fact]
    public void Setup_SetupWithoutAnyTarget_ThrowsInvalidOperationExceptionNamingTypes()
    {
        var builder = Host.CreateApplicationBuilder();

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Setup(new MarkerOnlySetup()));

        Assert.Multiple(
            () => Assert.Contains(typeof(MarkerOnlySetup).FullName!, ex.Message, StringComparison.Ordinal),
            () => Assert.Contains(typeof(HostApplicationBuilder).FullName!, ex.Message, StringComparison.Ordinal),
            () => Assert.Contains("ISetup<WebApplication>", ex.Message, StringComparison.Ordinal));
    }

    [Fact]
    public void Setup_ServicesOnlySetup_ReceivesOnlyServicesAndAReadOnlyContext()
    {
        var builder = Host.CreateApplicationBuilder();
        var setup = new ServicesOnlySetup();

        builder.Setup(setup);

        Assert.Multiple(
            () => Assert.Same(builder.Services, setup.Services),
            () => Assert.Same(builder.Configuration, setup.Context?.Configuration),
            () => Assert.Same(builder.Environment, setup.Context?.Environment));
    }

    [Fact]
    public void Setup_NarrowTargets_ReceiveTheMatchingPartOfTheBuilder()
    {
        var builder = Host.CreateApplicationBuilder();
        var spy = new SpySetup();

        builder.Setup(spy);

        Assert.Multiple(
            () => Assert.Same(builder.Configuration, spy.Configuration),
            () => Assert.Same(builder.Logging, spy.Logging),
            () => Assert.Same(builder.Metrics, spy.Metrics));
    }
}
