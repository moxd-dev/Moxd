using Moxd.Models;

namespace Moxd.Interfaces;

/// <summary>
/// Provides high-performance UI dispatching with batching, priority queuing, and timing metrics.
/// </summary>
public interface IDispatcherService
{
    /// <summary>
    /// Gets whether the current thread is the main/UI thread.
    /// </summary>
    bool IsMainThread { get; }

    /// <summary>
    /// Gets whether there is currently an active batch scope.
    /// </summary>
    bool IsBatching { get; }

    /// <summary>
    /// Dispatches an action to the UI thread.
    /// </summary>
    /// <param name="action">The action to execute on the UI thread.</param>
    /// <param name="priority">The priority of this dispatch operation.</param>
    void Dispatch(Action action, DispatchPriority priority = DispatchPriority.Normal);

    /// <summary>
    /// Dispatches an action to the UI thread and awaits its completion.
    /// </summary>
    /// <param name="action">The action to execute on the UI thread.</param>
    /// <param name="priority">The priority of this dispatch operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DispatchAsync(Action action, DispatchPriority priority = DispatchPriority.Normal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches a function to the UI thread and returns the result.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="func">The function to execute on the UI thread.</param>
    /// <param name="priority">The priority of this dispatch operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the function.</returns>
    Task<T> DispatchAsync<T>(Func<T> func, DispatchPriority priority = DispatchPriority.Normal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs work on a background thread, then dispatches the result to the UI thread.
    /// Returns timing metrics for both operations.
    /// </summary>
    /// <typeparam name="T">The type of data returned by the background work.</typeparam>
    /// <param name="backgroundWork">The work to execute on a background thread.</param>
    /// <param name="uiWork">The work to execute on the UI thread with the background result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing timing metrics.</returns>
    Task<DispatchResult<T>> RunAsync<T>(Func<T> backgroundWork, Action<T> uiWork, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs async work on a background thread, then dispatches the result to the UI thread.
    /// </summary>
    /// <typeparam name="T">The type of data returned by the background work.</typeparam>
    /// <param name="backgroundWork">The async work to execute.</param>
    /// <param name="uiWork">The work to execute on the UI thread with the result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing timing metrics.</returns>
    Task<DispatchResult<T>> RunAsync<T>(Func<Task<T>> backgroundWork, Action<T> uiWork, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs work on a background thread, then dispatches to the UI thread.
    /// </summary>
    /// <param name="backgroundWork">The work to execute on a background thread.</param>
    /// <param name="uiWork">The work to execute on the UI thread.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing timing metrics.</returns>
    Task<DispatchResult> RunAsync(Action backgroundWork, Action uiWork, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs async work on a background thread, then dispatches to the UI thread.
    /// </summary>
    /// <param name="backgroundWork">The async work to execute.</param>
    /// <param name="uiWork">The work to execute on the UI thread.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing timing metrics.</returns>
    Task<DispatchResult> RunAsync(Func<Task> backgroundWork, Action uiWork, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a batch scope that collects all dispatch operations
    /// and executes them as a single batch when disposed.
    /// </summary>
    /// <returns>A batch scope that should be disposed to execute the batch.</returns>
    IBatchScope Batch();

    /// <summary>
    /// Dispatches an action to the UI thread after a delay.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="delay">The delay before execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DispatchDelayedAsync(Action action, TimeSpan delay, CancellationToken cancellationToken = default);
}