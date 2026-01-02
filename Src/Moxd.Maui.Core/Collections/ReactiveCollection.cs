using Moxd.Guards;
using Moxd.Threading;
using Moxd.Collections.Reactive;
using System.Collections;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Moxd.Collections;

/// <summary>
/// A reactive collection with filtering, sorting, and UI binding.
/// 
/// <para>
/// Uses <see cref="ObservableRangeCollection{T}"/> internally for efficient batch operations.
/// Follows DynamicData's ChangeSet pattern for diff-based updates.
/// </para>
/// 
/// <para><b>Thread Safety:</b> All public methods are thread-safe. The collection can be
/// modified from any thread; UI updates are automatically marshaled to the main thread.</para>
/// 
/// <example>
/// <code>
/// // Simple usage
/// public ReactiveCollection&lt;Product&gt; Products { get; } = new();
/// 
/// // Load data
/// Products.Load(items);
/// 
/// // Filter
/// Products.Filter(p =&gt; p.IsActive);
/// 
/// // Sort
/// Products.Sort(p =&gt; p.Name);
/// 
/// // In XAML: ItemsSource="{Binding Products.View}"
/// </code>
/// </example>
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public sealed class ReactiveCollection<T> : IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged, IDisposable
{
    #region Fields
    private readonly List<T> _sourceItems = [];
    private readonly HashSet<T> _filteredItems = [];
    private readonly ObservableRangeCollection<T> _internalCollection;
    private readonly IDispatcher _dispatcher;
    private readonly Lock _lock = new();

    private Func<T, bool>? _filterPredicate;
    private IComparer<T>? _sortComparer;
    private bool _disposed;
    #endregion

    #region Properties
    /// <summary>
    /// The filtered and sorted view. Bind your UI to this property.
    /// This is a read-only view; modifications must go through ReactiveCollection methods.
    /// </summary>
    public ReadOnlyObservableCollection<T> View { get; }

    /// <summary>
    /// Number of visible items (after filter).
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
                return _internalCollection.Count;
        }
    }

    /// <summary>
    /// Total items in source (before filter).
    /// </summary>
    public int SourceCount
    {
        get
        {
            lock (_lock)
                return _sourceItems.Count;
        }
    }

    /// <summary>
    /// Number of items filtered out.
    /// </summary>
    public int FilteredOutCount
    {
        get => SourceCount - Count;
    }

    /// <summary>
    /// Whether a filter is active.
    /// </summary>
    public bool IsFiltered
    {
        get
        {
            lock (_lock)
                return _filterPredicate != null;
        }
    }

    /// <summary>
    /// Whether sorting is active.
    /// </summary>
    public bool IsSorted
    {
        get
        {
            lock (_lock)
                return _sortComparer != null;
        }
    }

    /// <summary>
    /// Gets item at index in view.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The item at the specified index.</returns>
    public T this[int index]
    {
        get
        {
            lock (_lock)
                return _internalCollection[index];
        }
    }
    #endregion

    #region Events
    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add => _internalCollection.CollectionChanged += value;
        remove => _internalCollection.CollectionChanged -= value;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion

    /// <summary>
    /// Creates an empty reactive collection.
    /// </summary>
    /// <param name="dispatcher">
    /// The dispatcher for UI thread marshaling. 
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when dispatcher is null.</exception>
    public ReactiveCollection(IDispatcher dispatcher)
    {
        _dispatcher = Guard.IsNotNull(dispatcher);
        _internalCollection = [];
        View = new ReadOnlyObservableCollection<T>(_internalCollection);
    }

    /// <summary>
    /// Creates a reactive collection with initial items.
    /// </summary>
    /// <param name="dispatcher">The dispatcher for UI thread marshaling.</param>
    /// <param name="items">The initial items to load.</param>
    /// <exception cref="ArgumentNullException">Thrown when dispatcher or items is null.</exception>
    public ReactiveCollection(IDispatcher dispatcher, IEnumerable<T> items) 
        : this(dispatcher)
    {
        Guard.IsNotNull(items);
        Load(items);
    }

    /// <summary>
    /// Creates a reactive collection with initial filter and/or sort.
    /// </summary>
    /// <param name="dispatcher">The dispatcher for UI thread marshaling.</param>
    /// <param name="filter">Optional filter predicate.</param>
    /// <param name="sortBy">Optional sort key selector.</param>
    /// <param name="descending">Whether to sort in descending order.</param>
    /// <exception cref="ArgumentNullException">Thrown when dispatcher is null.</exception>
    public ReactiveCollection(IDispatcher dispatcher, Func<T, bool>? filter, Func<T, object>? sortBy = null, bool descending = false) 
        : this(dispatcher)
    {
        _filterPredicate = filter;
        if (sortBy != null)
            _sortComparer = CreateComparer(sortBy, descending);
    }

    #region Data Operations
    /// <summary>
    /// Loads items, replacing all existing items.
    /// </summary>
    /// <param name="items">The items to load.</param>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    public void Load(IEnumerable<T> items)
    {
        Guard.IsNotNull(items);
        List<T> itemList = [.. items];

        lock (_lock)
        {
            _sourceItems.Clear();
            _sourceItems.AddRange(itemList);
        }

        RebuildView();
    }

    /// <summary>
    /// Loads items asynchronously (fetching on background thread, UI update on main thread).
    /// </summary>
    /// <param name="fetchItems">The async function to fetch items.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when fetchItems is null.</exception>
    public async Task LoadAsync(Func<Task<IEnumerable<T>>> fetchItems, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(fetchItems);

        IEnumerable<T> items = await Task.Run(
            async () => await fetchItems().ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        Load(items);
    }

    /// <summary>
    /// Loads items asynchronously with a synchronous fetch function.
    /// </summary>
    /// <param name="fetchItems">The function to fetch items.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when fetchItems is null.</exception>
    public async Task LoadAsync(Func<IEnumerable<T>> fetchItems, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(fetchItems);

        IEnumerable<T> items = await Task.Run(fetchItems, cancellationToken).ConfigureAwait(false);
        Load(items);
    }

    /// <summary>
    /// Adds a single item.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
    public void Add(T item)
    {
        Guard.IsNotNull(item);

        bool passesFilter;
        int insertIndex;

        lock (_lock)
        {
            _sourceItems.Add(item);
            passesFilter = PassesFilter(item);

            if (!passesFilter)
                return;

            _filteredItems.Add(item);
            insertIndex = FindInsertIndex(item);
        }

        DispatchToUI(() => _internalCollection.Insert(insertIndex, item));
    }

    /// <summary>
    /// Adds multiple items efficiently.
    /// </summary>
    /// <param name="items">The items to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    public void AddRange(IEnumerable<T> items)
    {
        Guard.IsNotNull(items);
        List<T> itemList = [.. items];

        if (itemList.Count == 0)
            return;

        lock (_lock)
        {
            _sourceItems.AddRange(itemList);

            List<T> toAdd = [.. itemList.Where(PassesFilter)];

            if (toAdd.Count == 0)
                return;

            foreach (T item in toAdd)
                _filteredItems.Add(item);

            // Always rebuild for AddRange to avoid race conditions with insert indices
            // This is also more efficient for batch operations
        }

        RebuildView();
    }

    /// <summary>
    /// Removes a single item.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>True if the item was found and removed; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
    public bool Remove(T item)
    {
        Guard.IsNotNull(item);

        int removeIndex;

        lock (_lock)
        {
            if (!_sourceItems.Remove(item))
                return false;

            if (!_filteredItems.Remove(item))
                return true; // Was in source but not in view

            removeIndex = _internalCollection.IndexOf(item);

            if (removeIndex < 0)
                return true;
        }

        DispatchToUI(() =>
        {
            if (removeIndex < _internalCollection.Count)
                _internalCollection.RemoveAt(removeIndex);
        });

        return true;
    }

    /// <summary>
    /// Removes all items matching the predicate.
    /// </summary>
    /// <param name="predicate">The condition for removal.</param>
    /// <returns>The number of items removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when predicate is null.</exception>
    public int RemoveWhere(Func<T, bool> predicate)
    {
        Guard.IsNotNull(predicate);

        List<T> toRemoveFromView;
        int removedCount;

        lock (_lock)
        {
            List<T> toRemove = [.. _sourceItems.Where(predicate)];

            removedCount = toRemove.Count;

            if (removedCount == 0)
                return 0;

            toRemoveFromView = [.. toRemove.Where(_filteredItems.Contains)];

            foreach (T item in toRemove)
            {
                _sourceItems.Remove(item);
                _filteredItems.Remove(item);
            }
        }

        if (toRemoveFromView.Count > 0)
            DispatchToUI(() => _internalCollection.RemoveRange(toRemoveFromView));

        return removedCount;
    }

    /// <summary>
    /// Clears all items.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _sourceItems.Clear();
            _filteredItems.Clear();
        }

        DispatchToUI(_internalCollection.Clear);
    }

    /// <summary>
    /// Signals that an item's properties changed. Re-evaluates filter and sort.
    /// </summary>
    /// <param name="item">The item that changed.</param>
    /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
    public void Refresh(T item)
    {
        Guard.IsNotNull(item);
        ProcessRefresh(item);
    }

    /// <summary>
    /// Signals that multiple items changed.
    /// </summary>
    /// <param name="items">The items that changed.</param>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    public void Refresh(IEnumerable<T> items)
    {
        Guard.IsNotNull(items);

        foreach (T item in items)
            ProcessRefresh(item);
    }

    /// <summary>
    /// Re-evaluates all items (useful after filter/sort change).
    /// </summary>
    public void RefreshAll()
    {
        RebuildView();
    }
    #endregion

    #region Filter Operations
    /// <summary>
    /// Applies a filter. Only matching items are visible.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <exception cref="ArgumentNullException">Thrown when predicate is null.</exception>
    public void Filter(Func<T, bool> predicate)
    {
        Guard.IsNotNull(predicate);

        lock (_lock)
        {
            _filterPredicate = predicate;
        }

        RebuildView();
    }

    /// <summary>
    /// Clears the filter. All items become visible.
    /// </summary>
    public void ClearFilter()
    {
        lock (_lock)
        {
            _filterPredicate = null;
        }

        RebuildView();
    }
    #endregion

    #region Sort Operations
    /// <summary>
    /// Sorts by the specified key.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The key selector function.</param>
    /// <param name="descending">Whether to sort in descending order.</param>
    /// <exception cref="ArgumentNullException">Thrown when keySelector is null.</exception>
    public void Sort<TKey>(Func<T, TKey> keySelector, bool descending = false)
    {
        Guard.IsNotNull(keySelector);

        lock (_lock)
        {
            _sortComparer = CreateComparer(x => keySelector(x)!, descending);
        }

        ApplySort();
    }

    /// <summary>
    /// Sorts using a custom comparer.
    /// </summary>
    /// <param name="comparer">The comparer to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when comparer is null.</exception>
    public void Sort(IComparer<T> comparer)
    {
        Guard.IsNotNull(comparer);

        lock (_lock)
        {
            _sortComparer = comparer;
        }

        ApplySort();
    }

    /// <summary>
    /// Clears sorting. Items appear in source order (filtered).
    /// </summary>
    public void ClearSort()
    {
        lock (_lock)
        {
            _sortComparer = null;
        }

        RebuildView();
    }
    #endregion

    #region IEnumerable Implementation
    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        lock (_lock)
            return _internalCollection.ToList().GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion

    #region IDisposable Implementation
    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        lock (_lock)
        {
            _sourceItems.Clear();
            _filteredItems.Clear();
            _internalCollection.Clear();
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Rebuilds the view from source applying current filter and sort.
    /// </summary>
    private void RebuildView()
    {
        List<T> newViewItems;

        lock (_lock)
        {
            _filteredItems.Clear();

            // Apply filter
            IEnumerable<T> filtered = _filterPredicate != null
                ? _sourceItems.Where(_filterPredicate)
                : _sourceItems;

            // Apply sort
            IEnumerable<T> sorted = _sortComparer != null
                ? filtered.OrderBy(x => x, _sortComparer)
                : filtered;

            newViewItems = [.. sorted];

            foreach (T item in newViewItems)
                _filteredItems.Add(item);
        }

        // Use ReplaceRange for efficient batch update with single Reset notification
        DispatchToUI(() => _internalCollection.ReplaceRange(newViewItems));

        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(SourceCount));
        OnPropertyChanged(nameof(FilteredOutCount));
    }

    /// <summary>
    /// Applies sort using minimal move operations when possible.
    /// </summary>
    private void ApplySort()
    {
        if (_sortComparer == null)
        {
            RebuildView();
            return;
        }

        // Calculate moves using ChangeSet pattern
        ChangeSet<T> changes = [];
        List<T> sorted;

        lock (_lock)
        {
            if (_internalCollection.Count <= 1)
                return;

            sorted = [.. _internalCollection.OrderBy(x => x, _sortComparer)];

            // Calculate minimal moves
            List<T> working = [.. _internalCollection];

            for (int targetIndex = 0; targetIndex < sorted.Count; targetIndex++)
            {
                T item = sorted[targetIndex];
                int currentIndex = working.IndexOf(item);

                if (currentIndex != targetIndex)
                {
                    changes.Add(Change<T>.Move(item, targetIndex, currentIndex));
                    working.RemoveAt(currentIndex);
                    working.Insert(targetIndex, item);
                }
            }
        }

        // Apply moves on UI thread
        if (changes.HasChanges)
        {
            // If many moves (>33%), just replace (more efficient)
            if (changes.Moves > _internalCollection.Count / 3)
            {
                DispatchToUI(() => _internalCollection.ReplaceRange(sorted));
            }
            else
            {
                // Apply individual moves for smoother animation
                DispatchToUI(() =>
                {
                    foreach (Change<T> change in changes)
                    {
                        if (change.Reason == ChangeReason.Move)
                            _internalCollection.Move(change.PreviousIndex, change.CurrentIndex);
                    }
                });
            }
        }
    }

    /// <summary>
    /// Processes a refresh for a single item.
    /// </summary>
    private void ProcessRefresh(T item)
    {
        bool wasVisible;
        bool nowPasses;
        int oldIndex = -1;
        int newIndex = -1;
        ChangeReason? changeType = null;

        lock (_lock)
        {
            if (!_sourceItems.Contains(item))
                return;

            wasVisible = _filteredItems.Contains(item);
            nowPasses = PassesFilter(item);

            if (wasVisible && !nowPasses)
            {
                // Was visible, now filtered out → Remove
                changeType = ChangeReason.Remove;
                oldIndex = _internalCollection.IndexOf(item);
                _filteredItems.Remove(item);
            }
            else if (!wasVisible && nowPasses)
            {
                // Was hidden, now passes → Add
                changeType = ChangeReason.Add;
                _filteredItems.Add(item);
                newIndex = FindInsertIndex(item);
            }
            else if (wasVisible && nowPasses && _sortComparer != null)
            {
                // Still visible, check if needs repositioning
                int currentIndex = _internalCollection.IndexOf(item);
                int correctIndex = FindCorrectPosition(item, currentIndex);

                if (currentIndex != correctIndex)
                {
                    changeType = ChangeReason.Move;
                    oldIndex = currentIndex;
                    newIndex = correctIndex;
                }
            }
        }

        // Apply change on UI thread
        if (changeType == ChangeReason.Remove && oldIndex >= 0)
        {
            DispatchToUI(() =>
            {
                if (oldIndex < _internalCollection.Count)
                    _internalCollection.RemoveAt(oldIndex);
            });
        }
        else if (changeType == ChangeReason.Add && newIndex >= 0)
        {
            DispatchToUI(() => _internalCollection.Insert(newIndex, item));
        }
        else if (changeType == ChangeReason.Move && oldIndex >= 0 && newIndex >= 0)
        {
            DispatchToUI(() => _internalCollection.Move(oldIndex, newIndex));
        }
    }

    /// <summary>
    /// Checks if an item passes the current filter.
    /// Must be called within lock.
    /// </summary>
    private bool PassesFilter(T item)
    {
        return _filterPredicate == null || _filterPredicate(item);
    }

    /// <summary>
    /// Finds the correct insert index for an item based on current sort.
    /// Must be called within lock.
    /// </summary>
    private int FindInsertIndex(T item)
    {
        if (_sortComparer == null || _internalCollection.Count == 0)
            return _internalCollection.Count;

        // Binary search for insert position
        int low = 0, high = _internalCollection.Count;
        while (low < high)
        {
            int mid = (low + high) / 2;
            if (_sortComparer.Compare(_internalCollection[mid], item) <= 0)
                low = mid + 1;
            else
                high = mid;
        }
        return low;
    }

    /// <summary>
    /// Finds the correct position for an item that may need to be moved.
    /// Must be called within lock.
    /// </summary>
    private int FindCorrectPosition(T item, int currentIndex)
    {
        if (_sortComparer == null)
            return currentIndex;

        for (int i = 0; i < _internalCollection.Count; i++)
        {
            if (i == currentIndex)
                continue;

            if (_sortComparer.Compare(item, _internalCollection[i]) < 0)
                return i > currentIndex ? i - 1 : i;
        }
        return _internalCollection.Count - 1;
    }

    /// <summary>
    /// Dispatches an action to the UI thread using the injected dispatcher.
    /// </summary>
    private void DispatchToUI(Action action)
    {
        if (_disposed)
            return;

        DispatcherHelper.Dispatch(_dispatcher, action);
    }
    #endregion

    #region Private Static Methods
    /// <summary>
    /// Creates a comparer from a key selector.
    /// </summary>
    private static Comparer<T> CreateComparer(Func<T, object> keySelector, bool descending)
    {
        return Comparer<T>.Create((a, b) =>
        {
            object? keyA = keySelector(a);
            object? keyB = keySelector(b);
            int result = Comparer<object>.Default.Compare(keyA, keyB);
            return descending ? -result : result;
        });
    }
    #endregion
}