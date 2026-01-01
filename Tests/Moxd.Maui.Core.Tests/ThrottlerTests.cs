using Moxd.Threading;
using System.Diagnostics;
using FluentAssertions;
using Xunit.Abstractions;

namespace Moxd.Maui.Core.Tests;

public class ThrottlerTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public void Throttle_ExecutesImmediatelyOnFirstCall()
    {
        // Arrange
        using Throttler throttler = new(TimeSpan.FromMilliseconds(100));
        bool executed = false;

        // Act
        throttler.Throttle(() => executed = true);

        // Assert
        executed.Should().BeTrue("first call should execute immediately");
        LogSuccess("First call executed immediately");
    }

    [Fact]
    public async Task Throttle_BlocksSubsequentCallsWithinInterval()
    {
        LogSection("Throttle Test - Blocks Rapid Calls");

        // Arrange
        using Throttler throttler = new Throttler(TimeSpan.FromMilliseconds(100));
        int executionCount = 0;

        // Act - Call rapidly 10 times
        for (int i = 0; i < 10; i++)
        {
            throttler.Throttle(() => executionCount++, executeTrailing: false);
        }

        // Assert - Only first call should execute
        executionCount.Should().Be(1);
        LogResult("Rapid calls made", 10);
        LogResult("Executions (immediate)", executionCount);

        // Wait for interval to pass
        await Task.Delay(150);

        // Now another call should work
        throttler.Throttle(() => executionCount++, executeTrailing: false);
        executionCount.Should().Be(2);

        LogSuccess("Throttler correctly blocked rapid calls!");
    }

    [Fact]
    public async Task Throttle_WithTrailing_ExecutesLastCall()
    {
        LogSection("Throttle Test - Trailing Execution");

        // Arrange
        using Throttler throttler = new(TimeSpan.FromMilliseconds(100));
        int lastValue = 0;
        int executionCount = 0;

        // Act - Call rapidly with different values
        for (int i = 1; i <= 5; i++)
        {
            int value = i;
            throttler.Throttle(() =>
            {
                lastValue = value;
                executionCount++;
            }, executeTrailing: true);

            await Task.Delay(20);
        }

        // Wait for trailing execution
        await Task.Delay(150);

        // Assert
        LogResult("Calls made", 5);
        LogResult("Executions", executionCount);
        LogResult("Last value", lastValue);

        // First call executes immediately (value=1), trailing executes last (value=5)
        executionCount.Should().BeGreaterOrEqualTo(1);
        lastValue.Should().Be(5, because: "trailing should execute the last call");

        LogSuccess("Trailing execution captured the last value!");
    }

    [Fact]
    public async Task ThrottleAsync_ExecutesWithinInterval()
    {
        // Arrange
        using Throttler throttler = new(TimeSpan.FromMilliseconds(100));
        bool executed = false;

        // Act
        await throttler.ThrottleAsync(async () =>
        {
            await Task.Delay(10);
            executed = true;
        });

        // Assert
        executed.Should().BeTrue();
        LogSuccess("ThrottleAsync executed correctly");
    }

    [Fact]
    public void Reset_AllowsImmediateExecution()
    {
        // Arrange
        using Throttler throttler = new(TimeSpan.FromMilliseconds(1000)); // Long interval
        int count = 0;

        // First call
        throttler.Throttle(() => count++, executeTrailing: false);
        count.Should().Be(1);

        // Second call should be blocked
        throttler.Throttle(() => count++, executeTrailing: false);
        count.Should().Be(1);

        // Reset
        throttler.Reset();

        // Now call should work immediately
        throttler.Throttle(() => count++, executeTrailing: false);
        count.Should().Be(2);

        LogSuccess("Reset allows immediate execution");
    }

    [Fact]
    public async Task Throttle_RealWorldScenario_ScrollHandler()
    {
        LogSection("Real-World Scenario: Scroll Event Throttling");

        // Arrange
        using Throttler throttler = new(TimeSpan.FromMilliseconds(50));
        int updateCount = 0;
        List<int> scrollPositions = [];

        void OnScroll(int position)
        {
            throttler.Throttle(() =>
            {
                updateCount++;
                scrollPositions.Add(position);
            }, executeTrailing: true);
        }

        // Act - Simulate 100 rapid scroll events
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            OnScroll(i * 10);
            await Task.Delay(5); // Very rapid events
        }
        // Wait for any trailing execution
        await Task.Delay(100);
        sw.Stop();

        // Assert
        LogResult("Scroll events fired", 100);
        LogResult("UI updates triggered", updateCount);
        LogResult("Update reduction", $"{(100.0 - updateCount) / 100:P0}");
        LogPerformance("Total time", sw.Elapsed);

        updateCount.Should().BeLessThan(30, because: "throttling should significantly reduce updates");
        scrollPositions.Should().Contain(990, because: "trailing should capture final position");

        LogSuccess($"Throttling reduced {100 - updateCount} unnecessary UI updates!");
    }

    [Fact]
    public void TimeUntilNextAllowed_ReturnsCorrectValue()
    {
        // Arrange
        using Throttler throttler = new(TimeSpan.FromMilliseconds(100));

        // Before any call
        throttler.TimeUntilNextAllowed.Should().Be(TimeSpan.Zero);

        // After first call
        throttler.Throttle(() => { }, executeTrailing: false);

        // Should have some time remaining
        TimeSpan remaining = throttler.TimeUntilNextAllowed;
        remaining.Should().BeGreaterThan(TimeSpan.Zero);
        remaining.Should().BeLessOrEqualTo(TimeSpan.FromMilliseconds(100));

        LogResult("Time until next allowed", $"{remaining.TotalMilliseconds:F0}ms");
        LogSuccess("TimeUntilNextAllowed works correctly");
    }

    [Fact]
    public void Cancel_ClearsPendingAction()
    {
        // Arrange
        using Throttler throttler = new(TimeSpan.FromMilliseconds(100));

        // First call executes immediately
        throttler.Throttle(() => { }, executeTrailing: true);

        // Second call queues trailing
        throttler.Throttle(() => { }, executeTrailing: true);
        throttler.HasPending.Should().BeTrue();

        // Cancel
        throttler.Cancel();
        throttler.HasPending.Should().BeFalse();

        LogSuccess("Cancel clears pending action");
    }

    [Fact]
    public void Dispose_DisposesCleanly()
    {
        // Arrange
        Throttler throttler = new(TimeSpan.FromMilliseconds(100));
        throttler.Throttle(() => { });

        // Act
        throttler.Dispose();

        // Assert - Should throw
        Assert.Throws<ObjectDisposedException>(() => throttler.Throttle(() => { }));

        LogSuccess("Dispose works correctly");
    }
}