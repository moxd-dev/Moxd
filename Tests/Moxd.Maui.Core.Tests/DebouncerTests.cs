using Moxd.Utilities;
using System.Diagnostics;
using FluentAssertions;
using Xunit.Abstractions;

namespace Moxd.Maui.Core.Tests;

public class DebouncerTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task Debounce_ExecutesAfterDelay()
    {
        // Arrange
        using Debouncer debouncer = new(TimeSpan.FromMilliseconds(100));
        bool executed = false;
        TaskCompletionSource tcs = new();

        // Act
        debouncer.Debounce(() =>
        {
            executed = true;
            tcs.SetResult();
        });

        // Should not execute immediately
        executed.Should().BeFalse();

        // Wait for debounce
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        executed.Should().BeTrue();
        LogSuccess("Debounced action executed after delay");
    }

    [Fact]
    public async Task Debounce_OnlyExecutesLastCall()
    {
        LogSection("Debounce Test - Only Last Call Executes");

        // Arrange
        using Debouncer debouncer = new(TimeSpan.FromMilliseconds(100));
        int executedValue = 0;
        int executionCount = 0;
        TaskCompletionSource tcs = new();

        // Act - Call multiple times rapidly
        for (int i = 1; i <= 10; i++)
        {
            int value = i;
            debouncer.Debounce(() =>
            {
                executedValue = value;
                executionCount++;
                tcs.TrySetResult();
            });
            await Task.Delay(20); // Less than debounce delay
        }

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50); // Extra time to ensure no more executions

        // Assert
        LogResult("Calls made", 10);
        LogResult("Executions", executionCount);
        LogResult("Final value", executedValue);

        executionCount.Should().Be(1, because: "only the last call should execute");
        executedValue.Should().Be(10, because: "the last value was 10");

        LogSuccess("Only the last debounced call was executed!");
    }

    [Fact]
    public async Task DebounceAsync_ReturnsResult()
    {
        // Arrange
        using Debouncer debouncer = new(TimeSpan.FromMilliseconds(50));

        // Act
        int result = await debouncer.DebounceAsync(async () =>
        {
            await Task.Delay(10);
            return 42;
        });

        // Assert
        result.Should().Be(42);
        LogSuccess($"DebounceAsync returned: {result}");
    }

    [Fact]
    public async Task Debounce_Cancel_PreventsExecution()
    {
        // Arrange
        using Debouncer debouncer = new(TimeSpan.FromMilliseconds(100));
        bool executed = false;

        // Act
        debouncer.Debounce(() => executed = true);
        debouncer.IsPending.Should().BeTrue();

        debouncer.Cancel();
        debouncer.IsPending.Should().BeFalse();

        await Task.Delay(150); // Wait past debounce time

        // Assert
        executed.Should().BeFalse();
        LogSuccess("Cancel prevented execution");
    }

    [Fact]
    public async Task Debounce_RealWorldScenario_SearchBox()
    {
        LogSection("Real-World Scenario: Search Box Debouncing");

        // Arrange
        using Debouncer debouncer = new(TimeSpan.FromMilliseconds(300));
        List<string> searchCalls = [];
        TaskCompletionSource tcs = new();

        void SimulateSearch(string query)
        {
            debouncer.Debounce(() =>
            {
                searchCalls.Add(query);
                LogInfo($"Search executed for: '{query}'");
                tcs.TrySetResult();
            });
        }

        // Act - Simulate typing "hello" character by character
        Stopwatch sw = Stopwatch.StartNew();

        SimulateSearch("h");
        await Task.Delay(50);
        SimulateSearch("he");
        await Task.Delay(50);
        SimulateSearch("hel");
        await Task.Delay(50);
        SimulateSearch("hell");
        await Task.Delay(50);
        SimulateSearch("hello");

        // Wait for final debounce
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        sw.Stop();

        // Assert
        LogResult("Keystrokes", 5);
        LogResult("Search API calls", searchCalls.Count);
        LogResult("Final search term", searchCalls.LastOrDefault() ?? "none");
        LogPerformance("Total time", sw.Elapsed);

        searchCalls.Should().HaveCount(1);
        searchCalls[0].Should().Be("hello");

        LogSuccess("Debouncing prevented 4 unnecessary API calls!");
    }

    [Fact]
    public void Dispose_DisposesCleanly()
    {
        // Arrange
        Debouncer debouncer = new(TimeSpan.FromMilliseconds(100));
        debouncer.Debounce(() => { });

        // Act
        debouncer.Dispose();

        // Assert - Should throw
        Assert.Throws<ObjectDisposedException>(() => debouncer.Debounce(() => { }));

        LogSuccess("Dispose works correctly");
    }
}