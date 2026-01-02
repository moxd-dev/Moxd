using Microsoft.Maui.Dispatching;

using Moxd.Threading;

namespace Moxd.Extensions;

/// <summary>
/// Extension methods for <see cref="Task"/> and <see cref="Task{T}"/>.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Safely fires a task without awaiting, ensuring exceptions are handled.
    /// Use this for fire-and-forget scenarios to prevent unobserved task exceptions.
    /// </summary>
    /// <param name="task">The task to fire.</param>
    /// <param name="onException">Optional exception handler. If null, exceptions are silently ignored.</param>
    /// <param name="continueOnCapturedContext">Whether to continue on the captured synchronization context.</param>
    public static async void SafeFireAndForget(this Task task, Action<Exception>? onException = null, bool continueOnCapturedContext = false)
    {
        ArgumentNullException.ThrowIfNull(task);

        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            onException?.Invoke(ex);
        }
    }

    /// <summary>
    /// Safely fires a ValueTask without awaiting, ensuring exceptions are handled.
    /// </summary>
    /// <param name="task">The ValueTask to fire.</param>
    /// <param name="onException">Optional exception handler.</param>
    /// <param name="continueOnCapturedContext">Whether to continue on the captured synchronization context.</param>
    /// <remarks>
    /// This method intentionally uses async void to enable fire-and-forget semantics.
    /// The VSTHRD100 warning is suppressed because this is the intended design pattern.
    /// </remarks>
    public static async void SafeFireAndForget(this ValueTask task, Action<Exception>? onException = null, bool continueOnCapturedContext = false)
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            onException?.Invoke(ex);
        }
    }

    /// <summary>
    /// Safely fires a task with typed exception handling.
    /// Only exceptions of type <typeparamref name="TException"/> will be passed to the handler.
    /// Other exceptions are silently ignored.
    /// </summary>
    /// <typeparam name="TException">The type of exception to handle.</typeparam>
    /// <param name="task">The task to fire.</param>
    /// <param name="onException">Optional exception handler for the specific exception type.</param>
    /// <param name="continueOnCapturedContext">Whether to continue on the captured synchronization context.</param>
    public static async void SafeFireAndForget<TException>(this Task task, Action<TException>? onException = null, bool continueOnCapturedContext = false)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(task);

        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (TException ex)
        {
            onException?.Invoke(ex);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected, don't treat as error
        }
        catch
        {
            // Other exception types are silently ignored when using typed handler
        }
    }

    /// <summary>
    /// Wraps a task with a timeout. Throws <see cref="TimeoutException"/> if the task doesn't complete in time.
    /// </summary>
    /// <typeparam name="T">The result type of the task.</typeparam>
    /// <param name="task">The task to wrap.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The result of the task.</returns>
    /// <exception cref="TimeoutException">Thrown when the task doesn't complete within the timeout.</exception>
    public static async Task<T> WithTimeoutAsync<T>(this Task<T> task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task delayTask = Task.Delay(timeout, timeoutCts.Token);

        Task completedTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);

        if (completedTask == delayTask)
        {
            throw new TimeoutException($"The operation timed out after {timeout.TotalMilliseconds}ms.");
        }

        // Cancel the delay task since we completed
        await timeoutCts.CancelAsync().ConfigureAwait(false);

        return await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Wraps a task with a timeout. Throws <see cref="TimeoutException"/> if the task doesn't complete in time.
    /// </summary>
    /// <param name="task">The task to wrap.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="TimeoutException">Thrown when the task doesn't complete within the timeout.</exception>
    public static async Task WithTimeoutAsync(this Task task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task delayTask = Task.Delay(timeout, timeoutCts.Token);

        Task completedTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);

        if (completedTask == delayTask)
        {
            throw new TimeoutException($"The operation timed out after {timeout.TotalMilliseconds}ms.");
        }

        // Cancel the delay task since we completed
        await timeoutCts.CancelAsync().ConfigureAwait(false);

        await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Wraps a task with a fallback value if it times out.
    /// </summary>
    /// <typeparam name="T">The result type of the task.</typeparam>
    /// <param name="task">The task to wrap.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="fallbackValue">The value to return if the task times out.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The result of the task, or the fallback value if timed out.</returns>
    public static async Task<T> WithTimeoutOrDefaultAsync<T>(this Task<T> task, TimeSpan timeout, T fallbackValue = default!, CancellationToken cancellationToken = default)
    {
        try
        {
            return await task.WithTimeoutAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return fallbackValue;
        }
    }

    /// <summary>
    /// Continues with an action only if the task completes successfully.
    /// </summary>
    /// <typeparam name="T">The result type of the task.</typeparam>
    /// <param name="task">The task to continue from.</param>
    /// <param name="onSuccess">The action to invoke on success.</param>
    /// <param name="continueOnCapturedContext">Whether to continue on the captured synchronization context.</param>
    /// <returns>A task representing the continuation.</returns>
    public static async Task OnSuccessAsync<T>(this Task<T> task, Action<T> onSuccess, bool continueOnCapturedContext = false)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(onSuccess);

        T result = await task.ConfigureAwait(continueOnCapturedContext);
        onSuccess(result);
    }

    /// <summary>
    /// Continues with an action if the task faults.
    /// </summary>
    /// <param name="task">The task to continue from.</param>
    /// <param name="onError">The action to invoke on error.</param>
    /// <param name="continueOnCapturedContext">Whether to continue on the captured synchronization context.</param>
    /// <returns>A task representing the continuation.</returns>
    public static async Task OnErrorAsync(this Task task, Action<Exception> onError, bool continueOnCapturedContext = false)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(onError);

        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            onError(ex);
        }
    }

    /// <summary>
    /// Runs work on a background thread, then executes a UI action on the main thread with the result.
    /// This is the recommended pattern for loading data and updating the UI.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="dispatcher">The dispatcher for UI thread marshaling.</param>
    /// <param name="backgroundWork">The async work to run on the background thread.</param>
    /// <param name="uiWork">The action to run on the UI thread with the result.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <example>
    /// <code>
    /// await TaskExtensions.RunAsync(
    ///     _dispatcher,
    ///     async () => await _database.GetOrdersAsync(),
    ///     orders => Orders.Load(orders));
    /// </code>
    /// </example>
    public static async Task RunAsync<T>(IDispatcher dispatcher, Func<Task<T>> backgroundWork, Action<T> uiWork, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backgroundWork);
        ArgumentNullException.ThrowIfNull(uiWork);

        T result = await Task.Run(backgroundWork, cancellationToken).ConfigureAwait(false);

        DispatcherHelper.Dispatch(dispatcher, () => uiWork(result));
    }

    /// <summary>
    /// Runs synchronous work on a background thread, then executes a UI action on the main thread.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="dispatcher">The dispatcher for UI thread marshaling.</param>
    /// <param name="backgroundWork">The synchronous work to run on the background thread.</param>
    /// <param name="uiWork">The action to run on the UI thread with the result.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <example>
    /// <code>
    /// await TaskExtensions.RunAsync(
    ///     _dispatcher,
    ///     () => ComputeExpensiveCalculation(),
    ///     result => ResultLabel.Text = result.ToString());
    /// </code>
    /// </example>
    public static async Task RunAsync<T>(IDispatcher dispatcher, Func<T> backgroundWork, Action<T> uiWork, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backgroundWork);
        ArgumentNullException.ThrowIfNull(uiWork);

        T result = await Task.Run(backgroundWork, cancellationToken).ConfigureAwait(false);

        DispatcherHelper.Dispatch(dispatcher, () => uiWork(result));
    }

    /// <summary>
    /// Runs async work on a background thread, then executes an action on the UI thread.
    /// </summary>
    /// <param name="dispatcher">The dispatcher for UI thread marshaling.</param>
    /// <param name="backgroundWork">The async work to run on the background thread.</param>
    /// <param name="uiWork">The action to run on the UI thread.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static async Task RunAsync(IDispatcher dispatcher, Func<Task> backgroundWork, Action uiWork, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backgroundWork);
        ArgumentNullException.ThrowIfNull(uiWork);

        await Task.Run(backgroundWork, cancellationToken).ConfigureAwait(false);

        DispatcherHelper.Dispatch(dispatcher, uiWork);
    }

    /// <summary>
    /// Runs synchronous work on a background thread, then executes an action on the UI thread.
    /// </summary>
    /// <param name="dispatcher">The dispatcher for UI thread marshaling.</param>
    /// <param name="backgroundWork">The synchronous work to run on the background thread.</param>
    /// <param name="uiWork">The action to run on the UI thread.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static async Task RunAsync(IDispatcher dispatcher, Action backgroundWork, Action uiWork, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backgroundWork);
        ArgumentNullException.ThrowIfNull(uiWork);

        await Task.Run(backgroundWork, cancellationToken).ConfigureAwait(false);

        DispatcherHelper.Dispatch(dispatcher, uiWork);
    }
}