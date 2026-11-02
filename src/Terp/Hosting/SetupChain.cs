namespace Terp.Hosting;

/// <summary>
/// A fluent chain that applies <see cref="ISetup"/> instances to a target while preserving the concrete target type.
/// Each <see cref="Add{TSetup}"/> or <see cref="Add(ISetup)"/> call applies the setup immediately; there is no deferred execution.
/// Obtain a chain with <see cref="BuilderSetupChainExtensions.Setups{TBuilder}(TBuilder)"/> (builder) or
/// <see cref="ApplicationSetupChainExtensions.Setups{TApplication}(TApplication)"/> (application).
/// </summary>
/// <typeparam name="TTarget">The concrete target type, inferred from the receiver and preserved across the chain.</typeparam>
public readonly struct SetupChain<TTarget>
{
    private readonly TTarget _target;
    private readonly Action<TTarget, ISetup> _dispatch;

    internal SetupChain(TTarget target, Action<TTarget, ISetup> dispatch)
    {
        _target = target;
        _dispatch = dispatch;
    }

    /// <summary>
    /// Creates a <typeparamref name="TSetup"/> with its parameterless constructor and applies it to the target immediately.
    /// </summary>
    /// <typeparam name="TSetup">The setup type.</typeparam>
    /// <returns>This chain, for further chaining.</returns>
    /// <exception cref="InvalidOperationException">The setup matches no supported target.</exception>
    public SetupChain<TTarget> Add<TSetup>()
        where TSetup : ISetup, new()
    {
        return Add(new TSetup());
    }

    /// <summary>
    /// Applies the given <paramref name="setup"/> to the target immediately.
    /// </summary>
    /// <param name="setup">The setup to apply.</param>
    /// <returns>This chain, for further chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setup"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The setup matches no supported target.</exception>
    public SetupChain<TTarget> Add(ISetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        _dispatch(_target, setup);
        return this;
    }
}
