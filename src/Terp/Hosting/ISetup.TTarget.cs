namespace Terp.Hosting;

/// <summary>
/// Setup of one target: the thing the setup may change, for example <c>IServiceCollection</c>, <c>ILoggingBuilder</c>,
/// <c>IConfigurationManager</c>, <c>IMetricsBuilder</c>, a builder type (before build) or an application type such as
/// <c>WebApplication</c> (after build, applied with <c>app.Setup(...)</c>). A class can implement this interface for several targets.
/// </summary>
/// <typeparam name="TTarget">The target. Contravariant, so <c>ISetup&lt;IHostApplicationBuilder&gt;</c> also serves every builder type.</typeparam>
public interface ISetup<in TTarget> : ISetup
{
    /// <summary>Configures the target.</summary>
    /// <param name="target">What the setup may change.</param>
    /// <param name="context">Read-only information about the host: configuration and environment.</param>
    void Setup(TTarget target, SetupContext context);
}
