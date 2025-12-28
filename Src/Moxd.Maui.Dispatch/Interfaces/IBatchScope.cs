namespace Moxd.Interfaces;

/// <summary>
/// Represents a batch scope that collects UI dispatch operations
/// and executes them as a single batch when disposed.
/// </summary>
public interface IBatchScope : IDisposable
{
    /// <summary>
    /// Gets whether this batch scope is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the number of pending operations in this batch.
    /// </summary>
    int PendingCount { get; }

    /// <summary>
    /// Queues an action to be executed when the batch completes.
    /// </summary>
    /// <param name="action">The action to queue.</param>
    void Enqueue(Action action);

    /// <summary>
    /// Immediately flushes all pending operations without ending the batch.
    /// </summary>
    void Flush();

    /// <summary>
    /// Cancels all pending operations and ends the batch without executing them.
    /// </summary>
    void Cancel();
}