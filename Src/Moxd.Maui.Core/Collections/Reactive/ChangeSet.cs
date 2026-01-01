namespace Moxd.Collections.Reactive;

/// <summary>
/// A batch of changes representing an atomic update to a collection.
/// Following DynamicData's ChangeSet pattern for diff-based updates.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public sealed partial class ChangeSet<T> : List<Change<T>>
{
    #region Properties
    /// <summary>Number of Add operations in this change set.</summary>
    public int Adds
    {
        get => this.Count(c => c.Reason == ChangeReason.Add);
    }

    /// <summary>Number of Remove operations in this change set.</summary>
    public int Removes
    {
        get => this.Count(c => c.Reason == ChangeReason.Remove);
    }

    /// <summary>Number of Move operations in this change set.</summary>
    public int Moves
    {
        get => this.Count(c => c.Reason == ChangeReason.Move);
    }

    /// <summary>Number of Replace operations in this change set.</summary>
    public int Replaces
    {
        get => this.Count(c => c.Reason == ChangeReason.Replace);
    }

    /// <summary>Number of Refresh operations in this change set.</summary>
    public int Refreshes
    {
        get => this.Count(c => c.Reason == ChangeReason.Refresh);
    }

    /// <summary>Whether this change set has any changes.</summary>
    public bool HasChanges
    {
        get => Count > 0;
    }
    #endregion

    /// <summary>Creates an empty change set.</summary>
    public ChangeSet()
    {
    }

    /// <summary>Creates a change set with specified capacity.</summary>
    public ChangeSet(int capacity) : base(capacity)
    {
    }

    /// <summary>Creates a change set from existing changes.</summary>
    public ChangeSet(IEnumerable<Change<T>> changes) : base(changes) 
    {
    }

    #region Override Methods
    /// <inheritdoc />
    public override string ToString()
        => $"ChangeSet: {Adds} adds, {Removes} removes, {Moves} moves, {Replaces} replaces, {Refreshes} refreshes";
    #endregion
}