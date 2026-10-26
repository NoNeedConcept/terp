namespace Terp.Akka;

/// <summary>
/// Extracts an entity identity and the inner message from an incoming message.
/// The method signatures are compatible with Akka.Cluster.Sharding's <c>IMessageExtractor</c>
/// so that users can migrate from a local <see cref="EntityRegionActor"/> to cluster sharding
/// without rewriting their extractors.
/// </summary>
public interface IEntityMessageExtractor
{
    /// <summary>
    /// Extracts the entity identity from <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The incoming message.</param>
    /// <returns>The entity identity, or <see langword="null"/> when the message cannot be routed.</returns>
    string? EntityId(object message);

    /// <summary>
    /// Unwraps the message that is forwarded to the entity actor.
    /// Return <paramref name="message"/> unchanged when no unwrapping is needed.
    /// </summary>
    /// <param name="message">The incoming message.</param>
    /// <returns>The message to forward to the entity actor.</returns>
    object EntityMessage(object message);
}
