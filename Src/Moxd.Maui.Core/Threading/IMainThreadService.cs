namespace Moxd.Threading;

/// <summary>
/// Provides an abstraction for main/UI thread operations.
/// This enables unit testing of code that needs to dispatch to the UI thread.
/// </summary>
public interface IMainThreadService
{
    /// <summary>
    /// Gets a value indicating whether the current thread is the main/UI thread.
    /// </summary>
    bool IsMainThread { get; }

    /// <summary>
    /// Invokes an action on the main thread synchronously.
    /// If already on the main thread, executes immediately.
    /// </summary>
    /// <param name="action">The action to invoke.</param>
    void InvokeOnMainThread(Action action);

    /// <summary>
    /// Invokes an action on the main thread asynchronously.
    /// </summary>
    /// <param name="action">The action to invoke.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes when the action has been executed.</returns>
    Task InvokeOnMainThreadAsync(Action action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a function on the main thread asynchronously and returns the result.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="func">The function to invoke.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task containing the result of the function.</returns>
    Task<T> InvokeOnMainThreadAsync<T>(Func<T> func, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes an async function on the main thread and returns the result.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="asyncFunc">The async function to invoke.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task containing the result of the function.</returns>
    Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> asyncFunc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules an action to run on the main thread after a delay.
    /// </summary>
    /// <param name="action">The action to invoke.</param>
    /// <param name="delay">The delay before invoking the action.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes when the action has been executed.</returns>
    Task InvokeOnMainThreadDelayedAsync(Action action, TimeSpan delay, CancellationToken cancellationToken = default);
}