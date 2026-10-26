using Akka.Actor;

namespace Terp.Akka;

/// <summary>
/// Sent by an entity actor to its parent (<c>Context.Parent.Tell(Passivate.Instance)</c>) to request passivation.
/// The region buffers subsequent messages for that entity, sends the <see cref="StopMessage"/> back to the entity,
/// and waits for <see cref="Terminated"/>. This mirrors the Akka Cluster Sharding passivation protocol.
/// </summary>
public sealed class Passivate
{
    /// <summary>A shared instance that uses <see cref="PoisonPill.Instance"/> as the stop message.</summary>
    public static Passivate Instance { get; } = new(PoisonPill.Instance);

    /// <summary>Gets the message sent back to the entity to make it stop.</summary>
    public object StopMessage { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Passivate"/> class with a custom stop message.
    /// </summary>
    /// <param name="stopMessage">The message sent back to the entity to trigger its stop.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stopMessage"/> is <see langword="null"/>.</exception>
    public Passivate(object stopMessage)
    {
        ArgumentNullException.ThrowIfNull(stopMessage);
        StopMessage = stopMessage;
    }
}
