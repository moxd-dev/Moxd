# Moxd.Maui.Core

The foundational library for .NET MAUI applications providing high-performance collections, threading utilities, guards, and extensions.

## Installation

```bash
dotnet add package Moxd.Maui.Core
```

---

## Table of Contents

- [Collections](#collections)
  - [ReactiveCollection](#reactivecollection)
  - [ObservableRangeCollection](#observablerangecollection)
- [Threading](#threading)
  - [AsyncLock](#asynclock)
  - [Debouncer](#debouncer)
  - [Throttler](#throttler)
- [Guards](#guards)
- [Extensions](#extensions)
  - [TaskExtensions](#taskextensions)
  - [CollectionExtensions](#collectionextensions)

---

## Collections

### ReactiveCollection

A high-performance reactive collection with built-in filtering, sorting, and automatic UI thread marshaling. Inspired by DynamicData's ChangeSet pattern.

#### Basic Usage

```csharp
using Moxd.Collections;

public class ProductsViewModel : IDisposable
{
    public ReactiveCollection<Product> Products { get; } = new();

    public async Task LoadAsync()
    {
        var items = await _productService.GetAllAsync();
        Products.Load(items);
    }

    public void Dispose() => Products.Dispose();
}
```

**XAML Binding:**
```xml
<CollectionView ItemsSource="{Binding Products.View}" />
```

#### Filtering

```csharp
// Apply filter
Products.Filter(p => p.IsActive && p.Price > 10);

// Change filter
Products.Filter(p => p.Category == "Electronics");

// Clear filter (show all)
Products.ClearFilter();

// Check if filtered
if (Products.IsFiltered)
{
    Console.WriteLine($"{Products.FilteredOutCount} items hidden");
}
```

#### Sorting

```csharp
// Sort ascending
Products.Sort(p => p.Name);

// Sort descending
Products.Sort(p => p.Price, descending: true);

// Custom comparer
Products.Sort(StringComparer.OrdinalIgnoreCase);

// Clear sort
Products.ClearSort();
```

#### Combined Filter + Sort

```csharp
Products.Filter(p => p.IsActive);
Products.Sort(p => p.Price, descending: true);

// Items are now: active products, sorted by price (highest first)
```

#### CRUD Operations

```csharp
// Add
Products.Add(newProduct);
Products.AddRange(newProducts);

// Remove
Products.Remove(product);
Products.RemoveWhere(p => p.Stock == 0);

// Clear
Products.Clear();
```

#### Refresh (Property Changes)

When an item's properties change, call `Refresh` to re-evaluate filter/sort:

```csharp
product.IsActive = false;
Products.Refresh(product);  // Item may be filtered out or repositioned

// Refresh multiple
Products.Refresh(changedProducts);

// Refresh all
Products.RefreshAll();
```

#### Async Loading

```csharp
// Async with lambda
await Products.LoadAsync(async () => await _api.GetProductsAsync());

// Sync function on background thread
await Products.LoadAsync(() => _database.GetProducts());
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `View` | `ReadOnlyObservableCollection<T>` | The filtered/sorted collection. Bind UI to this. Read-only to ensure modifications go through ReactiveCollection methods. |
| `Count` | `int` | Number of visible items (after filter) |
| `SourceCount` | `int` | Total items (before filter) |
| `FilteredOutCount` | `int` | Items hidden by filter |
| `IsFiltered` | `bool` | Whether a filter is active |
| `IsSorted` | `bool` | Whether sorting is active |

---

### ObservableRangeCollection

An enhanced `ObservableCollection<T>` with batch operations that fire single notifications.

#### Why Use It?

Standard `ObservableCollection` fires a notification for each add/remove, causing UI performance issues. `ObservableRangeCollection` batches operations into single notifications.

```csharp
using Moxd.Collections;

var collection = new ObservableRangeCollection<string>();

// Add 1000 items with SINGLE notification
collection.AddRange(items);

// Replace all items with SINGLE Reset notification
collection.ReplaceRange(newItems);

// Remove multiple with SINGLE notification
collection.RemoveRange(itemsToRemove);
collection.RemoveRange(item => item.StartsWith("X"));
```

#### Batch Update

```csharp
collection.BatchUpdate(items =>
{
    items.Clear();
    items.Add("A");
    items.Add("B");
    items.Add("C");
});  // Single Reset notification
```

#### Sorting

```csharp
collection.Sort();  // Default comparer
collection.Sort(p => p.Name);  // By key
collection.Sort(p => p.Price, descending: true);
collection.Sort(customComparer);
```

---

## Threading

### AsyncLock

A lock that supports `await` inside the critical section. Use when you need to protect async operations.

```csharp
using Moxd.Threading;

private readonly AsyncLock _lock = new();

public async Task SaveDataAsync()
{
    using (await _lock.LockAsync())
    {
        // Safe to await here!
        await _database.SaveAsync(data);
        await _cache.InvalidateAsync();
    }
}
```

#### When to Use

| Scenario | Use |
|----------|-----|
| Protecting async I/O (database, HTTP, file) | `AsyncLock` |
| Protecting quick sync operations | `lock` statement |

```csharp
// DON'T do this - can cause deadlock
lock (_lock)
{
    await SomethingAsync();  // ❌ Error!
}

// DO this instead
using (await _asyncLock.LockAsync())
{
    await SomethingAsync();  // ✅ Safe
}
```

---

### Debouncer

Delays execution until a period of inactivity. Perfect for search boxes.

```csharp
using Moxd.Utilities;

private readonly Debouncer _searchDebouncer = new(TimeSpan.FromMilliseconds(300));

public string SearchText
{
    set => _searchDebouncer.Debounce(() => PerformSearch(value));
}

private void PerformSearch(string query)
{
    Products.Filter(p => p.Name.Contains(query));
}
```

#### Async Version

```csharp
await _debouncer.DebounceAsync(async () => 
{
    var results = await _api.SearchAsync(query);
    Products.Load(results);
});
```

#### With Result

```csharp
var result = await _debouncer.DebounceAsync(async () => 
{
    return await _api.SearchAsync(query);
});
```

---

### Throttler

Limits execution to at most once per time period. Good for scroll handlers, resize events.

```csharp
using Moxd.Utilities;

private readonly Throttler _scrollThrottler = new(TimeSpan.FromMilliseconds(100));

private void OnScroll(double position)
{
    _scrollThrottler.Throttle(() => UpdateScrollIndicator(position));
}
```

#### Leading vs Trailing Edge

```csharp
// Leading edge (default): Execute immediately, then wait
var throttler = new Throttler(TimeSpan.FromMilliseconds(100), leading: true);

// Trailing edge: Wait, then execute last call
var throttler = new Throttler(TimeSpan.FromMilliseconds(100), leading: false);
```

---

## Guards

Input validation with clean syntax and automatic parameter names.

```csharp
using Moxd.Guards;

public void ProcessOrder(Order order, int quantity, string notes)
{
    Guard.IsNotNull(order);
    Guard.IsPositive(quantity);
    Guard.IsNotNullOrWhiteSpace(notes);
    
    // Process...
}
```

#### Available Guards

| Method | Description |
|--------|-------------|
| `IsNotNull(value)` | Throws if null |
| `IsNotNullOrEmpty(string)` | Throws if null or empty |
| `IsNotNullOrWhiteSpace(string)` | Throws if null, empty, or whitespace |
| `IsNotNullOrEmpty(collection)` | Throws if collection is null or empty |
| `IsPositive(int)` | Throws if ≤ 0 |
| `IsNotNegative(int)` | Throws if < 0 |
| `IsInRange(value, min, max)` | Throws if outside range |
| `IsTrue(condition, message)` | Throws if false |
| `IsFalse(condition, message)` | Throws if true |

#### Fluent Return

Guards return the validated value for inline use:

```csharp
_name = Guard.IsNotNullOrWhiteSpace(name);
_items = Guard.IsNotNullOrEmpty(items);
```

---

## Extensions

### TaskExtensions

#### SafeFireAndForget

Fire async methods without await, with optional error handling:

```csharp
using Moxd.Extensions;

// Fire and forget
LoadDataAsync().SafeFireAndForget();

// With error handling
LoadDataAsync().SafeFireAndForget(
    onException: ex => Logger.LogError(ex));

// Typed exception handling
LoadDataAsync().SafeFireAndForget<HttpRequestException>(
    onException: ex => ShowNetworkError());
```

#### Timeout

```csharp
// With timeout
var result = await LongOperationAsync()
    .WithTimeoutAsync(TimeSpan.FromSeconds(30));

// With fallback on timeout
var result = await LongOperationAsync()
    .WithTimeoutOrDefaultAsync(
        TimeSpan.FromSeconds(5), 
        fallbackValue: "default");
```

#### Continuations

```csharp
// On success
await GetDataAsync().OnSuccessAsync(data => 
    Console.WriteLine($"Got {data.Count} items"));

// On error
await RiskyOperationAsync().OnErrorAsync(ex => 
    Logger.LogError(ex));
```

#### RunAsync (Background/UI Pattern)

Execute work on background thread, then handle result on UI thread:

```csharp
// Async background work
await TaskExtensions.RunAsync(
    async () => await _database.GetOrdersAsync(),
    orders => Orders.Load(orders));

// Sync background work
await TaskExtensions.RunAsync(
    () => ComputeExpensiveCalculation(),
    result => ResultLabel.Text = result.ToString());
```

---

### CollectionExtensions

LINQ-like helpers for collections.

```csharp
using Moxd.Extensions;

// Check if empty
if (items.IsNullOrEmpty())
    return;

// ForEach with index
items.ForEach((item, index) => 
    Console.WriteLine($"{index}: {item}"));

// Batch processing
foreach (var batch in items.Batch(100))
{
    await ProcessBatchAsync(batch);
}

// Distinct by property
var uniqueByName = products.DistinctBy(p => p.Name);
```

---

## Thread Safety

| Class | Thread Safe | Notes |
|-------|-------------|-------|
| `ReactiveCollection<T>` | ✅ Yes | All methods safe from any thread |
| `ObservableRangeCollection<T>` | ❌ No | Use from UI thread or with external locking |
| `AsyncLock` | ✅ Yes | Designed for concurrent access |
| `Debouncer` | ✅ Yes | Safe to call from multiple threads |
| `Throttler` | ✅ Yes | Safe to call from multiple threads |

---

## Best Practices

### 1. Use ReactiveCollection for ViewModels

```csharp
// ✅ Good
public ReactiveCollection<Product> Products { get; } = new();

// ❌ Avoid - manual thread marshaling needed
public ObservableCollection<Product> Products { get; } = new();
```

### 2. Load Data on Background Thread

```csharp
// ✅ Good
var items = await Task.Run(() => _database.GetAll());
Products.Load(items);

// ❌ Avoid - blocks UI thread
var items = _database.GetAll();  // On UI thread
Products.Load(items);
```

### 3. Debounce Search Input

```csharp
// ✅ Good - searches after 300ms of no typing
_debouncer.Debounce(() => Products.Filter(p => p.Name.Contains(text)));

// ❌ Avoid - searches on every keystroke
Products.Filter(p => p.Name.Contains(text));
```

### 4. Dispose When Done

```csharp
public class MyViewModel : IDisposable
{
    public ReactiveCollection<Item> Items { get; } = new();
    private readonly Debouncer _debouncer = new(TimeSpan.FromMilliseconds(300));

    public void Dispose()
    {
        Items.Dispose();
        _debouncer.Dispose();
    }
}
```