namespace Terp.Hosting;

/// <summary>
/// Marker for a composable unit of application setup. A setup declares what it configures by implementing
/// <see cref="ISetup{TTarget}"/> once per target (for example <c>ISetup&lt;IServiceCollection&gt;</c>); the method is always called
/// <c>Setup</c>, overloaded by target. Each implementation hands the setup only the target it names (least privilege).
/// Apply setups with <see cref="SetupExtensions.Setup{TBuilder}(TBuilder, ISetup)"/> (before build) or
/// <see cref="ApplicationSetupExtensions.Setup{TApplication}(TApplication, ISetup)"/> (after build).
/// </summary>
/// <remarks>
/// Inside one setup that implements several targets the order is fixed: <c>IConfigurationManager</c>, then <c>ILoggingBuilder</c>,
/// then <c>IServiceCollection</c>, then <c>IMetricsBuilder</c>, then the builder type itself (the full-access exception:
/// <c>ISetup&lt;IHostApplicationBuilder&gt;</c> runs with every builder, <c>ISetup&lt;WebApplicationBuilder&gt;</c> receives the concrete type).
/// </remarks>
public interface ISetup;
