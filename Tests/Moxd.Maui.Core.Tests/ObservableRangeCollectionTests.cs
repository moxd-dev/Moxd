using Moxd.Collections;
using Moxd.Maui.Core.Tests.Models;
using System.Diagnostics;
using System.Collections.ObjectModel;
using FluentAssertions;
using Xunit.Abstractions;

namespace Moxd.Maui.Core.Tests;

public class ObservableRangeCollectionTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public void AddRange_AddsAllItems()
    {
        // Arrange
        ObservableRangeCollection<int> collection = [];
        List<int> items = [.. Enumerable.Range(1, 100)];

        // Act
        collection.AddRange(items);

        // Assert
        collection.Should().BeEquivalentTo(items);
        LogSuccess($"Added {items.Count} items successfully");
    }

    [Fact]
    public void AddRange_RaisesSingleNotification()
    {
        // Arrange
        ObservableRangeCollection<int> collection = [];
        int notificationCount = 0;
        collection.CollectionChanged += (s, e) => notificationCount++;

        // Act
        collection.AddRange([.. Enumerable.Range(1, 1000)]);

        // Assert
        notificationCount.Should().Be(1, because: "AddRange should raise exactly one notification");
        LogSuccess($"Added 1000 items with only {notificationCount} notification(s)");
    }

    [Fact]
    public void AddRange_PerformanceComparison_VsStandardObservableCollection()
    {
        LogSection("AddRange Performance: ObservableRangeCollection vs ObservableCollection");

        const int itemCount = 10_000;
        List<int> items = [.. Enumerable.Range(1, itemCount)];

        // Standard ObservableCollection
        ObservableCollection<int> standardCollection = [];
        int standardNotifications = 0;
        standardCollection.CollectionChanged += (s, e) => standardNotifications++;

        Stopwatch swStandard = Stopwatch.StartNew();
        foreach (int item in items)
        {
            standardCollection.Add(item);
        }
        swStandard.Stop();

        // ObservableRangeCollection
        ObservableRangeCollection<int> rangeCollection = [];
        int rangeNotifications = 0;
        rangeCollection.CollectionChanged += (s, e) => rangeNotifications++;

        Stopwatch swRange = Stopwatch.StartNew();
        rangeCollection.AddRange(items);
        swRange.Stop();

        // Report
        LogPerformance($"Standard Add ({itemCount:N0} items)", swStandard.Elapsed, itemCount);
        LogResult("  Notifications", $"{standardNotifications:N0}");

        LogPerformance($"AddRange ({itemCount:N0} items)", swRange.Elapsed, itemCount);
        LogResult("  Notifications", $"{rangeNotifications:N0}");

        double speedup = swStandard.Elapsed.TotalMilliseconds / swRange.Elapsed.TotalMilliseconds;
        LogResult("Speedup", $"{speedup:F1}x faster");
        LogResult("Notification reduction", $"{standardNotifications / rangeNotifications:N0}x fewer");

        // Assert
        rangeNotifications.Should().Be(1);
        swRange.Elapsed.Should().BeLessThan(swStandard.Elapsed);

        LogSuccess($"ObservableRangeCollection is {speedup:F1}x faster with {standardNotifications:N0}x fewer notifications!");
    }

    [Fact]
    public void RemoveRange_RemovesMatchingItems()
    {
        // Arrange
        ObservableRangeCollection<int> collection = new([.. Enumerable.Range(1, 100)]);

        // Act - Remove even numbers
        int removed = collection.RemoveRange(x => x % 2 == 0);

        // Assert
        removed.Should().Be(50);
        collection.Should().OnlyContain(x => x % 2 != 0);
        LogSuccess($"Removed {removed} even numbers, {collection.Count} odd numbers remain");
    }

    [Fact]
    public void RemoveRange_RaisesSingleNotification()
    {
        // Arrange
        ObservableRangeCollection<int> collection = new([.. Enumerable.Range(1, 1000)]);
        int notificationCount = 0;
        collection.CollectionChanged += (s, e) => notificationCount++;

        // Act - Remove even numbers
        collection.RemoveRange(x => x % 2 == 0);

        // Assert
        notificationCount.Should().Be(1);
        LogSuccess($"Removed 500 items with only {notificationCount} notification");
    }

    [Fact]
    public void ReplaceRange_ReplacesAllItems()
    {
        // Arrange
        ObservableRangeCollection<string> collection = new(["a", "b", "c"]);
        List<string> newItems = ["x", "y", "z", "w"];

        // Act
        collection.ReplaceRange(newItems);

        // Assert
        collection.Should().BeEquivalentTo(newItems);
        LogSuccess("ReplaceRange replaced all items correctly");
    }

    [Fact]
    public void ReplaceRange_PerformanceComparison()
    {
        LogSection("ReplaceRange Performance: Single op vs Clear+AddRange");

        const int itemCount = 10_000;
        List<int> initialItems = [.. Enumerable.Range(1, itemCount)];
        List<int> newItems = [.. Enumerable.Range(itemCount + 1, itemCount)];

        // Method 1: Clear + Add individually
        ObservableCollection<int> collection1 = new(initialItems);
        int notifications1 = 0;
        collection1.CollectionChanged += (s, e) => notifications1++;

        Stopwatch sw1 = Stopwatch.StartNew();
        collection1.Clear();
        foreach (int item in newItems)
        {
            collection1.Add(item);
        }
        sw1.Stop();

        // Method 2: ReplaceRange
        ObservableRangeCollection<int> collection2 = new(initialItems);
        int notifications2 = 0;
        collection2.CollectionChanged += (s, e) => notifications2++;

        Stopwatch sw2 = Stopwatch.StartNew();
        collection2.ReplaceRange(newItems);
        sw2.Stop();

        // Report
        LogPerformance("Clear + Add loop", sw1.Elapsed, itemCount);
        LogResult("Notifications", $"{notifications1:N0}");

        LogPerformance("ReplaceRange", sw2.Elapsed, itemCount);
        LogResult("Notifications", $"{notifications2:N0}");

        double speedup = sw1.Elapsed.TotalMilliseconds / sw2.Elapsed.TotalMilliseconds;
        LogResult("Speedup", $"{speedup:F1}x faster");

        // Assert
        notifications2.Should().Be(1);
        LogSuccess($"ReplaceRange is {speedup:F1}x faster with only 1 notification!");
    }

    [Fact]
    public void Sort_SortsItemsCorrectly()
    {
        // Arrange
        ObservableRangeCollection<int> collection = new([5, 3, 8, 1, 9, 2]);

        // Act
        collection.Sort();

        // Assert
        collection.Should().BeInAscendingOrder();
        LogSuccess("Sort orders items correctly");
    }

    [Fact]
    public void Sort_WithKeySelector_SortsCorrectly()
    {
        // Arrange
        ObservableRangeCollection<string> collection = new(["banana", "apple", "cherry"]);

        // Act
        collection.Sort(x => x);

        // Assert
        collection.Should().BeInAscendingOrder();
        LogSuccess("Sort with key selector works correctly");
    }

    [Fact]
    public void Sort_Descending_SortsCorrectly()
    {
        // Arrange
        ObservableRangeCollection<int> collection = new([5, 3, 8, 1, 9, 2]);

        // Act
        collection.Sort(x => x, descending: true);

        // Assert
        collection.Should().BeInDescendingOrder();
        LogSuccess("Descending sort works correctly");
    }

    [Fact]
    public void Sort_RaisesSingleNotification()
    {
        // Arrange
        ObservableRangeCollection<int> collection = new(Enumerable.Range(1, 1000).Reverse());
        int notificationCount = 0;
        collection.CollectionChanged += (s, e) => notificationCount++;

        // Act
        collection.Sort();

        // Assert
        notificationCount.Should().Be(1);
        LogSuccess($"Sorted 1000 items with only {notificationCount} notification");
    }

    [Fact]
    public void BatchUpdate_ExecutesActionsWithSingleNotification()
    {
        // Arrange
        ObservableRangeCollection<int> collection = new([.. Enumerable.Range(1, 10)]);
        int notificationCount = 0;
        collection.CollectionChanged += (s, e) => notificationCount++;

        // Act
        collection.BatchUpdate(items =>
        {
            items.Clear();
            items.Add(100);
            items.Add(200);
            items.Add(300);
        });

        // Assert
        notificationCount.Should().Be(1);
        collection.Should().BeEquivalentTo([100, 200, 300]);
        LogSuccess("BatchUpdate executed with single notification");
    }

    [Fact]
    public void InsertRange_InsertsAtCorrectPosition()
    {
        // Arrange
        ObservableRangeCollection<int> collection = new([1, 2, 5, 6]);

        // Act
        collection.InsertRange(2, [3, 4]);

        // Assert
        collection.Should().BeEquivalentTo([1, 2, 3, 4, 5, 6]);
        LogSuccess("InsertRange inserted items at correct position");
    }

    [Fact]
    public void InsertRange_RaisesSingleNotification()
    {
        // Arrange
        ObservableRangeCollection<int> collection = new([.. Enumerable.Range(1, 100)]);
        int notificationCount = 0;
        collection.CollectionChanged += (s, e) => notificationCount++;

        // Act
        collection.InsertRange(50, [.. Enumerable.Range(1000, 500)]);

        // Assert
        notificationCount.Should().Be(1);
        LogSuccess($"Inserted 500 items with only {notificationCount} notification");
    }

    [Fact]
    public void RealWorldScenario_UpdatingLargeList()
    {
        LogSection("Real-World Scenario: Updating Large List (simulating UI binding)");

        const int itemCount = 5_000;
        List<TestItem> initialData = [.. Enumerable.Range(1, itemCount).Select(i => new TestItem { Id = i, Name = $"Item {i}" })];

        List<TestItem> updatedData = [.. Enumerable.Range(1, itemCount).Select(i => new TestItem { Id = i, Name = $"Updated Item {i}" })];

        // Standard approach: Clear and re-add
        ObservableCollection<TestItem> standardCollection = new(initialData);
        int standardNotifications = 0;
        standardCollection.CollectionChanged += (s, e) => standardNotifications++;

        Stopwatch swStandard = Stopwatch.StartNew();
        standardCollection.Clear();
        foreach (TestItem item in updatedData)
        {
            standardCollection.Add(item);
        }
        swStandard.Stop();

        // Optimized approach: ReplaceRange
        ObservableRangeCollection<TestItem> optimizedCollection = new(initialData);
        int optimizedNotifications = 0;
        optimizedCollection.CollectionChanged += (s, e) => optimizedNotifications++;

        Stopwatch swOptimized = Stopwatch.StartNew();
        optimizedCollection.ReplaceRange(updatedData);
        swOptimized.Stop();

        // Report
        Output.WriteLine("");
        Output.WriteLine("Standard Approach (Clear + Add loop):");
        LogPerformance("Time", swStandard.Elapsed, itemCount);
        LogResult("Notifications", $"{standardNotifications:N0}");
        LogResult("UI Updates triggered", $"{standardNotifications:N0}");

        Output.WriteLine("");
        Output.WriteLine("Optimized Approach (ReplaceRange):");
        LogPerformance("Time", swOptimized.Elapsed, itemCount);
        LogResult("Notifications", $"{optimizedNotifications:N0}");
        LogResult("UI Updates triggered", $"{optimizedNotifications:N0}");

        Output.WriteLine("");
        double speedup = swStandard.Elapsed.TotalMilliseconds / swOptimized.Elapsed.TotalMilliseconds;
        double notificationReduction = (double)standardNotifications / optimizedNotifications;

        LogResult("PERFORMANCE GAIN", $"{speedup:F1}x faster");
        LogResult("NOTIFICATION REDUCTION", $"{notificationReduction:N0}x fewer");
        LogResult("UI THREAD SAVINGS", $"{standardNotifications - optimizedNotifications:N0} fewer UI updates");

        // Assert - focus on what really matters: notification reduction
        optimizedNotifications.Should().Be(1, "ReplaceRange should fire only one notification");
        standardNotifications.Should().BeGreaterThan(1000, "Standard approach fires many notifications");

        // The notification reduction is the key metric
        notificationReduction.Should().BeGreaterThan(100,
            "ObservableRangeCollection should reduce notifications by at least 100x");

        Output.WriteLine("");
        LogSuccess("ObservableRangeCollection dramatically reduces UI thread work!");
    }
}