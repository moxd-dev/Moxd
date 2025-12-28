using System.Diagnostics;
using FluentAssertions;
using Moxd.Threading;
using Xunit.Abstractions;

namespace Moxd.Maui.Core.Tests;

public class AsyncLockTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task LockAsync_AcquiresAndReleasesLock()
    {
        // Arrange
        AsyncLock asyncLock = new();

        // Act
        using (await asyncLock.LockAsync())
        {
            asyncLock.IsLocked.Should().BeTrue();
        }

        // Assert
        asyncLock.IsLocked.Should().BeFalse();
        LogSuccess("Lock acquired and released correctly");
    }

    [Fact]
    public async Task LockAsync_EnforcesExclusiveAccess()
    {
        LogSection("AsyncLock Exclusive Access Test");

        // Arrange
        AsyncLock asyncLock = new();
        List<int> entryOrder = [];
        List<int> exitOrder = [];
        object lockObj = new();

        // Act - Start 3 concurrent tasks
        Task[] tasks = [.. Enumerable.Range(1, 3).Select(async i =>
        {
            using (await asyncLock.LockAsync())
            {
                lock (lockObj)
                    entryOrder.Add(i);
                LogInfo($"Task {i} entered critical section");

                await Task.Delay(50); // Simulate work

                lock (lockObj)
                    exitOrder.Add(i);
                LogInfo($"Task {i} exiting critical section");
            }
        })];

        await Task.WhenAll(tasks);

        // Assert - Entry and exit orders should match (no interleaving)
        entryOrder.Should().BeEquivalentTo(exitOrder, because: "tasks should execute sequentially, not concurrently");

        LogSuccess($"Entry order: [{string.Join(", ", entryOrder)}]");
        LogSuccess($"Exit order:  [{string.Join(", ", exitOrder)}]");
        LogSuccess("No interleaving detected - lock works correctly!");
    }

    [Fact]
    public async Task LockAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        AsyncLock asyncLock = new();
        using CancellationTokenSource cts = new();

        // Hold the lock
        IDisposable holder = await asyncLock.LockAsync();

        // Cancel immediately
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await asyncLock.LockAsync(cts.Token));

        holder.Dispose();
        LogSuccess("Cancellation handled correctly");
    }

    [Fact]
    public void TryLock_WhenAvailable_ReturnsTrue()
    {
        // Arrange
        AsyncLock asyncLock = new();

        // Act
        bool acquired = asyncLock.TryLock(out IDisposable? releaser);

        // Assert
        acquired.Should().BeTrue();
        releaser.Should().NotBeNull();
        asyncLock.IsLocked.Should().BeTrue();

        releaser!.Dispose();
        asyncLock.IsLocked.Should().BeFalse();
        LogSuccess("TryLock acquired and released correctly");
    }

    [Fact]
    public async Task TryLock_WhenHeld_ReturnsFalse()
    {
        // Arrange
        AsyncLock asyncLock = new();

        // Act
        using (await asyncLock.LockAsync())
        {
            bool acquired = asyncLock.TryLock(out IDisposable? releaser);
            // Assert
            acquired.Should().BeFalse();
            releaser.Should().BeNull();
        }

        LogSuccess("TryLock correctly returns false when lock is held");
    }

    [Fact]
    public async Task LockAsync_PerformanceTest_ManySequentialLocks()
    {
        LogSection("AsyncLock Performance Test");

        // Arrange
        AsyncLock asyncLock = new();
        const int iterations = 10_000;

        Stopwatch sw = Stopwatch.StartNew();
        // Act
        for (int i = 0; i < iterations; i++)
        {
            using (await asyncLock.LockAsync())
            {
                // Minimal work
            }
        }
        sw.Stop();

        // Assert & Report
        LogPerformance($"{iterations:N0} lock/unlock cycles", sw.Elapsed, iterations);

        LogResult("Average per operation", $"{sw.Elapsed.TotalMicroseconds / iterations:F2}μs");

        sw.Elapsed.TotalMilliseconds.Should().BeLessThan(1000, because: "10K lock operations should complete in under 1 second");

        LogSuccess("Performance is acceptable");
    }

    [Fact]
    public async Task Lock_Synchronous_Works()
    {
        // Arrange
        AsyncLock asyncLock = new();
        int counter = 0;

        // Act
        await Task.Run(() =>
        {
            using (asyncLock.Lock())
            {
                counter++;
            }
        });

        // Assert
        counter.Should().Be(1);
        asyncLock.IsLocked.Should().BeFalse();
        LogSuccess("Synchronous Lock works correctly");
    }

    [Fact]
    public void Dispose_ReleasesResources()
    {
        // Arrange
        AsyncLock asyncLock = new();

        // Act
        asyncLock.Dispose();

        // Assert - Further operations should throw
        Assert.Throws<ObjectDisposedException>(() => asyncLock.Lock());
        LogSuccess("Dispose releases resources correctly");
    }
}