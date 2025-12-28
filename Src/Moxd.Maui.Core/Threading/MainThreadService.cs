using Microsoft.Maui.ApplicationModel;

namespace Moxd.Threading;

/// <summary>
/// MAUI implementation of <see cref="IMainThreadService"/>.
/// Wraps <see cref="MainThread"/> for testability.
/// </summary>
public sealed class MainThreadService : IMainThreadService
{
    /// <inheritdoc />
    public bool IsMainThread => MainThread.IsMainThread;

    /// <inheritdoc />
    public void InvokeOnMainThread(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsMainThread)
        {
            action();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(action);
        }
    }

    /// <inheritdoc />
    public Task InvokeOnMainThreadAsync(Action action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        cancellationToken.ThrowIfCancellationRequested();

        if (IsMainThread)
        {
            action();
            return Task.CompletedTask;
        }

        return MainThread.InvokeOnMainThreadAsync(action);
    }

    /// <inheritdoc />
    public Task<T> InvokeOnMainThreadAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(func);

        cancellationToken.ThrowIfCancellationRequested();

        return IsMainThread ? Task.FromResult(func()) : MainThread.InvokeOnMainThreadAsync(func);
    }

    /// <inheritdoc />
    public async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> asyncFunc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asyncFunc);

        cancellationToken.ThrowIfCancellationRequested();

        return IsMainThread
            ? await asyncFunc().ConfigureAwait(false)
            : await MainThread.InvokeOnMainThreadAsync(asyncFunc).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task InvokeOnMainThreadDelayedAsync(Action action, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        await InvokeOnMainThreadAsync(action, cancellationToken).ConfigureAwait(false);
    }
}