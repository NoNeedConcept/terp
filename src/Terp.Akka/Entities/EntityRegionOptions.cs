namespace Terp.Akka.Entities;

/// <summary>
/// Configuration for an <see cref="EntityRegionActor"/>: passivation, restart limits and entity caps.
/// </summary>
public sealed record EntityRegionOptions
{
    /// <summary>How long an entity may be idle before passivation. Default 2 minutes.</summary>
    public TimeSpan PassivationTimeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>How often the region checks for idle entities. Default 1 minute.</summary>
    public TimeSpan PassivationInterval { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>Maximum unexpected terminations within <see cref="RestartWindow"/> before an entity is stopped permanently. Default 3.</summary>
    public int MaxRestarts { get; init; } = 3;

    /// <summary>The sliding window in which <see cref="MaxRestarts"/> is counted. Default 1 minute.</summary>
    public TimeSpan RestartWindow { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>Maximum number of live entities. 0 means unlimited. Default 0.</summary>
    public int MaxEntities { get; init; }

    /// <summary>Maximum messages buffered per entity during passivation. Overflow goes to <c>DeadLetters</c>. Default 1000.</summary>
    public int MaxBufferedMessagesPerEntity { get; init; } = 1000;
}
