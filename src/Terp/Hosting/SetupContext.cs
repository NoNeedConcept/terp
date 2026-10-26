using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Terp.Hosting;

/// <summary>
/// Read-only information handed to a setup next to its target: the target is what it may change, the context is only readable.
/// </summary>
public sealed class SetupContext
{
    /// <summary>Initializes a new instance of the <see cref="SetupContext"/> class.</summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The host environment.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> or <paramref name="environment"/> is <see langword="null"/>.</exception>
    public SetupContext(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        Configuration = configuration;
        Environment = environment;
    }

    /// <summary>Gets the application configuration.</summary>
    public IConfiguration Configuration { get; }

    /// <summary>Gets the host environment.</summary>
    public IHostEnvironment Environment { get; }
}
