namespace Terp.Hosting.Gates;

/// <summary>
/// A readiness check that must succeed before the host finishes starting, for example
/// "the database accepts connections". Register with <c>services.AddStartupGate&lt;TGate&gt;()</c> or
/// <c>services.AddStartupGate(name, check)</c>; a ready-made instance is registered with
/// <c>services.AddSingleton&lt;IStartupGate&gt;(instance)</c> (together with at least one of the calls above,
/// which installs the evaluating hosted service).
/// Gates are singletons; a scoped service cannot be a gate. See <see cref="StartupGateExtensions"/> for the evaluation rules.
/// </summary>
public interface IStartupGate
{
    /// <summary>
    /// Gets the name used in logs and exception messages to identify this gate.
    /// Defaults to the name of the implementing type.
    /// </summary>
    string Name => GetType().Name;

    /// <summary>
    /// Checks whether the gate is clear. Implementations should honour <paramref name="cancellationToken"/>
    /// and must not block synchronously; a gate that ignores the token is still abandoned when startup is cancelled.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the host start is aborted or when the per-check timeout of 10 seconds elapses.</param>
    /// <returns><see langword="true"/> when the gate is clear; <see langword="false"/> to be checked again later.</returns>
    Task<bool> CheckAsync(CancellationToken cancellationToken);
}
