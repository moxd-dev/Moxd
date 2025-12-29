using System.Diagnostics;

using Moxd.Interfaces;
using Moxd.Models;
using Moxd.Threading;

namespace Moxd.Services;

/// <summary>
/// High-performance dispatcher service with batching, priority queuing, and timing metrics.
/// </summary>
/// <remarks>
/// Creates a new dispatcher service.
/// </remarks>
/// <param name="mainThread">The main thread service.</param>
public sealed class DispatcherService(IMainThreadService mainThread) : IDispatcherService
{
    #region Fields
    private readonly IMainThreadService _mainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));
    private readonly AsyncLocal<BatchScope?> _currentBatch = new();
    #endregion

    #region Properties
    /// <inheritdoc />
    public bool IsMainThread => _mainThread.IsMainThread;

    /// <inheritdoc />
    public bool IsBatching => _currentBatch.Value?.IsActive == true;
    #endregion

    #region Public Methods
    /// <inheritdoc />
    public void Dispatch(Action action, DispatchPriority priority = DispatchPriority.Normal)
    {
        ArgumentNullException.ThrowIfNull(action);

        // If batching, queue the action
        if (IsBatching)
        {
            _currentBatch.Value?.Enqueue(action);
            return;
        }
        // Otherwise dispatch immediately
        _mainThread.InvokeOnMainThread(action);
    }

    /// <inheritdoc />
    public async Task DispatchAsync(Action action, DispatchPriority priority = DispatchPriority.Normal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();

        // If batching, queue the action
        if (IsBatching)
        {
            _currentBatch.Value?.Enqueue(action);
            return;
        }

        await _mainThread.InvokeOnMainThreadAsync(action, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<T> DispatchAsync<T>(Func<T> func, DispatchPriority priority = DispatchPriority.Normal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(func);
        cancellationToken.ThrowIfCancellationRequested();

        return await _mainThread.InvokeOnMainThreadAsync(func, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DispatchResult<T>> RunAsync<T>(Func<T> backgroundWork, Action<T> uiWork, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backgroundWork);
        ArgumentNullException.ThrowIfNull(uiWork);

        Stopwatch backgroundStopwatch = Stopwatch.StartNew();
        T result;

        try
        {
            // Run background work on thread pool
            result = await Task.Run(backgroundWork, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            backgroundStopwatch.Stop();
            return DispatchResult<T>.Failure(ex, backgroundStopwatch.Elapsed);
        }

        backgroundStopwatch.Stop();
        TimeSpan backgroundTime = backgroundStopwatch.Elapsed;

        Stopwatch dispatchStopwatch = Stopwatch.StartNew();
        try
        {
            // Dispatch to UI thread
            await _mainThread.InvokeOnMainThreadAsync(() => uiWork(result), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            dispatchStopwatch.Stop();
            return DispatchResult<T>.Failure(ex, backgroundTime, dispatchStopwatch.Elapsed);
        }
        dispatchStopwatch.Stop();

        return DispatchResult<T>.Success(result, backgroundTime, dispatchStopwatch.Elapsed);
    }

    /// <inheritdoc />
    public async Task<DispatchResult<T>> RunAsync<T>(Func<Task<T>> backgroundWork, Action<T> uiWork, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backgroundWork);
        ArgumentNullException.ThrowIfNull(uiWork);

        Stopwatch backgroundStopwatch = Stopwatch.StartNew();
        T result;

        try
        {
            result = await backgroundWork().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            backgroundStopwatch.Stop();
            return DispatchResult<T>.Failure(ex, backgroundStopwatch.Elapsed);
        }
        backgroundStopwatch.Stop();
        TimeSpan backgroundTime = backgroundStopwatch.Elapsed;

        Stopwatch dispatchStopwatch = Stopwatch.StartNew();
        try
        {
            await _mainThread.InvokeOnMainThreadAsync(() => uiWork(result), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            dispatchStopwatch.Stop();
            return DispatchResult<T>.Failure(ex, backgroundTime, dispatchStopwatch.Elapsed);
        }
        dispatchStopwatch.Stop();

        return DispatchResult<T>.Success(result, backgroundTime, dispatchStopwatch.Elapsed);
    }

    /// <inheritdoc />
    public async Task<DispatchResult> RunAsync(Action backgroundWork, Action uiWork, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backgroundWork);
        ArgumentNullException.ThrowIfNull(uiWork);

        Stopwatch backgroundStopwatch = Stopwatch.StartNew();
        try
        {
            await Task.Run(backgroundWork, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            backgroundStopwatch.Stop();
            return DispatchResult.Failure(ex, backgroundStopwatch.Elapsed);
        }

        backgroundStopwatch.Stop();
        TimeSpan backgroundTime = backgroundStopwatch.Elapsed;

        Stopwatch dispatchStopwatch = Stopwatch.StartNew();
        try
        {
            await _mainThread.InvokeOnMainThreadAsync(uiWork, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            dispatchStopwatch.Stop();
            return DispatchResult.Failure(ex, backgroundTime, dispatchStopwatch.Elapsed);
        }
        dispatchStopwatch.Stop();

        return DispatchResult.Success(backgroundTime, dispatchStopwatch.Elapsed);
    }

    /// <inheritdoc />
    public async Task<DispatchResult> RunAsync(Func<Task> backgroundWork, Action uiWork, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backgroundWork);
        ArgumentNullException.ThrowIfNull(uiWork);

        Stopwatch backgroundStopwatch = Stopwatch.StartNew();
        try
        {
            await backgroundWork().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            backgroundStopwatch.Stop();
            return DispatchResult.Failure(ex, backgroundStopwatch.Elapsed);
        }

        backgroundStopwatch.Stop();
        TimeSpan backgroundTime = backgroundStopwatch.Elapsed;

        Stopwatch dispatchStopwatch = Stopwatch.StartNew();
        try
        {
            await _mainThread.InvokeOnMainThreadAsync(uiWork, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            dispatchStopwatch.Stop();
            return DispatchResult.Failure(ex, backgroundTime, dispatchStopwatch.Elapsed);
        }
        dispatchStopwatch.Stop();

        return DispatchResult.Success(backgroundTime, dispatchStopwatch.Elapsed);
    }

    /// <inheritdoc />
    public IBatchScope Batch()
    {
        BatchScope scope = new BatchScope(
             executeActions: ExecuteBatchActions,
             onDispose: ClearCurrentBatch);
        _currentBatch.Value = scope;
        return scope;
    }

    /// <inheritdoc />
    public async Task DispatchDelayedAsync(Action action, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);

        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        await _mainThread.InvokeOnMainThreadAsync(action, cancellationToken).ConfigureAwait(false);
    }
    #endregion

    #region Private Methods
    private void ExecuteBatchActions(List<Action> actions)
    {
        if (actions.Count == 0)
        {
            return;
        }

        _mainThread.InvokeOnMainThread(() =>
        {
            foreach (var action in actions)
            {
                action();
            }
        });
    }

    private void ClearCurrentBatch()
    {
        _currentBatch.Value = null;
    }
    #endregion
}