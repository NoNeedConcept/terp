using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Hosting;

namespace Terp.Akka;

/// <summary>
/// Extension methods for registering an <see cref="EntityRegionActor"/> with Akka.Hosting.
/// </summary>
public static class EntityRegionExtensions
{
    /// <summary>
    /// Registers an <see cref="EntityRegionActor"/> in the actor registry under <typeparamref name="TKey"/>.
    /// </summary>
    /// <typeparam name="TKey">The registry key type, used with <c>GetActor&lt;TKey&gt;()</c> to resolve the region.</typeparam>
    /// <param name="builder">The Akka configuration builder.</param>
    /// <param name="entityPropsFactory">Creates the <see cref="Props"/> for an entity actor given the dependency resolver.</param>
    /// <param name="configure">Optional; configures the <see cref="EntityRegionOptions"/>.</param>
    /// <param name="extractor">Optional; the message extractor. Defaults to <see cref="EntityIdExtractor.Instance"/>.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="entityPropsFactory"/> is <see langword="null"/>.</exception>
    public static AkkaConfigurationBuilder WithEntityRegion<TKey>(
        this AkkaConfigurationBuilder builder,
        Func<IDependencyResolver, Props> entityPropsFactory,
        Action<EntityRegionOptions>? configure = null,
        IEntityMessageExtractor? extractor = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(entityPropsFactory);

        var options = new EntityRegionOptions();
        configure?.Invoke(options);
        extractor ??= EntityIdExtractor.Instance;
        var regionTag = typeof(TKey).Name;

        builder.WithActors((system, registry, resolver) =>
        {
            var entityFactory = (IActorContext context, string entityId) =>
            {
                var props = entityPropsFactory(resolver);
                return context.ActorOf(props, entityId);
            };

            var regionProps = Props.Create(() =>
                new EntityRegionActor(entityFactory, extractor, options, regionTag));

            var regionRef = system.ActorOf(regionProps, $"entity-region-{regionTag.ToLowerInvariant()}");
            registry.Register<TKey>(regionRef);
        });

        return builder;
    }
}
