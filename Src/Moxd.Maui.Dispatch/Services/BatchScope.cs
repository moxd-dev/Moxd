using Moxd.Interfaces;

namespace Moxd.Services;

/// <summary>
/// Implementation of <see cref="IBatchScope"/> that collects actions
/// and executes them as a single batch on dispose.
/// </summary>
internal sealed partial class BatchScope : IBatchScope
{
    #region Fields
    private readonly Action<List<Action>> _onDispose;
    private readonly List<Action> _pendingActions = [];
    private readonly Lock _lock = new();
    private bool _disposed;
    private bool _cancelled;
    #endregion

    /// <summary>
    /// Creates a new batch scope.
    /// </summary>
    /// <param name="onDispose">Callback to execute all batched actions.</param>
    internal BatchScope(Action<List<Action>> onDispose)
    {
        _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
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
                return;

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
                return;

            actionsToExecute = [.. _pendingActions];
            _pendingActions.Clear();
        }

        _onDispose(actionsToExecute);
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
                return;

            _disposed = true;

            if (!_cancelled && _pendingActions.Count > 0)
            {
                actionsToExecute = [.. _pendingActions];
                _pendingActions.Clear();
            }
        }

        if (actionsToExecute is { Count: > 0 })
            _onDispose(actionsToExecute);
    }
}