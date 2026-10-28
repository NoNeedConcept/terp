using System.Diagnostics.Metrics;
using System.Numerics;

namespace Terp.Diagnostics;

/// <summary>Semantic increment/decrement helpers for <see cref="Counter{T}"/> and <see cref="UpDownCounter{T}"/>.</summary>
public static class CounterExtensions
{
    /// <summary>Increments the counter by one.</summary>
    /// <param name="counter">The counter to increment.</param>
    /// <typeparam name="T">The numeric type of the counter.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="counter"/> is <see langword="null"/>.</exception>
    public static void Up<T>(this Counter<T> counter) where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(counter);
        counter.Add(T.One);
    }

    /// <summary>Increments the counter by one with a tag.</summary>
    /// <param name="counter">The counter to increment.</param>
    /// <param name="tag">A tag added to the measurement.</param>
    /// <typeparam name="T">The numeric type of the counter.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="counter"/> is <see langword="null"/>.</exception>
    public static void Up<T>(this Counter<T> counter, KeyValuePair<string, object?> tag) where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(counter);
        counter.Add(T.One, tag);
    }

    /// <summary>Increments the counter by one with two tags.</summary>
    /// <param name="counter">The counter to increment.</param>
    /// <param name="tag1">The first tag.</param>
    /// <param name="tag2">The second tag.</param>
    /// <typeparam name="T">The numeric type of the counter.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="counter"/> is <see langword="null"/>.</exception>
    public static void Up<T>(this Counter<T> counter, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2) where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(counter);
        counter.Add(T.One, tag1, tag2);
    }

    /// <summary>Increments the counter by one with any number of tags.</summary>
    /// <param name="counter">The counter to increment.</param>
    /// <param name="tags">Tags added to the measurement.</param>
    /// <typeparam name="T">The numeric type of the counter.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="counter"/> is <see langword="null"/>.</exception>
    public static void Up<T>(this Counter<T> counter, ReadOnlySpan<KeyValuePair<string, object?>> tags) where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(counter);
        counter.Add(T.One, tags);
    }

    /// <summary>Increments the up-down counter by one.</summary>
    /// <param name="counter">The counter to increment.</param>
    /// <typeparam name="T">The numeric type of the counter.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="counter"/> is <see langword="null"/>.</exception>
    public static void Up<T>(this UpDownCounter<T> counter) where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(counter);
        counter.Add(T.One);
    }

    /// <summary>Increments the up-down counter by one with a tag.</summary>
    /// <param name="counter">The counter to increment.</param>
    /// <param name="tag">A tag added to the measurement.</param>
    /// <typeparam name="T">The numeric type of the counter.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="counter"/> is <see langword="null"/>.</exception>
    public static void Up<T>(this UpDownCounter<T> counter, KeyValuePair<string, object?> tag) where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(counter);
        counter.Add(T.One, tag);
    }

    /// <summary>Increments the up-down counter by one with two tags.</summary>
    /// <param name="counter">The counter to increment.</param>
    /// <param name="tag1">The first tag.</param>
    /// <param name="tag2">The second tag.</param>
    /// <typeparam name="T">The numeric type of the counter.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="counter"/> is <see langword="null"/>.</exception>
    public static void Up<T>(this UpDownCounter<T> counter, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2) where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(counter);
        counter.Add(T.One, tag1, tag2);
    }

    /// <summary>Increments the up-down counter by one with any number of tags.</summary>
    /// <param name="counter">The counter to increment.</param>
    /// <param name="tags">Tags added to the measurement.</param>
    /// <typeparam name="T">The numeric type of the counter.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="counter"/> is <see langword="null"/>.</exception>
    public static void Up<T>(this UpDownCounter<T> counter, ReadOnlySpan<KeyValuePair<string, object?>> tags) where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(counter);
        counter.Add(T.One, tags);
    }

    /// <summary>Decrements the up-down counter by one.</summary>
    /// <param name="counter">The counter to decrement.</param>
    /// <typeparam name="T">The numeric type of the counter.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="counter"/> is <see langword="null"/>.</exception>
    public static void Down<T>(this UpDownCounter<T> counter) where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(counter);
        counter.Add(-T.One);
    }

    /// <summary>Decrements the up-down counter by one with a tag.</summary>
    /// <param name="counter">The counter to decrement.</param>
    /// <param name="tag">A tag added to the measurement.</param>
    /// <typeparam name="T">The numeric type of the counter.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="counter"/> is <see langword="null"/>.</exception>
    public static void Down<T>(this UpDownCounter<T> counter, KeyValuePair<string, object?> tag) where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(counter);
        counter.Add(-T.One, tag);
    }

    /// <summary>Decrements the up-down counter by one with two tags.</summary>
    /// <param name="counter">The counter to decrement.</param>
    /// <param name="tag1">The first tag.</param>
    /// <param name="tag2">The second tag.</param>
    /// <typeparam name="T">The numeric type of the counter.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="counter"/> is <see langword="null"/>.</exception>
    public static void Down<T>(this UpDownCounter<T> counter, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2) where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(counter);
        counter.Add(-T.One, tag1, tag2);
    }

    /// <summary>Decrements the up-down counter by one with any number of tags.</summary>
    /// <param name="counter">The counter to decrement.</param>
    /// <param name="tags">Tags added to the measurement.</param>
    /// <typeparam name="T">The numeric type of the counter.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="counter"/> is <see langword="null"/>.</exception>
    public static void Down<T>(this UpDownCounter<T> counter, ReadOnlySpan<KeyValuePair<string, object?>> tags) where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(counter);
        counter.Add(-T.One, tags);
    }
}
