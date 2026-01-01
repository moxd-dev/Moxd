namespace Moxd.Collections.Reactive;

/// <summary>
/// The reason for a change in the collection.
/// </summary>
public enum ChangeReason
{
    /// <summary>Item was added.</summary>
    Add,
    /// <summary>Item was removed.</summary>
    Remove,
    /// <summary>Item was replaced.</summary>
    Replace,
    /// <summary>Item was moved.</summary>
    Move,
    /// <summary>Item needs re-evaluation (property changed).</summary>
    Refresh
}