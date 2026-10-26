using Akka.Util;

namespace Terp.Akka;

/// <summary>
/// Pattern-matching helpers for <see cref="Option{T}"/>, which has no <c>Match</c> of its own.
/// </summary>
public static class AkkaOptionsExtensions
{
    /// <summary>Evaluates <paramref name="some"/> with the value if the option has one, otherwise <paramref name="none"/>.</summary>
    /// <typeparam name="TIn">The option's value type.</typeparam>
    /// <typeparam name="TOut">The result type.</typeparam>
    /// <param name="option">The option to match.</param>
    /// <param name="some">Projection used when the option has a value.</param>
    /// <param name="none">Factory used when the option is empty.</param>
    /// <returns>The result of whichever branch was taken.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="some"/> or <paramref name="none"/> is <see langword="null"/>.</exception>
    public static TOut Match<TIn, TOut>(this Option<TIn> option, Func<TIn, TOut> some, Func<TOut> none)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        return option.HasValue ? some(option.Value) : none();
    }

    /// <summary>Invokes <paramref name="some"/> with the value if the option has one, otherwise <paramref name="none"/>.</summary>
    /// <typeparam name="T">The option's value type.</typeparam>
    /// <param name="option">The option to match.</param>
    /// <param name="some">Action invoked when the option has a value.</param>
    /// <param name="none">Action invoked when the option is empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="some"/> or <paramref name="none"/> is <see langword="null"/>.</exception>
    public static void Match<T>(this Option<T> option, Action<T> some, Action none)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        if (option.HasValue)
        {
            some(option.Value);
        }
        else
        {
            none();
        }
    }
}
