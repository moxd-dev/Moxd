using Moxd.Interfaces;

namespace Moxd.Services;

/// <summary>
/// Implementation of <see cref="IBatchScope"/> that collects actions
/// and executes them as a single batch on dispose.
/// </summary>
internal sealed partial class BatchScope : IBatchScope
{
    private readonly Action<List<Action>> _executeActions;
    private readonly Action? _onDispose;
    private readonly List<Action> _pendingActions = [];
    private readonly Lock _lock = new();
    private bool _disposed;
    private bool _cancelled;

    /// <summary>
    /// Creates a new batch scope.
    /// </summary>
    /// <param name="executeActions">Callback to execute batched actions.</param>
    /// <param name="onDispose">Optional callback when scope is disposed.</param>
    internal BatchScope(Action<List<Action>> executeActions, Action? onDispose = null)
    {
        _executeActions = executeActions ?? throw new ArgumentNullException(nameof(executeActions));
        _onDispose = onDispose;
    }

    /// <inheritdoc />
    public bool IsActive => !_disposed && !_cancelled;

    /// <inheritdoc />
    public int PendingCount
    {
        get
        {
            lock (_lock)
            {
                return _pendingActions.Count;
            }
        }
    }

    /// <inheritdoc />
    public void Enqueue(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_cancelled)
            {
                return;
            }

            _pendingActions.Add(action);
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        List<Action> actionsToExecute;

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_cancelled || _pendingActions.Count == 0)
            {
                return;
            }

            actionsToExecute = [.. _pendingActions];
            _pendingActions.Clear();
        }

        // Execute without triggering dispose callback
        _executeActions(actionsToExecute);
    }

    /// <inheritdoc />
    public void Cancel()
    {
        lock (_lock)
        {
            _cancelled = true;
            _pendingActions.Clear();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        List<Action>? actionsToExecute = null;

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (!_cancelled && _pendingActions.Count > 0)
            {
                actionsToExecute = [.. _pendingActions];
                _pendingActions.Clear();
            }
        }

        if (actionsToExecute is { Count: > 0 })
        {
            _executeActions(actionsToExecute);
        }

        // Always notify dispose so dispatcher can clear the reference
        _onDispose?.Invoke();
    }
}