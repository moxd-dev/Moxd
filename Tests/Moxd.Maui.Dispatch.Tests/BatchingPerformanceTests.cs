using Moxd.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Xunit.Abstractions;

namespace Moxd.Maui.Dispatch.Tests;

/// <summary>
/// Performance tests that demonstrate the real-world benefits of batching.
/// </summary>
public class BatchingPerformanceTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public void PerformanceShowcase_CompleteComparison()
    {
        LogSection("MOXD.MAUI.DISPATCH PERFORMANCE SHOWCASE");

        Output.WriteLine(@"
        This test demonstrates the dramatic performance improvement when using
        batched dispatching vs individual dispatches for UI updates.
        Scenario: Updating 1,000 items with 5 properties each = 5,000 updates");

        MockMainThreadService mockMainThread = new();
        DispatcherService dispatcher = new(mockMainThread);

        const int itemCount = 1000;
        const int propertiesPerItem = 5;
        int totalOperations = itemCount * propertiesPerItem;

        Output.WriteLine("  ┌─────────────────────────────────────────────────────────────┐");
        Output.WriteLine("  │  TEST 1: INDIVIDUAL DISPATCHES (Traditional Approach)      │");
        Output.WriteLine("  └─────────────────────────────────────────────────────────────┘");

        mockMainThread.Reset();
        List<SimpleItem> items1 = CreateItems(itemCount);

        Stopwatch sw1 = Stopwatch.StartNew();
        foreach (SimpleItem item in items1)
        {
            dispatcher.Dispatch(() => item.Prop1 = "A");
            dispatcher.Dispatch(() => item.Prop2 = "B");
            dispatcher.Dispatch(() => item.Prop3 = 100);
            dispatcher.Dispatch(() => item.Prop4 = true);
            dispatcher.Dispatch(() => item.Prop5 = DateTime.Now);
        }
        sw1.Stop();

        int dispatches1 = mockMainThread.DispatchCount;
        Output.WriteLine($"    Operations:  {totalOperations:N0}");
        Output.WriteLine($"    Dispatches:  {dispatches1:N0}");
        Output.WriteLine($"    Time:        {sw1.Elapsed.TotalMilliseconds:F2}ms");
        Output.WriteLine($"    Per item:    {sw1.Elapsed.TotalMicroseconds / itemCount:F2}μs");
        Output.WriteLine("");

        Output.WriteLine("  ┌─────────────────────────────────────────────────────────────┐");
        Output.WriteLine("  │  TEST 2: BATCHED DISPATCHES (Moxd.Maui.Dispatch)            │");
        Output.WriteLine("  └─────────────────────────────────────────────────────────────┘");

        mockMainThread.Reset();
        List<SimpleItem> items2 = CreateItems(itemCount);

        Stopwatch sw2 = Stopwatch.StartNew();
        using (dispatcher.Batch())
        {
            foreach (SimpleItem item in items2)
            {
                dispatcher.Dispatch(() => item.Prop1 = "A");
                dispatcher.Dispatch(() => item.Prop2 = "B");
                dispatcher.Dispatch(() => item.Prop3 = 100);
                dispatcher.Dispatch(() => item.Prop4 = true);
                dispatcher.Dispatch(() => item.Prop5 = DateTime.Now);
            }
        }
        sw2.Stop();

        int dispatches2 = mockMainThread.DispatchCount;
        Output.WriteLine($"    Operations:  {totalOperations:N0}");
        Output.WriteLine($"    Dispatches:  {dispatches2:N0}");
        Output.WriteLine($"    Time:        {sw2.Elapsed.TotalMilliseconds:F2}ms");
        Output.WriteLine($"    Per item:    {sw2.Elapsed.TotalMicroseconds / itemCount:F2}μs");
        Output.WriteLine("");

        Output.WriteLine("  ╔═════════════════════════════════════════════════════════════╗");
        Output.WriteLine("  ║                     RESULTS SUMMARY                         ║");
        Output.WriteLine("  ╠═════════════════════════════════════════════════════════════╣");

        double speedup = Math.Ceiling(sw1.Elapsed.TotalMilliseconds / sw2.Elapsed.TotalMilliseconds);
        double dispatchReduction = (double)dispatches1 / dispatches2;
        int dispatchesSaved = dispatches1 - dispatches2;

        Output.WriteLine($"  ║  SPEEDUP:              {speedup,8:F1}x faster              ║");
        Output.WriteLine($"  ║  DISPATCH REDUCTION:   {dispatchReduction,8:N0}x fewer               ║");
        Output.WriteLine($"  ║  UI CALLS SAVED:       {dispatchesSaved,8:N0}                   ║");
        Output.WriteLine("  ╚═════════════════════════════════════════════════════════════╝");
        Output.WriteLine("");

        // Assertions
        dispatches2.Should().Be(1, "batching should result in exactly 1 dispatch");
        speedup.Should().BeGreaterThan(1, "batching should be faster");

        LogSuccess($"Batching provides {speedup:F1}x speedup and {dispatchesSaved:N0} fewer UI thread calls!");
    }

    [Theory]
    [InlineData(100, 3)]
    [InlineData(500, 5)]
    [InlineData(1000, 5)]
    [InlineData(2000, 10)]
    public void PerformanceScaling_VariousDataSizes(int itemCount, int propertiesPerItem)
    {
        LogSection($"Scaling Test: {itemCount:N0} items × {propertiesPerItem} properties");

        MockMainThreadService mockMainThread = new MockMainThreadService();
        DispatcherService dispatcher = new DispatcherService(mockMainThread);
        int totalOps = itemCount * propertiesPerItem;

        // Without batching
        mockMainThread.Reset();
        Stopwatch sw1 = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
            dispatcher.Dispatch(() => { });
        sw1.Stop();
        int d1 = mockMainThread.DispatchCount;

        // With batching
        mockMainThread.Reset();
        Stopwatch sw2 = Stopwatch.StartNew();
        using (dispatcher.Batch())
        {
            for (int i = 0; i < totalOps; i++)
                dispatcher.Dispatch(() => { });
        }
        sw2.Stop();
        int d2 = mockMainThread.DispatchCount;

        double speedup = sw1.Elapsed.TotalMilliseconds / sw2.Elapsed.TotalMilliseconds;

        LogResult("Total operations", totalOps);
        LogResult("Without batch (dispatches)", d1);
        LogResult("With batch (dispatches)", d2);
        LogResult("Without batch (time)", $"{sw1.Elapsed.TotalMilliseconds:F2}ms");
        LogResult("With batch (time)", $"{sw2.Elapsed.TotalMilliseconds:F2}ms");
        LogResult("Speedup", $"{speedup:F1}x");

        d2.Should().Be(1);
        LogSuccess($"{totalOps:N0} operations: {speedup:F1}x faster with batching");
    }

    [Fact]
    public void PerformanceWithRealLatency_DemonstratesUIThreadSavings()
    {
        LogSection("Real-World Simulation: UI Thread Dispatch Latency");

        Output.WriteLine(@"
        In real MAUI apps, each MainThread.InvokeOnMainThread() call has overhead.
        This test simulates 50μs latency per dispatch to show real-world impact.");

        MockMainThreadService mockMainThread = new()
        {
            DispatchDelay = TimeSpan.FromMicroseconds(50) // Realistic overhead
        };
        DispatcherService dispatcher = new(mockMainThread);

        const int operations = 200;

        // Without batching
        mockMainThread.Reset();
        Stopwatch sw1 = Stopwatch.StartNew();
        for (int i = 0; i < operations; i++)
            dispatcher.Dispatch(() => { });
        sw1.Stop();

        // With batching
        mockMainThread.Reset();
        Stopwatch sw2 = Stopwatch.StartNew();
        using (dispatcher.Batch())
        {
            for (int i = 0; i < operations; i++)
                dispatcher.Dispatch(() => { });
        }
        sw2.Stop();

        TimeSpan timeSaved = sw1.Elapsed - sw2.Elapsed;
        double percentSaved = (timeSaved.TotalMilliseconds / sw1.Elapsed.TotalMilliseconds) * 100;

        Output.WriteLine($"Operations: {operations}");
        Output.WriteLine($"Dispatch latency: 50μs");
        Output.WriteLine("");
        Output.WriteLine($"Without batching: {sw1.Elapsed.TotalMilliseconds:F2}ms");
        Output.WriteLine($"With batching:    {sw2.Elapsed.TotalMilliseconds:F2}ms");
        Output.WriteLine($"Time saved:       {timeSaved.TotalMilliseconds:F2}ms ({percentSaved:F0}%)");
        Output.WriteLine("");

        LogSuccess($"Batching saves {timeSaved.TotalMilliseconds:F2}ms of UI thread time!");
    }

    [Fact]
    public void PerformanceComparison_ViewModelPropertyUpdates()
    {
        LogSection("Real-World Scenario: ViewModel Property Updates");

        MockMainThreadService mockMainThread = new();
        DispatcherService dispatcher = new(mockMainThread);

        const int itemCount = 500;
        const int propertiesPerItem = 5;
        List<TestViewModel> items = [.. Enumerable.Range(0, itemCount).Select(_ => new TestViewModel())];

        // Scenario 1: No batching (typical approach)
        mockMainThread.Reset();
        Stopwatch sw1 = Stopwatch.StartNew();

        foreach (TestViewModel item in items)
        {
            dispatcher.Dispatch(() => item.Name = "Updated");
            dispatcher.Dispatch(() => item.Description = "New Description");
            dispatcher.Dispatch(() => item.Value = 100);
            dispatcher.Dispatch(() => item.IsActive = true);
            dispatcher.Dispatch(() => item.Category = "Test");
        }

        sw1.Stop();
        int dispatchesWithoutBatch = mockMainThread.DispatchCount;

        // Reset for scenario 2
        items = [.. Enumerable.Range(0, itemCount).Select(_ => new TestViewModel())];
        mockMainThread.Reset();

        // Scenario 2: With batching
        Stopwatch sw2 = Stopwatch.StartNew();

        using (dispatcher.Batch())
        {
            foreach (TestViewModel item in items)
            {
                dispatcher.Dispatch(() => item.Name = "Updated");
                dispatcher.Dispatch(() => item.Description = "New Description");
                dispatcher.Dispatch(() => item.Value = 100);
                dispatcher.Dispatch(() => item.IsActive = true);
                dispatcher.Dispatch(() => item.Category = "Test");
            }
        }

        sw2.Stop();
        int dispatchesWithBatch = mockMainThread.DispatchCount;

        // Report
        Output.WriteLine($"Items: {itemCount:N0}");
        Output.WriteLine($"Properties per item: {propertiesPerItem}");
        Output.WriteLine($"Total property updates: {itemCount * propertiesPerItem:N0}\n");

        Output.WriteLine("WITHOUT Batching");
        LogPerformance("Time", sw1.Elapsed, itemCount * propertiesPerItem);
        LogResult("UI Thread Dispatches", $"{dispatchesWithoutBatch:N0}");

        Output.WriteLine("WITH Batching");
        LogPerformance("Time", sw2.Elapsed, itemCount * propertiesPerItem);
        LogResult("UI Thread Dispatches", $"{dispatchesWithBatch:N0}");

        Output.WriteLine("IMPROVEMENT");
        double speedup = sw1.Elapsed.TotalMilliseconds / sw2.Elapsed.TotalMilliseconds;
        double dispatchReduction = (double)dispatchesWithoutBatch / dispatchesWithBatch;

        LogResult("Speed improvement", $"{speedup:F1}x faster");
        LogResult("Dispatch reduction", $"{dispatchReduction:N0}x fewer dispatches");
        LogResult("UI thread calls saved", $"{dispatchesWithoutBatch - dispatchesWithBatch:N0}");

        // Assertions
        dispatchesWithBatch.Should().Be(1);
        speedup.Should().BeGreaterThan(1);

        Output.WriteLine("");
        LogSuccess($"Batching provides {speedup:F1}x speedup with {dispatchReduction:N0}x fewer UI dispatches!");
    }

    private static List<SimpleItem> CreateItems(int count) =>
        Enumerable.Range(0, count).Select(_ => new SimpleItem()).ToList();

    private class SimpleItem
    {
        public string Prop1 { get; set; } = "";
        public string Prop2 { get; set; } = "";
        public int Prop3 { get; set; }
        public bool Prop4 { get; set; }
        public DateTime Prop5 { get; set; }
    }
}

/// <summary>
/// Test view model with INotifyPropertyChanged for performance testing.
/// </summary>
public class TestViewModel : INotifyPropertyChanged
{
    private string _name = "";
    private string _description = "";
    private int _value;
    private bool _isActive;
    private string _category = "";

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public int Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }

    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }

    public string Category
    {
        get => _category;
        set { _category = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}