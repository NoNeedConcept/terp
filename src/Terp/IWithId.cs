namespace Terp;

/// <summary>
/// A type with a unique <see cref="Guid"/> identity.
/// </summary>
public interface IWithId
{
    /// <summary>Gets the unique identifier.</summary>
    Guid Id { get; }
}
