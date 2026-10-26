using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Terp.Tests.Hosting;

/// <summary>A second builder type, delegating to a real builder, to test containers that require a specific builder.</summary>
public sealed class FakeBuilder(HostApplicationBuilder inner) : IHostApplicationBuilder
{
    public IDictionary<object, object> Properties => ((IHostApplicationBuilder)inner).Properties;

    public IConfigurationManager Configuration => inner.Configuration;

    public IHostEnvironment Environment => inner.Environment;

    public ILoggingBuilder Logging => inner.Logging;

    public IMetricsBuilder Metrics => inner.Metrics;

    public IServiceCollection Services => inner.Services;

    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null)
        where TContainerBuilder : notnull
    {
        inner.ConfigureContainer(factory, configure);
    }
}
