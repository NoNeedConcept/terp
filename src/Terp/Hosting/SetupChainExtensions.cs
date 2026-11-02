using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Terp.Hosting;

/// <summary>
/// Starts a <see cref="SetupChain{TTarget}"/> on a builder before build. The concrete builder type is inferred from the
/// receiver and preserved across the chain, so <c>builder.Setups().Add&lt;X&gt;().Add&lt;Y&gt;()</c> keeps the type
/// (e.g. <c>WebApplicationBuilder</c>) without requiring the consumer to spell it out.
/// For built applications see <see cref="ApplicationSetupChainExtensions"/>.
/// </summary>
/// <remarks>
/// Each <see cref="SetupChain{TTarget}.Add{TSetup}"/> dispatches the setup to the builder's sub-targets in the fixed order:
/// <c>IConfigurationManager</c>, <c>ILoggingBuilder</c>, <c>IServiceCollection</c>, <c>IMetricsBuilder</c>, then the builder
/// type itself. The dispatch is pure pattern matching: reflection-free and trim/AOT-safe.
/// </remarks>
/// <example>
/// <code>
/// var builder = WebApplication.CreateBuilder(args);
/// builder.Setups()
///     .Add&lt;ConfigSetup&gt;()
///     .Add&lt;AkkaSetup&gt;();
/// var app = builder.Build();   // still WebApplication
/// </code>
/// </example>
public static class BuilderSetupChainExtensions
{
    /// <summary>
    /// Starts a setup chain for a builder.
    /// </summary>
    /// <typeparam name="TBuilder">The concrete builder type, inferred from the receiver.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>A chain that preserves <typeparamref name="TBuilder"/> across all <c>Add</c> calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static SetupChain<TBuilder> Setups<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new SetupChain<TBuilder>(builder, ApplyToBuilder);
    }

    internal static void ApplyToBuilder<TBuilder>(TBuilder builder, ISetup setup)
        where TBuilder : IHostApplicationBuilder
    {
        var context = new SetupContext(builder.Configuration, builder.Environment);
        var matched = false;

        if (setup is ISetup<IConfigurationManager> configuration)
        {
            configuration.Setup(builder.Configuration, context);
            matched = true;
        }

        if (setup is ISetup<ILoggingBuilder> logging)
        {
            logging.Setup(builder.Logging, context);
            matched = true;
        }

        if (setup is ISetup<IServiceCollection> services)
        {
            services.Setup(builder.Services, context);
            matched = true;
        }

        if (setup is ISetup<IMetricsBuilder> metrics)
        {
            metrics.Setup(builder.Metrics, context);
            matched = true;
        }

        if (setup is ISetup<TBuilder> typed)
        {
            typed.Setup(builder, context);
            matched = true;
        }

        if (!matched)
        {
            throw new InvalidOperationException(
                $"The setup '{setup.GetType().FullName}' implements no ISetup<T> that fits the builder '{typeof(TBuilder).FullName}': it matches none of the supported targets (IConfigurationManager, ILoggingBuilder, IServiceCollection, IMetricsBuilder) and not the builder type itself. ISetup<WebApplication>-style application setups are applied to the built app with app.Setups().Add, not to the builder.");
        }
    }
}

/// <summary>
/// Starts a <see cref="SetupChain{TTarget}"/> on a built application (any <see cref="IHost"/>). The concrete application type
/// is inferred from the receiver; contravariance lets <c>ISetup&lt;IHost&gt;</c> serve every application. Reflection-free.
/// For builders see <see cref="BuilderSetupChainExtensions"/>.
/// </summary>
/// <example>
/// <code>
/// var app = builder.Build();
/// app.Setups()
///     .Add&lt;EndpointsSetup&gt;();
/// </code>
/// </example>
public static class ApplicationSetupChainExtensions
{
    /// <summary>
    /// Starts a setup chain for a built application.
    /// </summary>
    /// <typeparam name="TApplication">The concrete application type, inferred from the receiver.</typeparam>
    /// <param name="app">The built application.</param>
    /// <returns>A chain that preserves <typeparamref name="TApplication"/> across all <c>Add</c> calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static SetupChain<TApplication> Setups<TApplication>(this TApplication app)
        where TApplication : IHost
    {
        ArgumentNullException.ThrowIfNull(app);
        return new SetupChain<TApplication>(app, ApplyToApplication);
    }

    internal static void ApplyToApplication<TApplication>(TApplication app, ISetup setup)
        where TApplication : IHost
    {
        if (setup is not ISetup<TApplication> typed)
        {
            throw new InvalidOperationException(
                $"The setup '{setup.GetType().FullName}' does not implement ISetup<{typeof(TApplication).FullName}> (or one for a base type), so it cannot be applied to this application. Builder setups are applied before Build with builder.Setups().Add.");
        }

        var services = app.Services;
        typed.Setup(
            app,
            new SetupContext(services.GetRequiredService<IConfiguration>(), services.GetRequiredService<IHostEnvironment>()));
    }
}
