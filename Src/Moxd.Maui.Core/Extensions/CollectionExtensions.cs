namespace Moxd.Extensions;

/// <summary>
/// Extension methods for collections and enumerables.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Returns an empty enumerable if the source is null.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <returns>The source enumerable, or empty if null.</returns>
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source)
        => source ?? [];

    /// <summary>
    /// Determines whether the collection is null or empty.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <returns>True if null or empty; otherwise, false.</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
        => source is null || !source.Any();

    /// <summary>
    /// Executes an action for each element in the enumerable.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="action">The action to execute.</param>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

        foreach (T item in source)
        {
            action(item);
        }
    }

    /// <summary>
    /// Executes an action for each element with its index.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="action">The action to execute with element and index.</param>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

        int index = 0;
        foreach (T item in source)
        {
            action(item, index++);
        }
    }

    /// <summary>
    /// Executes an async action for each element sequentially.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="action">The async action to execute.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static async Task ForEachAsync<T>(
        this IEnumerable<T> source,
        Func<T, Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

        foreach (T item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action(item).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Splits the collection into chunks of the specified size.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="chunkSize">The size of each chunk.</param>
    /// <returns>An enumerable of chunks.</returns>
    public static IEnumerable<IReadOnlyList<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSize);

        return source.Chunk(chunkSize).Select(chunk => (IReadOnlyList<T>)chunk);
    }

    /// <summary>
    /// Returns distinct elements by a key selector.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="keySelector">The key selector.</param>
    /// <returns>Distinct elements by key.</returns>
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        HashSet<TKey> seen = [];
        foreach (T item in source)
        {
            if (seen.Add(keySelector(item)))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Converts an enumerable to a HashSet.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <returns>A HashSet containing the elements.</returns>
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return [.. source];
    }

    /// <summary>
    /// Returns the index of the first element matching the predicate, or -1 if not found.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>The index, or -1 if not found.</returns>
    public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        int index = 0;
        foreach (T item in source)
        {
            if (predicate(item))
            {
                return index;
            }
            index++;
        }

        return -1;
    }

    /// <summary>
    /// Adds items from a source collection to a target list.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="list">The target list.</param>
    /// <param name="items">The items to add.</param>
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(items);

        if (list is List<T> concreteList)
        {
            concreteList.AddRange(items);
            return;
        }

        foreach (T item in items)
        {
            list.Add(item);
        }
    }

    /// <summary>
    /// Removes all items matching the predicate from the list.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="list">The list to modify.</param>
    /// <param name="predicate">The predicate to match items for removal.</param>
    /// <returns>The number of items removed.</returns>
    public static int RemoveWhere<T>(this IList<T> list, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(predicate);

        if (list is List<T> concreteList)
        {
            return concreteList.RemoveAll(new Predicate<T>(predicate));
        }

        List<T> itemsToRemove = [.. list.Where(predicate)];
        foreach (T item in itemsToRemove)
        {
            list.Remove(item);
        }

        return itemsToRemove.Count;
    }
}