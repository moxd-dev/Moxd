using Moxd.Threading;

namespace Moxd.Maui.Dispatch.Tests;

/// <summary>
/// Mock implementation of IMainThreadService for unit testing.
/// Tracks all dispatches and can simulate various scenarios.
/// </summary>
public class MockMainThreadService : IMainThreadService
{
    private readonly List<Action> _dispatchedActions = [];
    private readonly Lock _lock = new();

    /// <summary>
    /// Gets or sets whether the current thread should be considered the main thread.
    /// </summary>
    public bool SimulateIsMainThread { get; set; } = false;

    /// <summary>
    /// Gets or sets the delay to add to dispatches (simulating UI thread latency).
    /// </summary>
    public TimeSpan DispatchDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets the number of dispatches that have occurred.
    /// </summary>
    public int DispatchCount
    {
        get
        {
            lock (_lock)
            {
                return _dispatchedActions.Count;
            }
        }
    }

    /// <inheritdoc />
    public bool IsMainThread => SimulateIsMainThread;

    /// <inheritdoc />
    public void InvokeOnMainThread(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (_lock)
        {
            _dispatchedActions.Add(action);
        }

        if (DispatchDelay > TimeSpan.Zero)
        {
            Thread.Sleep(DispatchDelay);
        }

        action();
    }

    /// <inheritdoc />
    public async Task InvokeOnMainThreadAsync(Action action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _dispatchedActions.Add(action);
        }

        if (DispatchDelay > TimeSpan.Zero)
        {
            await Task.Delay(DispatchDelay, cancellationToken);
        }

        action();
    }

    /// <inheritdoc />
    public async Task<T> InvokeOnMainThreadAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(func);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _dispatchedActions.Add(() => func());
        }

        if (DispatchDelay > TimeSpan.Zero)
        {
            await Task.Delay(DispatchDelay, cancellationToken);
        }

        return func();
    }

    /// <inheritdoc />
    public async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> asyncFunc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asyncFunc);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _dispatchedActions.Add(() => asyncFunc());
        }

        if (DispatchDelay > TimeSpan.Zero)
        {
            await Task.Delay(DispatchDelay, cancellationToken);
        }

        return await asyncFunc();
    }

    /// <inheritdoc />
    public async Task InvokeOnMainThreadDelayedAsync(Action action, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        await Task.Delay(delay, cancellationToken);
        await InvokeOnMainThreadAsync(action, cancellationToken);
    }

    /// <summary>
    /// Resets the dispatch counter.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _dispatchedActions.Clear();
        }
    }
}