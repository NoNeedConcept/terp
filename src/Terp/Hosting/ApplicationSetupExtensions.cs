using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Terp.Hosting;

/// <summary>
/// Extension methods that apply <see cref="ISetup"/> instances to a built application (any <see cref="IHost"/>, for example a
/// <c>WebApplication</c>, without this library referencing ASP.NET Core). The application target is <c>ISetup&lt;TApplication&gt;</c>
/// with the static type of the receiver; contravariance lets <c>ISetup&lt;IHost&gt;</c> serve every application. Reflection-free.
/// </summary>
/// <example>
/// <code>
/// public sealed class EndpointsSetup : ISetup&lt;WebApplication&gt;
/// {
///     public void Setup(WebApplication app, SetupContext context) =&gt; app.MapGet("/", () =&gt; "ok");
/// }
///
/// var app = builder.Build();
/// app.Setup(new EndpointsSetup());
/// </code>
/// </example>
public static class ApplicationSetupExtensions
{
    /// <summary>Runs <paramref name="setup"/> against the built application and returns the application for chaining.</summary>
    /// <typeparam name="TApplication">The concrete application type (e.g. <c>WebApplication</c>).</typeparam>
    /// <param name="app">The built application.</param>
    /// <param name="setup">The setup to apply; must implement <c>ISetup&lt;TApplication&gt;</c> (or for a base type).</param>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> or <paramref name="setup"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The setup does not implement <c>ISetup&lt;TApplication&gt;</c>; the message names both types.</exception>
    public static TApplication Setup<TApplication>(this TApplication app, ISetup setup)
        where TApplication : IHost
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(setup);

        if (setup is not ISetup<TApplication> typed)
        {
            throw new InvalidOperationException(
                $"The setup '{setup.GetType().FullName}' does not implement ISetup<{typeof(TApplication).FullName}> (or one for a base type), so it cannot be applied to this application. Builder setups are applied before Build with builder.Setup.");
        }

        var services = app.Services;
        typed.Setup(
            app,
            new SetupContext(services.GetRequiredService<IConfiguration>(), services.GetRequiredService<IHostEnvironment>()));
        return app;
    }

    /// <summary>
    /// Creates a <typeparamref name="TSetup"/> with its parameterless constructor and runs it against the application, with the application target <see cref="IHost"/>.
    /// Use <see cref="Setup{TApplication, TSetup}(TApplication)"/> for a concrete application type.
    /// </summary>
    /// <typeparam name="TSetup">The setup type; must implement <c>ISetup&lt;IHost&gt;</c>.</typeparam>
    /// <param name="app">The built application.</param>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The setup does not implement <c>ISetup&lt;IHost&gt;</c>.</exception>
    public static IHost Setup<TSetup>(this IHost app)
        where TSetup : ISetup, new()
    {
        return app.Setup(new TSetup());
    }

    /// <summary>Creates a <typeparamref name="TSetup"/> with its parameterless constructor and runs it against the application, keeping the application type.</summary>
    /// <typeparam name="TApplication">The concrete application type.</typeparam>
    /// <typeparam name="TSetup">The setup type.</typeparam>
    /// <param name="app">The built application.</param>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The setup does not implement <c>ISetup&lt;TApplication&gt;</c>.</exception>
    public static TApplication Setup<TApplication, TSetup>(this TApplication app)
        where TApplication : IHost
        where TSetup : ISetup, new()
    {
        return app.Setup(new TSetup());
    }
}
