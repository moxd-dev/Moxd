namespace Moxd.Threading;

/// <summary>
/// Helper for dispatching actions to the UI thread using MAUI's <see cref="IDispatcher"/>.
/// </summary>
public static class DispatcherHelper
{
    /// <summary>
    /// Dispatches an action to the UI thread using the provided dispatcher.
    /// If already on the UI thread, executes immediately.
    /// </summary>
    /// <param name="dispatcher">The dispatcher to use.</param>
    /// <param name="action">The action to dispatch.</param>
    /// <exception cref="ArgumentNullException">Thrown when dispatcher or action is null.</exception>
    public static void Dispatch(IDispatcher dispatcher, Action action)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(action);

        if (dispatcher.IsDispatchRequired)
            dispatcher.Dispatch(action);
        else
            action();
    }

    /// <summary>
    /// Dispatches an action asynchronously to the UI thread.
    /// </summary>
    /// <param name="dispatcher">The dispatcher to use.</param>
    /// <param name="action">The action to dispatch.</param>
    /// <returns>A task that completes when the action has been dispatched.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dispatcher or action is null.</exception>
    public static Task DispatchAsync(IDispatcher dispatcher, Action action)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(action);

        if (dispatcher.IsDispatchRequired)
            return dispatcher.DispatchAsync(action);

        action();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Dispatches a function asynchronously to the UI thread and returns the result.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="dispatcher">The dispatcher to use.</param>
    /// <param name="func">The function to dispatch.</param>
    /// <returns>A task containing the result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dispatcher or func is null.</exception>
    public static Task<T> DispatchAsync<T>(IDispatcher dispatcher, Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(func);

        return dispatcher.IsDispatchRequired ? 
            dispatcher.DispatchAsync(func) : 
            Task.FromResult(func());
    }

    /// <summary>
    /// Creates a test dispatcher that executes actions immediately on the current thread.
    /// Use this in unit tests when you need an IDispatcher instance.
    /// </summary>
    /// <returns>A test dispatcher instance.</returns>
    public static IDispatcher CreateTestDispatcher() => new TestDispatcher();

    /// <summary>
    /// A simple dispatcher for unit testing that executes actions immediately.
    /// </summary>
    private sealed class TestDispatcher : IDispatcher
    {
        public bool IsDispatchRequired => false;

        public bool Dispatch(Action action)
        {
            action();
            return true;
        }

        public bool DispatchDelayed(TimeSpan delay, Action action)
        {
            Task.Delay(delay).ContinueWith(_ => action());
            return true;
        }

        public IDispatcherTimer CreateTimer() => new TestDispatcherTimer();
    }

    /// <summary>
    /// A simple dispatcher timer for unit testing.
    /// </summary>
    private sealed class TestDispatcherTimer : IDispatcherTimer
    {
        #region Fields
        private Timer? _timer;
        #endregion

        #region Properties
        public TimeSpan Interval { get; set; }

        public bool IsRepeating { get; set; } = true;

        public bool IsRunning { get; private set; }
        #endregion

        #region Events
        public event EventHandler? Tick;
        #endregion

        #region Public Methods
        public void Start()
        {
            IsRunning = true;
            _timer = new Timer(
                _ => Tick?.Invoke(this, EventArgs.Empty),
                null,
                Interval,
                IsRepeating ? Interval : Timeout.InfiniteTimeSpan);
        }

        public void Stop()
        {
            IsRunning = false;
            _timer?.Dispose();
            _timer = null;
        }
        #endregion
    }
}