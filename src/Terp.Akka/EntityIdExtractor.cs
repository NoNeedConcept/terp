namespace Terp.Akka;

/// <summary>
/// Default <see cref="IEntityMessageExtractor"/> that casts messages to <see cref="IWithEntityId"/>.
/// Messages that do not implement <see cref="IWithEntityId"/> return <see langword="null"/> for the entity identity.
/// </summary>
public sealed class EntityIdExtractor : IEntityMessageExtractor
{
    /// <summary>A shared instance of the default extractor.</summary>
    public static EntityIdExtractor Instance { get; } = new();

    /// <inheritdoc />
    public string? EntityId(object message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return message is IWithEntityId entity ? entity.EntityId : null;
    }

    /// <inheritdoc />
    public object EntityMessage(object message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return message;
    }
}
