using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Moxd.Guards;

/// <summary>
/// Provides guard clauses for argument validation.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> if the value is null.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="parameterName">The name of the parameter (auto-populated).</param>
    /// <returns>The non-null value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public static T IsNotNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        return value is null ? throw new ArgumentNullException(parameterName) : value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if the string is null or empty.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="parameterName">The name of the parameter (auto-populated).</param>
    /// <returns>The non-null, non-empty string.</returns>
    /// <exception cref="ArgumentException">Thrown when value is null or empty.</exception>
    public static string IsNotNullOrEmpty([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        return string.IsNullOrEmpty(value) ? throw new ArgumentException("Value cannot be null or empty.", parameterName) : value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if the string is null, empty, or whitespace.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="parameterName">The name of the parameter (auto-populated).</param>
    /// <returns>The non-null, non-whitespace string.</returns>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace.</exception>
    public static string IsNotNullOrWhiteSpace([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null, empty, or whitespace.", parameterName)
            : value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is not within the specified range.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="parameterName">The name of the parameter (auto-populated).</param>
    /// <returns>The value if within range.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside the range.</exception>
    public static T IsInRange<T>(T value, T min, T max, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : IComparable<T>
    {
        return value.CompareTo(min) < 0 || value.CompareTo(max) > 0
            ? throw new ArgumentOutOfRangeException(parameterName, value, $"Value must be between {min} and {max}.")
            : value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is less than or equal to zero.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="parameterName">The name of the parameter (auto-populated).</param>
    /// <returns>The positive value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is not positive.</exception>
    public static int IsPositive(int value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        return value <= 0 ? throw new ArgumentOutOfRangeException(parameterName, value, "Value must be positive.") : value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is less than zero.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="parameterName">The name of the parameter (auto-populated).</param>
    /// <returns>The non-negative value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative.</exception>
    public static int IsNotNegative(int value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        return value < 0 ? throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.") : value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if the collection is null or empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="parameterName">The name of the parameter (auto-populated).</param>
    /// <returns>The non-null, non-empty collection.</returns>
    /// <exception cref="ArgumentException">Thrown when collection is null or empty.</exception>
    public static IEnumerable<T> IsNotNullOrEmpty<T>([NotNull] IEnumerable<T>? collection, [CallerArgumentExpression(nameof(collection))] string? parameterName = null)
    {
        return collection is null || !collection.Any()
            ? throw new ArgumentException("Collection cannot be null or empty.", parameterName)
            : collection;
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if the condition is false.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="parameterName">The name of the parameter (auto-populated).</param>
    /// <exception cref="ArgumentException">Thrown when condition is false.</exception>
    public static void IsTrue(bool condition, string message, [CallerArgumentExpression(nameof(condition))] string? parameterName = null)
    {
        if (!condition)
        {
            throw new ArgumentException(message, parameterName);
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if the condition is true.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="parameterName">The name of the parameter (auto-populated).</param>
    /// <exception cref="ArgumentException">Thrown when condition is true.</exception>
    public static void IsFalse(bool condition, string message, [CallerArgumentExpression(nameof(condition))] string? parameterName = null)
    {
        if (condition)
        {
            throw new ArgumentException(message, parameterName);
        }
    }
}