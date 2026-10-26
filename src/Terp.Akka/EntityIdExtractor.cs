using Akka.Cluster.Sharding;

namespace Terp.Akka;

/// <summary>
/// Default <see cref="IMessageExtractor"/> that casts messages to <see cref="IWithEntityId"/>.
/// Messages that do not implement <see cref="IWithEntityId"/> return <see langword="null"/> for the entity identity.
/// The shard count defaults to 100; adjust via <see cref="EntityIdExtractor(int)"/>.
/// </summary>
/// <remarks>
/// This extractor is compatible with both <see cref="EntityRegionActor"/> (local) and
/// <c>WithShardRegion</c> (cluster), so switching between the two requires no extractor changes.
/// </remarks>
public sealed class EntityIdExtractor : HashCodeMessageExtractor
{
    /// <summary>A shared instance with 100 shards (shard count is only relevant for cluster sharding).</summary>
    public static EntityIdExtractor Instance { get; } = new(100);

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityIdExtractor"/> class.
    /// </summary>
    /// <param name="maxNumberOfShards">The maximum number of shards (only relevant when used with cluster sharding).</param>
    public EntityIdExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
    {
    }

    /// <inheritdoc />
    public override string? EntityId(object message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return message is IWithEntityId entity ? entity.EntityId : null;
    }
}
