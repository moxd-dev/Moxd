namespace Moxd.Models;

/// <summary>
/// Represents the result of a dispatch operation with timing metrics.
/// </summary>
public sealed class DispatchResult
{
    /// <summary>
    /// Gets the time spent executing background work.
    /// </summary>
    public TimeSpan BackgroundTime { get; init; }

    /// <summary>
    /// Gets the time spent dispatching to the UI thread.
    /// </summary>
    public TimeSpan DispatchTime { get; init; }

    /// <summary>
    /// Gets the total time for the entire operation.
    /// </summary>
    public TimeSpan TotalTime => BackgroundTime + DispatchTime;

    /// <summary>
    /// Gets whether the operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Gets the exception if the operation failed.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    internal static DispatchResult Success(TimeSpan backgroundTime, TimeSpan dispatchTime) => new()
    {
        BackgroundTime = backgroundTime,
        DispatchTime = dispatchTime,
        IsSuccess = true
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    internal static DispatchResult Failure(Exception exception, TimeSpan backgroundTime = default, TimeSpan dispatchTime = default) => new()
    {
        BackgroundTime = backgroundTime,
        DispatchTime = dispatchTime,
        IsSuccess = false,
        Exception = exception
    };

    /// <inheritdoc />
    public override string ToString() =>
        IsSuccess
            ? $"Success - Background: {BackgroundTime.TotalMilliseconds:F2}ms, Dispatch: {DispatchTime.TotalMilliseconds:F2}ms, Total: {TotalTime.TotalMilliseconds:F2}ms"
            : $"Failed - {Exception?.Message}";
}

/// <summary>
/// Represents the result of a dispatch operation with a return value and timing metrics.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public sealed class DispatchResult<T>
{
    /// <summary>
    /// Gets the result value from the background operation.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Gets the time spent executing background work.
    /// </summary>
    public TimeSpan BackgroundTime { get; init; }

    /// <summary>
    /// Gets the time spent dispatching to the UI thread.
    /// </summary>
    public TimeSpan DispatchTime { get; init; }

    /// <summary>
    /// Gets the total time for the entire operation.
    /// </summary>
    public TimeSpan TotalTime => BackgroundTime + DispatchTime;

    /// <summary>
    /// Gets whether the operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Gets the exception if the operation failed.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    internal static DispatchResult<T> Success(T value, TimeSpan backgroundTime, TimeSpan dispatchTime) => new()
    {
        Value = value,
        BackgroundTime = backgroundTime,
        DispatchTime = dispatchTime,
        IsSuccess = true
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    internal static DispatchResult<T> Failure(Exception exception, TimeSpan backgroundTime = default, TimeSpan dispatchTime = default) => new()
    {
        Value = default,
        BackgroundTime = backgroundTime,
        DispatchTime = dispatchTime,
        IsSuccess = false,
        Exception = exception
    };

    /// <inheritdoc />
    public override string ToString() =>
        IsSuccess
            ? $"Success - Background: {BackgroundTime.TotalMilliseconds:F2}ms, Dispatch: {DispatchTime.TotalMilliseconds:F2}ms, Total: {TotalTime.TotalMilliseconds:F2}ms"
            : $"Failed - {Exception?.Message}";
}