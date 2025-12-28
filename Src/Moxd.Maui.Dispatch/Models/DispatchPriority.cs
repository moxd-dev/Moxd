namespace Moxd.Models;

/// <summary>
/// Specifies the priority level for UI dispatch operations.
/// Higher priority operations are processed before lower priority ones.
/// </summary>
public enum DispatchPriority
{
    /// <summary>
    /// Low priority. Use for non-urgent background updates.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority. Default for most operations.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority. Use for important user-initiated updates.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority. Use for urgent updates that must be processed immediately.
    /// Examples: Error messages, critical state changes.
    /// </summary>
    Critical = 3
}