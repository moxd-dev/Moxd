namespace Moxd.Utilities;

/// <summary>
/// Debounces rapid calls to an action, executing only after a period of inactivity.
/// Useful for search boxes, resize handlers, and other scenarios with rapid input.
/// </summary>
public sealed partial class Debouncer : IDisposable
{
    #region Fields
    private readonly TimeSpan _delay;
    private CancellationTokenSource? _cts;
    private readonly Lock _lock = new();
    private bool _disposed;
    #endregion

    /// <summary>
    /// Creates a new debouncer with the specified delay.
    /// </summary>
    /// <param name="delay">The delay to wait after the last call before executing.</param>
    public Debouncer(TimeSpan delay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(delay, TimeSpan.Zero);
        _delay = delay;
    }

    /// <summary>
    /// Debounces the action. If called multiple times within the delay period,
    /// only the last call will execute after the delay.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void Debounce(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ObjectDisposedException.ThrowIf(_disposed, this);

        CancellationToken token;

        lock (_lock)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            token = _cts.Token;
        }

        _ = Task.Delay(_delay, token).ContinueWith(_ =>
            {
                if (!token.IsCancellationRequested)
                {
                    action();
                }
            }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }

    /// <summary>
    /// Debounces the async action. If called multiple times within the delay period,
    /// only the last call will execute after the delay.
    /// </summary>
    /// <param name="action">The async action to execute.</param>
    /// <returns>A task that completes when the debounced action executes or is cancelled.</returns>
    public async Task DebounceAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ObjectDisposedException.ThrowIf(_disposed, this);

        CancellationToken token;

        lock (_lock)
        {
            _cts?.CancelAsync();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            token = _cts.Token;
        }

        try
        {
            await Task.Delay(_delay, token).ConfigureAwait(false);

            if (!token.IsCancellationRequested)
            {
                await action().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Debounced - this is expected
        }
    }

    /// <summary>
    /// Debounces the action with a result. Returns the result of the last call.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <returns>A task containing the result, or default if cancelled.</returns>
    public async Task<T?> DebounceAsync<T>(Func<Task<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        ObjectDisposedException.ThrowIf(_disposed, this);

        CancellationToken token;

        lock (_lock)
        {
            _cts?.CancelAsync();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            token = _cts.Token;
        }

        try
        {
            await Task.Delay(_delay, token).ConfigureAwait(false);

            if (!token.IsCancellationRequested)
            {
                return await func().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Debounced - this is expected
        }

        return default;
    }

    /// <summary>
    /// Cancels any pending debounced action.
    /// </summary>
    public void Cancel()
    {
        lock (_lock)
        {
            _cts?.Cancel();
        }
    }

    /// <summary>
    /// Gets whether there is a pending debounced action.
    /// </summary>
    public bool IsPending
    {
        get
        {
            lock (_lock)
            {
                return _cts is not null && !_cts.IsCancellationRequested;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        lock (_lock)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}