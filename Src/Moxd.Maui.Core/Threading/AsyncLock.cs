namespace Moxd.Threading;

/// <summary>
/// Provides an async-compatible locking mechanism.
/// Use this instead of <c>lock</c> statements in async code.
/// </summary>
public sealed partial class AsyncLock : IDisposable
{
    #region Fields
    private bool _disposed;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    #endregion

    /// <summary>
    /// Asynchronously acquires the lock.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A disposable that releases the lock when disposed.</returns>
    public async Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Releaser(this);
    }

    /// <summary>
    /// Synchronously acquires the lock.
    /// Prefer <see cref="LockAsync"/> in async contexts.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A disposable that releases the lock when disposed.</returns>
    public IDisposable Lock(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _semaphore.Wait(cancellationToken);
        return new Releaser(this);
    }

    /// <summary>
    /// Attempts to acquire the lock without waiting.
    /// </summary>
    /// <param name="releaser">The disposable to release the lock, if acquired.</param>
    /// <returns>True if the lock was acquired; otherwise, false.</returns>
    public bool TryLock(out IDisposable? releaser)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_semaphore.Wait(0))
        {
            releaser = new Releaser(this);
            return true;
        }

        releaser = null;
        return false;
    }

    /// <summary>
    /// Gets whether the lock is currently held.
    /// </summary>
    public bool IsLocked => _semaphore.CurrentCount == 0;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _semaphore.Dispose();
    }

    private sealed partial class Releaser(AsyncLock parent) : IDisposable
    {
        #region Fields
        private readonly AsyncLock _parent = parent;
        private bool _disposed;
        #endregion

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (!_parent._disposed)
                _parent._semaphore.Release();
        }
    }
}