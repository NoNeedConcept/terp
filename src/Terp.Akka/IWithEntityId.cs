namespace Terp.Akka;

/// <summary>
/// Implemented by messages or types that carry an entity identity, e.g. for a sharding message extractor or routing.
/// </summary>
public interface IWithEntityId
{
    /// <summary>Gets the identifier of the entity this instance belongs to.</summary>
    string EntityId { get; }
}
