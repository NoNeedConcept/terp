using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Terp.Hosting;

/// <summary>
/// Extension methods that apply <see cref="ISetup"/> instances to a builder before build. Every call runs immediately, in call order.
/// For built applications see <see cref="ApplicationSetupExtensions"/>.
/// </summary>
/// <remarks>
/// Which <c>ISetup&lt;T&gt;</c> for what: <c>IConfigurationManager</c> adds configuration sources; <c>ILoggingBuilder</c> configures logging;
/// <c>IServiceCollection</c> registers services; <c>IMetricsBuilder</c> configures metrics; the builder type (<c>IHostApplicationBuilder</c>
/// or a concrete one such as <c>WebApplicationBuilder</c>) is the full-access exception. Inside one setup that implements several of them
/// they run in exactly that order. The builder target is matched against the static type of the receiver: an <c>ISetup&lt;WebApplicationBuilder&gt;</c>
/// is applied only when <c>Setup</c> is called on an expression of type <c>WebApplicationBuilder</c> (or a subtype).
/// The dispatch is pure pattern matching: reflection-free and trim/AOT-safe.
/// </remarks>
/// <example>
/// <code>
/// var builder = WebApplication.CreateBuilder(args);
/// builder.Setup(new ConfigSetup())   // keeps the builder type, can be chained
///        .Setup(new AkkaSetup());
/// builder.Setup&lt;MetricsSetup&gt;();     // returns IHostApplicationBuilder; use as a statement
/// var app = builder.Build();
/// app.Setup(new EndpointsSetup());   // ISetup&lt;WebApplication&gt;
/// </code>
/// </example>
public static class SetupExtensions
{
    /// <summary>
    /// Runs every target that <paramref name="setup"/> implements against the builder, in the fixed order configuration, logging,
    /// services, metrics, builder, and returns the builder for chaining.
    /// </summary>
    /// <typeparam name="TBuilder">The concrete builder type; preserved so the chain keeps the type (e.g. <c>WebApplicationBuilder</c>).</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="setup">The setup to apply.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="setup"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The setup implements <c>ISetup&lt;T&gt;</c> for none of the supported targets nor for <typeparamref name="TBuilder"/> or a base.</exception>
    public static TBuilder Setup<TBuilder>(this TBuilder builder, ISetup setup)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setup);

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
            throw NoTarget(setup.GetType(), typeof(TBuilder));
        }

        return builder;
    }

    /// <summary>
    /// Creates a <typeparamref name="TSetup"/> with its parameterless constructor and runs it.
    /// C# cannot infer the builder type when only <typeparamref name="TSetup"/> is given, so this returns
    /// <see cref="IHostApplicationBuilder"/>, the builder target is <see cref="IHostApplicationBuilder"/>, and the concrete builder type is lost for chaining.
    /// Use <see cref="Setup{TBuilder, TSetup}(TBuilder)"/> to keep the type.
    /// </summary>
    /// <typeparam name="TSetup">The setup type.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The setup matches no target; see <see cref="Setup{TBuilder}(TBuilder, ISetup)"/>.</exception>
    public static IHostApplicationBuilder Setup<TSetup>(this IHostApplicationBuilder builder)
        where TSetup : ISetup, new()
    {
        return builder.Setup(new TSetup());
    }

    /// <summary>Creates a <typeparamref name="TSetup"/> with its parameterless constructor and runs it, keeping the builder type.</summary>
    /// <typeparam name="TBuilder">The concrete builder type.</typeparam>
    /// <typeparam name="TSetup">The setup type.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The setup matches no target; see <see cref="Setup{TBuilder}(TBuilder, ISetup)"/>.</exception>
    public static TBuilder Setup<TBuilder, TSetup>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
        where TSetup : ISetup, new()
    {
        return builder.Setup(new TSetup());
    }

    private static InvalidOperationException NoTarget(Type setupType, Type builderType)
    {
        return new InvalidOperationException(
            $"The setup '{setupType.FullName}' implements no ISetup<T> that fits the builder '{builderType.FullName}': it matches none of the supported targets (IConfigurationManager, ILoggingBuilder, IServiceCollection, IMetricsBuilder) and not the builder type itself (an ISetup<WebApplicationBuilder> needs a call on a WebApplicationBuilder-typed receiver). ISetup<WebApplication>-style application setups are applied to the built app with app.Setup, not to the builder.");
    }
}
