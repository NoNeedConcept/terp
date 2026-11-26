using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Terp.Hosting;

namespace Terp.Example;

internal sealed class DemoSetup
    : ISetup<IConfigurationManager>, ISetup<ILoggingBuilder>, ISetup<IServiceCollection>, ISetup<IMetricsBuilder>, ISetup<IHostApplicationBuilder>
{
    public void Setup(IConfigurationManager configuration, SetupContext context)
    {
        SetupTrace.Add(nameof(IConfigurationManager));
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Demo:Greeting"] = "Hello from a Native AOT host",
            ["Demo:GateAttempts"] = "2",
        });
    }

    public void Setup(ILoggingBuilder logging, SetupContext context)
    {
        SetupTrace.Add(nameof(ILoggingBuilder));
        logging.ClearProviders().AddSimpleConsole(o => o.SingleLine = true);
        logging.AddFilter("Microsoft", LogLevel.Warning);
    }

    public void Setup(IServiceCollection services, SetupContext context)
    {
        SetupTrace.Add(nameof(IServiceCollection));
        services.AddSingleton(new AppInfo(context.Environment.ApplicationName, context.Configuration["Demo:Greeting"] ?? "no greeting"));
    }

    public void Setup(IMetricsBuilder metrics, SetupContext context)
    {
        SetupTrace.Add(nameof(IMetricsBuilder));
        metrics.AddListener(new ConsoleMetricsListener());
        metrics.EnableMetrics(DemoWorker.MeterName);
        metrics.EnableMetrics("System.Net.Http");
    }

    public void Setup(IHostApplicationBuilder builder, SetupContext context)
    {
        SetupTrace.Add(nameof(IHostApplicationBuilder));
        builder.Services.AddHostedService<DemoWorker>();
    }
}
