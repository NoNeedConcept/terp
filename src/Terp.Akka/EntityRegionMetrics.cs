using System.Diagnostics.Metrics;

namespace Terp.Akka;

/// <summary>
/// Metrics instruments for <see cref="EntityRegionActor"/>. All instruments live on a shared
/// <see cref="Meter"/> named <c>Terp.Akka.EntityRegion</c> and carry a <c>region</c> tag.
/// </summary>
internal static class EntityRegionMetrics
{
    internal static readonly Meter Meter = new("Terp.Akka.EntityRegion");

    internal static readonly UpDownCounter<int> ActiveEntities =
        Meter.CreateUpDownCounter<int>("entity_region.active_entities", description: "Current live entity count.");

    internal static readonly Counter<long> Passivations =
        Meter.CreateCounter<long>("entity_region.passivations", description: "Total passivation events.");

    internal static readonly Counter<long> Restarts =
        Meter.CreateCounter<long>("entity_region.restarts", description: "Total unexpected restart events.");

    internal static readonly Counter<long> MessagesBuffered =
        Meter.CreateCounter<long>("entity_region.messages_buffered", description: "Messages buffered during passivation.");
}
