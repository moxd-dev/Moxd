using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Moxd.Collections;

/// <summary>
/// An <see cref="ObservableCollection{T}"/> with support for bulk operations.
/// Unlike the standard ObservableCollection, this raises a single notification
/// for bulk operations, improving UI performance significantly.
/// </summary>
public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    #region Fields
    private bool _suppressNotifications;
    #endregion

    /// <summary>
    /// Initializes a new empty instance.
    /// </summary>
    public ObservableRangeCollection()
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified items.
    /// </summary>
    /// <param name="collection">The items to add.</param>
    public ObservableRangeCollection(IEnumerable<T> collection) : base(collection)
    {
    }

    /// <summary>
    /// Adds a range of items to the collection.
    /// Raises a single <see cref="INotifyCollectionChanged.CollectionChanged"/> event.
    /// </summary>
    /// <param name="items">The items to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    public void AddRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        IList<T> itemList = items as IList<T> ?? [.. items];

        if (itemList.Count == 0)
            return;

        CheckReentrancy();

        int startIndex = Count;

        _suppressNotifications = true;
        try
        {
            foreach (T item in itemList)
            {
                Items.Add(item);
            }
        }
        finally
        {
            _suppressNotifications = false;
        }

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (System.Collections.IList)itemList, startIndex));
    }

    /// <summary>
    /// Inserts a range of items at the specified index.
    /// Raises a single <see cref="INotifyCollectionChanged.CollectionChanged"/> event.
    /// </summary>
    /// <param name="index">The index at which to insert.</param>
    /// <param name="items">The items to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public void InsertRange(int index, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);

        IList<T> itemList = items as IList<T> ?? [.. items];

        if (itemList.Count == 0)
            return;

        CheckReentrancy();

        _suppressNotifications = true;
        try
        {
            int insertIndex = index;
            foreach (T item in itemList)
            {
                Items.Insert(insertIndex++, item);
            }
        }
        finally
        {
            _suppressNotifications = false;
        }

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (System.Collections.IList)itemList, index));
    }

    /// <summary>
    /// Removes a range of items from the collection.
    /// Raises a single <see cref="INotifyCollectionChanged.CollectionChanged"/> event.
    /// </summary>
    /// <param name="items">The items to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    public void RemoveRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        IList<T> itemList = items as IList<T> ?? [.. items];

        if (itemList.Count == 0)
            return;

        CheckReentrancy();

        List<T> removedItems = [];

        _suppressNotifications = true;
        try
        {
            foreach (T item in itemList)
            {
                if (Items.Remove(item))
                {
                    removedItems.Add(item);
                }
            }
        }
        finally
        {
            _suppressNotifications = false;
        }

        if (removedItems.Count > 0)
        {
            OnCountPropertyChanged();
            OnIndexerPropertyChanged();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems));
        }
    }

    /// <summary>
    /// Removes all items matching the predicate.
    /// Raises a single <see cref="INotifyCollectionChanged.CollectionChanged"/> event.
    /// </summary>
    /// <param name="predicate">The condition for removal.</param>
    /// <returns>The number of items removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when predicate is null.</exception>
    public int RemoveRange(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        List<T> itemsToRemove = [.. Items.Where(predicate)];
        RemoveRange(itemsToRemove);
        return itemsToRemove.Count;
    }

    /// <summary>
    /// Replaces all items in the collection with the new items.
    /// More efficient than Clear() followed by AddRange().
    /// Raises a single <see cref="INotifyCollectionChanged.CollectionChanged"/> event with Reset action.
    /// </summary>
    /// <param name="items">The new items.</param>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    public void ReplaceRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        IList<T> itemList = items as IList<T> ?? [.. items];

        CheckReentrancy();

        _suppressNotifications = true;
        try
        {
            Items.Clear();
            foreach (T item in itemList)
            {
                Items.Add(item);
            }
        }
        finally
        {
            _suppressNotifications = false;
        }

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Replaces items in a specific range with new items.
    /// </summary>
    /// <param name="startIndex">The starting index of items to replace.</param>
    /// <param name="count">The number of items to replace.</param>
    /// <param name="items">The new items to insert.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index or count is invalid.</exception>
    public void ReplaceRange(int startIndex, int count, IEnumerable<T> items)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex + count, Count);

        IList<T> itemList = items as IList<T> ?? [.. items];

        CheckReentrancy();

        _suppressNotifications = true;
        try
        {
            // Remove old items
            for (int i = 0; i < count; i++)
            {
                Items.RemoveAt(startIndex);
            }

            // Insert new items
            int insertIndex = startIndex;
            foreach (T item in itemList)
            {
                Items.Insert(insertIndex++, item);
            }
        }
        finally
        {
            _suppressNotifications = false;
        }

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Moves an item from one index to another.
    /// </summary>
    /// <param name="oldIndex">The current index of the item.</param>
    /// <param name="newIndex">The new index for the item.</param>
    public new void Move(int oldIndex, int newIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(oldIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(oldIndex, Count);
        ArgumentOutOfRangeException.ThrowIfNegative(newIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(newIndex, Count);

        if (oldIndex == newIndex)
            return;

        base.Move(oldIndex, newIndex);
    }

    /// <summary>
    /// Sorts the collection in place using the default comparer.
    /// Raises a single Reset notification.
    /// </summary>
    public void Sort()
    {
        Sort(Comparer<T>.Default);
    }

    /// <summary>
    /// Sorts the collection in place using the specified comparer.
    /// Raises a single Reset notification.
    /// </summary>
    /// <param name="comparer">The comparer to use.</param>
    public void Sort(IComparer<T> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);

        CheckReentrancy();

        List<T> sorted = [.. Items.OrderBy(x => x, comparer)];

        _suppressNotifications = true;
        try
        {
            Items.Clear();
            foreach (T item in sorted)
            {
                Items.Add(item);
            }
        }
        finally
        {
            _suppressNotifications = false;
        }

        OnIndexerPropertyChanged();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Sorts the collection in place using a key selector.
    /// Raises a single Reset notification.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The key selector.</param>
    /// <param name="descending">Whether to sort in descending order.</param>
    public void Sort<TKey>(Func<T, TKey> keySelector, bool descending = false)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        CheckReentrancy();

        List<T> sorted = descending
            ? [.. Items.OrderByDescending(keySelector)]
            : [.. Items.OrderBy(keySelector)];

        _suppressNotifications = true;
        try
        {
            Items.Clear();
            foreach (T item in sorted)
            {
                Items.Add(item);
            }
        }
        finally
        {
            _suppressNotifications = false;
        }

        OnIndexerPropertyChanged();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Executes an action that may modify multiple items, raising only a single Reset notification.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void BatchUpdate(Action<IList<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        CheckReentrancy();

        _suppressNotifications = true;
        try
        {
            action(Items);
        }
        finally
        {
            _suppressNotifications = false;
        }

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <inheritdoc />
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotifications)
        {
            base.OnCollectionChanged(e);
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!_suppressNotifications)
        {
            base.OnPropertyChanged(e);
        }
    }

    private void OnCountPropertyChanged()
    {
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
    }

    private void OnIndexerPropertyChanged()
    {
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
    }
}