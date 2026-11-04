using Microsoft.Extensions.Options;

namespace Terp.Options;

/// <summary>
/// Collects validation failures for an options validator and maps them to a <see cref="ValidateOptionsResult"/>.
/// </summary>
/// <remarks>
/// Create one instance per <c>Validate</c> call. The type is not thread-safe.
/// </remarks>
public sealed class ValidationFailures
{
    private readonly List<string> _failures = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFailures"/> class.
    /// </summary>
    /// <param name="section">
    /// The configuration section name that prefixes every failure as <c>"{section}:{message}"</c>.
    /// Failures are not prefixed when it is <see langword="null"/> or empty.
    /// </param>
    public ValidationFailures(string? section = null)
    {
        Section = section;
    }

    /// <summary>
    /// Gets the section name that prefixes every failure, or <see langword="null"/>.
    /// </summary>
    public string? Section { get; }

    /// <summary>
    /// Gets the number of recorded failures.
    /// </summary>
    public int Count => _failures.Count;

    /// <summary>
    /// Records a failure.
    /// </summary>
    /// <param name="message">The failure message, usually starting with the property name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> is empty or whitespace.</exception>
    public void Add(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _failures.Add(string.IsNullOrEmpty(Section) ? message : $"{Section}:{message}");
    }

    /// <summary>
    /// Records a failure when <paramref name="condition"/> is <see langword="false"/>.
    /// </summary>
    /// <param name="condition">The condition that must hold.</param>
    /// <param name="message">The failure message, usually starting with the property name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> is empty or whitespace.</exception>
    public void Require(bool condition, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (!condition)
        {
            Add(message);
        }
    }

    /// <summary>
    /// Maps the recorded failures to a validation result.
    /// </summary>
    /// <returns>
    /// <see cref="ValidateOptionsResult.Success"/> when nothing was recorded; otherwise a failed result
    /// that carries all failures in insertion order.
    /// </returns>
    public ValidateOptionsResult ToResult()
    {
        return _failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(_failures);
    }
}
