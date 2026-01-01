namespace Moxd.Collections.Reactive;

/// <summary>
/// Represents a single change in a reactive collection.
/// Immutable struct for efficient memory usage.
/// </summary>
/// <typeparam name="T">The type of item that changed.</typeparam>
public readonly struct Change<T>
{
    #region Properties
    /// <summary>The reason for this change.</summary>
    public ChangeReason Reason { get; }

    /// <summary>The item involved in the change.</summary>
    public T Item { get; }

    /// <summary>The previous item (for Replace operations).</summary>
    public T? PreviousItem { get; }

    /// <summary>The current index of the item.</summary>
    public int CurrentIndex { get; }

    /// <summary>The previous index (for Move operations).</summary>
    public int PreviousIndex { get; }
    #endregion

    private Change(ChangeReason reason, T item, int currentIndex, int previousIndex = -1, T? previousItem = default)
    {
        Reason = reason;
        Item = item;
        CurrentIndex = currentIndex;
        PreviousIndex = previousIndex;
        PreviousItem = previousItem;
    }

    #region Static Methods
    /// <summary>Creates an Add change.</summary>
    public static Change<T> Add(T item, int index)
    {
        return new(ChangeReason.Add, item, index);
    }

    /// <summary>Creates a Remove change.</summary>
    public static Change<T> Remove(T item, int index)
    {
        return new(ChangeReason.Remove, item, index);
    }

    /// <summary>Creates a Replace change.</summary>
    public static Change<T> Replace(T newItem, T oldItem, int index)
    {
        return new(ChangeReason.Replace, newItem, index, index, oldItem);
    }

    /// <summary>Creates a Move change.</summary>
    public static Change<T> Move(T item, int newIndex, int oldIndex)
    {
        return new(ChangeReason.Move, item, newIndex, oldIndex);
    }

    /// <summary>Creates a Refresh change.</summary>
    public static Change<T> Refresh(T item, int index)
    {
        return new(ChangeReason.Refresh, item, index);
    }
    #endregion

    #region Override Methods
    /// <inheritdoc />
    public override string ToString()
        => $"{Reason}: {Item} at {CurrentIndex}" +
           (Reason == ChangeReason.Move ? $" (from {PreviousIndex})" : "");
    #endregion
}