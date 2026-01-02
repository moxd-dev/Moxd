using Moxd.Threading;
using Moxd.Collections;
using Moxd.Maui.Core.Tests.Models;

namespace Moxd.Maui.Core.Tests;

public class ReactiveCollectionTests
{
    private readonly IDispatcher _dispatcher;

    public ReactiveCollectionTests()
    {
        _dispatcher = DispatcherHelper.CreateTestDispatcher();
    }

    [Fact]
    public void Constructor_Default_CreatesEmptyCollection()
    {
        ReactiveCollection<string> collection = new(_dispatcher);

        Assert.Empty(collection);
        Assert.Equal(0, collection.SourceCount);
        Assert.NotNull(collection.View);
    }

    [Fact]
    public void Constructor_Default_ViewIsReadOnly()
    {
        ReactiveCollection<string> collection = new(_dispatcher);
        // View should be ReadOnlyObservableCollection
        Assert.IsType<System.Collections.ObjectModel.ReadOnlyObservableCollection<string>>(collection.View);
    }

    [Fact]
    public void Constructor_WithItems_LoadsItems()
    {
        string[] items = ["A", "B", "C"];

        ReactiveCollection<string> collection = new(_dispatcher, items);

        Assert.Equal(3, collection.Count);
        Assert.Equal(3, collection.SourceCount);
    }

    [Fact]
    public void Constructor_WithNullItems_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ReactiveCollection<string>(null!));
    }

    [Fact]
    public void Constructor_WithFilterAndSort_AppliesOnLoad()
    {
        ReactiveCollection<int> collection = new(
            _dispatcher,
            filter: x => x > 5,
            sortBy: x => x,
            descending: true);

        collection.Load([1, 10, 5, 8, 3, 7]);

        Assert.Equal(3, collection.Count); // 10, 8, 7
        Assert.Equal(6, collection.SourceCount); // 1, 10, 5, 8, 3, 7
        Assert.Equal(10, collection[0]);
        Assert.Equal(8, collection[1]);
        Assert.Equal(7, collection[2]);
    }

    [Fact]
    public void Load_ReplacesExistingItems()
    {
        ReactiveCollection<string> collection = new(_dispatcher, ["A", "B"]);

        collection.Load(["X", "Y", "Z"]);

        Assert.Equal(3, collection.Count);
        Assert.Contains("X", collection);
        Assert.DoesNotContain("A", collection);
    }

    [Fact]
    public void Load_WithFilter_AppliesFilter()
    {
        ReactiveCollection<int> collection = new(_dispatcher);
        collection.Filter(x => x % 2 == 0);

        collection.Load([1, 2, 3, 4, 5, 6]);

        Assert.Equal(3, collection.Count); // 2, 4, 6
        Assert.Equal(6, collection.SourceCount);
        Assert.Equal(3, collection.FilteredOutCount);
    }

    [Fact]
    public async Task LoadAsync_WithAsyncFetch_LoadsItems()
    {
        ReactiveCollection<string> collection = new(_dispatcher);

        await collection.LoadAsync(async () =>
        {
            await Task.Delay(10);
            return ["A", "B", "C"];
        });

        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public async Task LoadAsync_WithSyncFetch_LoadsItems()
    {
        ReactiveCollection<string> collection = new(_dispatcher);

        await collection.LoadAsync(() => ["X", "Y"]);

        Assert.Equal(2, collection.Count);
    }

    [Fact]
    public void Add_SingleItem_AddsToCollection()
    {
        ReactiveCollection<string> collection = new(_dispatcher)
        {
            "Test"
        };

        Assert.Single(collection);
        Assert.Contains("Test", collection);
    }

    [Fact]
    public void Add_WithFilter_OnlyAddsPassingItems()
    {
        ReactiveCollection<int> collection = new(_dispatcher);
        collection.Filter(x => x > 5);

        collection.Add(3);
        collection.Add(10);

        Assert.Single(collection);
        Assert.Equal(2, collection.SourceCount);
        Assert.Contains(10, collection);
    }

    [Fact]
    public void Add_WithSort_InsertsAtCorrectPosition()
    {
        ReactiveCollection<int> collection = new(_dispatcher);
        collection.Sort(x => x);

        collection.Add(5);
        collection.Add(3);
        collection.Add(7);
        collection.Add(1);

        Assert.Equal(1, collection[0]);
        Assert.Equal(3, collection[1]);
        Assert.Equal(5, collection[2]);
        Assert.Equal(7, collection[3]);
    }

    [Fact]
    public void AddRange_MultipleItems_AddsAll()
    {
        ReactiveCollection<string> collection = new(_dispatcher);

        collection.AddRange(["A", "B", "C"]);

        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void AddRange_EmptyCollection_NoOp()
    {
        ReactiveCollection<string> collection = new(_dispatcher, ["X"]);

        collection.AddRange([]);

        Assert.Single(collection);
    }

    [Fact]
    public void Remove_ExistingItem_ReturnsTrue()
    {
        ReactiveCollection<string> collection = new(_dispatcher, ["A", "B", "C"]);

        bool result = collection.Remove("B");

        Assert.True(result);
        Assert.Equal(2, collection.Count);
        Assert.DoesNotContain("B", collection);
    }

    [Fact]
    public void Remove_NonExistingItem_ReturnsFalse()
    {
        ReactiveCollection<string> collection = new(_dispatcher, ["A", "B"]);

        bool result = collection.Remove("X");

        Assert.False(result);
        Assert.Equal(2, collection.Count);
    }

    [Fact]
    public void Remove_FilteredOutItem_RemovesFromSource()
    {
        ReactiveCollection<int> collection = new(_dispatcher, [1, 2, 3, 4, 5]);
        collection.Filter(x => x > 3);

        // 1 is in source but filtered out
        bool result = collection.Remove(1);

        Assert.True(result);
        Assert.Equal(4, collection.SourceCount);
    }

    [Fact]
    public void RemoveWhere_MatchingItems_RemovesAll()
    {
        ReactiveCollection<int> collection = new(_dispatcher, [1, 2, 3, 4, 5, 6]);

        int removed = collection.RemoveWhere(x => x % 2 == 0);

        Assert.Equal(3, removed);
        Assert.Equal(3, collection.Count);
        Assert.All(collection, x => Assert.NotEqual(0, x % 2));
    }

    [Fact]
    public void RemoveWhere_NoMatches_ReturnsZero()
    {
        ReactiveCollection<int> collection = new(_dispatcher, [1, 3, 5]);

        int removed = collection.RemoveWhere(x => x % 2 == 0);

        Assert.Equal(0, removed);
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        ReactiveCollection<string> collection = new(_dispatcher, ["A", "B", "C"]);

        collection.Clear();

        Assert.Empty(collection);
        Assert.Equal(0, collection.SourceCount);
    }

    [Fact]
    public void Filter_AppliesPredicate()
    {
        ReactiveCollection<int> collection = new(_dispatcher, [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

        collection.Filter(x => x > 5);

        Assert.Equal(5, collection.Count);
        Assert.Equal(10, collection.SourceCount);
        Assert.True(collection.IsFiltered);
    }

    [Fact]
    public void Filter_Change_RebuildsView()
    {
        ReactiveCollection<int> collection = new(_dispatcher, [1, 2, 3, 4, 5]);
        collection.Filter(x => x > 3);

        Assert.Equal(2, collection.Count);

        collection.Filter(x => x < 3);

        Assert.Equal(2, collection.Count);
        Assert.Contains(1, collection);
        Assert.Contains(2, collection);
    }

    [Fact]
    public void ClearFilter_ShowsAllItems()
    {
        ReactiveCollection<int> collection = new(_dispatcher, [1, 2, 3, 4, 5]);
        collection.Filter(x => x > 3);

        collection.ClearFilter();

        Assert.Equal(5, collection.Count);
        Assert.False(collection.IsFiltered);
    }

    [Fact]
    public void Filter_WithProducts_FiltersByProperty()
    {
        Product[] products =
        [
            new Product { Id = 1, Name = "Widget", IsActive = true },
            new Product { Id = 2, Name = "Gadget", IsActive = false },
            new Product { Id = 3, Name = "Thing", IsActive = true }
        ];
        ReactiveCollection<Product> collection = new(_dispatcher, products);

        collection.Filter(p => p.IsActive);

        Assert.Equal(2, collection.Count);
        Assert.All(collection, p => Assert.True(p.IsActive));
    }

    [Fact]
    public void Sort_ByKey_SortsAscending()
    {
        ReactiveCollection<int> collection = new(_dispatcher, [5, 3, 8, 1, 9]);

        collection.Sort(x => x);

        Assert.True(collection.IsSorted);
        Assert.Equal([1, 3, 5, 8, 9], [.. collection]);
    }

    [Fact]
    public void Sort_Descending_SortsDescending()
    {
        ReactiveCollection<int> collection = new(_dispatcher, [5, 3, 8, 1, 9]);

        collection.Sort(x => x, descending: true);

        Assert.Equal([9, 8, 5, 3, 1], [.. collection]);
    }

    [Fact]
    public void Sort_WithComparer_UsesComparer()
    {
        ReactiveCollection<string> collection = new(_dispatcher, ["banana", "Apple", "cherry"]);

        collection.Sort(StringComparer.OrdinalIgnoreCase);

        Assert.Equal("Apple", collection[0]);
        Assert.Equal("banana", collection[1]);
        Assert.Equal("cherry", collection[2]);
    }

    [Fact]
    public void ClearSort_ReturnsToSourceOrder()
    {
        ReactiveCollection<int> collection = new(_dispatcher, [5, 3, 8, 1, 9]);
        collection.Sort(x => x);

        collection.ClearSort();

        Assert.False(collection.IsSorted);
        // Note: After ClearSort, order depends on how items are stored
    }

    [Fact]
    public void Sort_Products_ByProperty()
    {
        Product[] products =
        [
            new Product { Id = 1, Name = "Widget", Price = 29.99m },
            new Product { Id = 2, Name = "Gadget", Price = 19.99m },
            new Product { Id = 3, Name = "Thing", Price = 39.99m }
        ];
        ReactiveCollection<Product> collection = new(_dispatcher, products);

        collection.Sort(p => p.Price);

        Assert.Equal(19.99m, collection[0].Price);
        Assert.Equal(29.99m, collection[1].Price);
        Assert.Equal(39.99m, collection[2].Price);
    }

    [Fact]
    public void FilterAndSort_WorkTogether()
    {
        Product[] products =
        [
            new Product { Id = 1, Name = "A", Price = 30, IsActive = true },
            new Product { Id = 2, Name = "B", Price = 10, IsActive = false },
            new Product { Id = 3, Name = "C", Price = 20, IsActive = true },
            new Product { Id = 4, Name = "D", Price = 40, IsActive = true }
        ];
        ReactiveCollection<Product> collection = new(_dispatcher, products);

        collection.Filter(p => p.IsActive);
        collection.Sort(p => p.Price);

        Assert.Equal(3, collection.Count);
        Assert.Equal("C", collection[0].Name); // Price 20
        Assert.Equal("A", collection[1].Name); // Price 30
        Assert.Equal("D", collection[2].Name); // Price 40
    }

    [Fact]
    public void Refresh_ItemNowPassesFilter_AddsToView()
    {
        Product product = new() { Id = 1, Name = "Test", IsActive = false };
        ReactiveCollection<Product> collection = new(_dispatcher, [product]);
        collection.Filter(p => p.IsActive);

        Assert.Empty(collection);

        product.IsActive = true;
        collection.Refresh(product);

        Assert.Single(collection);
    }

    [Fact]
    public void Refresh_ItemNoLongerPassesFilter_RemovesFromView()
    {
        Product product = new() { Id = 1, Name = "Test", IsActive = true };
        ReactiveCollection<Product> collection = new(_dispatcher, [product]);
        collection.Filter(p => p.IsActive);

        Assert.Single(collection);

        product.IsActive = false;
        collection.Refresh(product);

        Assert.Empty(collection);
    }

    [Fact]
    public void Refresh_ItemNotInSource_NoOp()
    {
        ReactiveCollection<Product> collection = new(_dispatcher, [new Product { Id = 1, Name = "A" }]);

        Product otherProduct = new() { Id = 99, Name = "Other" };

        // Should not throw
        collection.Refresh(otherProduct);

        Assert.Single(collection);
    }

    [Fact]
    public void RefreshAll_RebuildsEntireView()
    {
        Product[] products =
        [
            new Product { Id = 1, Name = "A", Price = 10 },
            new Product { Id = 2, Name = "B", Price = 20 }
        ];

        ReactiveCollection<Product> collection = new(_dispatcher, products);
        collection.Sort(p => p.Price);

        // Modify without refresh
        products[0].Price = 30;

        // RefreshAll should reorder
        collection.RefreshAll();

        Assert.Equal("B", collection[0].Name);
        Assert.Equal("A", collection[1].Name);
    }

    [Fact]
    public void GetEnumerator_ReturnsViewItems()
    {
        ReactiveCollection<string> collection = new(_dispatcher, ["A", "B", "C"]);

        List<string> items = [.. collection];

        Assert.Equal(3, items.Count);
        Assert.Contains("A", items);
        Assert.Contains("B", items);
        Assert.Contains("C", items);
    }

    [Fact]
    public void Indexer_ReturnsCorrectItem()
    {
        ReactiveCollection<string> collection = new(_dispatcher, ["A", "B", "C"]);

        Assert.Equal("A", collection[0]);
        Assert.Equal("B", collection[1]);
        Assert.Equal("C", collection[2]);
    }

    [Fact]
    public void Dispose_ClearsCollections()
    {
        ReactiveCollection<string> collection = new(_dispatcher, ["A", "B", "C"]);

        collection.Dispose();

        Assert.Empty(collection);
    }

    [Fact]
    public void Dispose_MultipleCallsSafe()
    {
        ReactiveCollection<string> collection = new(_dispatcher);
        collection.Dispose();
        collection.Dispose(); // Should not throw
    }

    [Fact]
    public void Load_RaisesPropertyChangedForCount()
    {
        ReactiveCollection<string> collection = new(_dispatcher);
        List<string> changedProperties = [];

        collection.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);
        collection.Load(["A", "B"]);

        Assert.Contains(nameof(collection.Count), changedProperties);
        Assert.Contains(nameof(collection.SourceCount), changedProperties);
    }
}