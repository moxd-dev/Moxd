namespace Moxd.Utilities;

/// <summary>
/// Throttles calls to an action, ensuring it executes at most once per time interval.
/// Unlike debouncing, throttling executes immediately and then prevents further calls until the interval passes.
/// Useful for scroll handlers, mouse move events, and API rate limiting.
/// </summary>
public sealed partial class Throttler : IDisposable
{
    #region Fields
    private readonly TimeSpan _interval;
    private readonly Lock _lock = new();
    private DateTime _lastExecutionTime = DateTime.MinValue;
    private bool _disposed;
    private Action? _pendingAction;
    private CancellationTokenSource? _pendingCts;
    #endregion

    /// <summary>
    /// Creates a new throttler with the specified interval.
    /// </summary>
    /// <param name="interval">The minimum interval between executions.</param>
    public Throttler(TimeSpan interval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
        _interval = interval;
    }

    /// <summary>
    /// Throttles the action. Executes immediately if the interval has passed,
    /// otherwise schedules execution for when the interval completes.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="executeTrailing">If true, executes the last call after the interval. If false, drops trailing calls.</param>
    public void Throttle(Action action, bool executeTrailing = true)
    {
        ArgumentNullException.ThrowIfNull(action);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan elapsed = now - _lastExecutionTime;

            if (elapsed >= _interval)
            {
                // Interval has passed, execute immediately
                _lastExecutionTime = now;
                _pendingAction = null;
                _pendingCts?.Cancel();
                _pendingCts?.Dispose();
                _pendingCts = null;

                action();
            }
            else if (executeTrailing)
            {
                // Schedule for later
                _pendingAction = action;
                _pendingCts?.Cancel();
                _pendingCts?.Dispose();
                _pendingCts = new CancellationTokenSource();

                TimeSpan delay = _interval - elapsed;
                CancellationToken token = _pendingCts.Token;

                _ = Task.Delay(delay, token).ContinueWith(_ =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        Action? actionToExecute;
                        lock (_lock)
                        {
                            actionToExecute = _pendingAction;
                            _pendingAction = null;
                            _lastExecutionTime = DateTime.UtcNow;
                        }

                        actionToExecute?.Invoke();
                    },
                    token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            }
            // If not executeTrailing, we simply drop this call
        }
    }

    /// <summary>
    /// Throttles the async action. Executes immediately if the interval has passed.
    /// </summary>
    /// <param name="action">The async action to execute.</param>
    /// <param name="executeTrailing">If true, executes the last call after the interval.</param>
    /// <returns>A task that completes when the action executes or is throttled.</returns>
    public async Task ThrottleAsync(Func<Task> action, bool executeTrailing = true)
    {
        ArgumentNullException.ThrowIfNull(action);
        ObjectDisposedException.ThrowIf(_disposed, this);

        bool shouldExecuteNow;
        TimeSpan delay;

        lock (_lock)
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan elapsed = now - _lastExecutionTime;

            if (elapsed >= _interval)
            {
                _lastExecutionTime = now;
                shouldExecuteNow = true;
                delay = TimeSpan.Zero;
            }
            else
            {
                shouldExecuteNow = false;
                delay = _interval - elapsed;
            }
        }

        if (shouldExecuteNow)
        {
            await action().ConfigureAwait(false);
        }
        else if (executeTrailing)
        {
            await Task.Delay(delay).ConfigureAwait(false);

            lock (_lock)
            {
                _lastExecutionTime = DateTime.UtcNow;
            }

            await action().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Resets the throttler, allowing immediate execution on the next call.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _lastExecutionTime = DateTime.MinValue;
            _pendingAction = null;
            _pendingCts?.Cancel();
            _pendingCts?.Dispose();
            _pendingCts = null;
        }
    }

    /// <summary>
    /// Cancels any pending throttled action.
    /// </summary>
    public void Cancel()
    {
        lock (_lock)
        {
            _pendingAction = null;
            _pendingCts?.Cancel();
            _pendingCts?.Dispose();
            _pendingCts = null;
        }
    }

    /// <summary>
    /// Gets the time remaining until the next execution is allowed.
    /// Returns <see cref="TimeSpan.Zero"/> if execution is allowed immediately.
    /// </summary>
    public TimeSpan TimeUntilNextAllowed
    {
        get
        {
            lock (_lock)
            {
                TimeSpan elapsed = DateTime.UtcNow - _lastExecutionTime;
                TimeSpan remaining = _interval - elapsed;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
    }

    /// <summary>
    /// Gets whether there is a pending throttled action.
    /// </summary>
    public bool HasPending
    {
        get
        {
            lock (_lock)
            {
                return _pendingAction is not null;
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
            _pendingAction = null;
            _pendingCts?.Cancel();
            _pendingCts?.Dispose();
            _pendingCts = null;
        }
    }
}