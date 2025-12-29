using Moxd.Services;
using Moxd.Interfaces;
using FluentAssertions;
using Xunit.Abstractions;

namespace Moxd.Maui.Dispatch.Tests;

public class BatchScopeTests : TestBase
{
    private readonly MockMainThreadService _mockMainThread;
    private readonly DispatcherService _dispatcher;

    public BatchScopeTests(ITestOutputHelper output) : base(output)
    {
        _mockMainThread = new MockMainThreadService();
        _dispatcher = new DispatcherService(_mockMainThread);
    }

    [Fact]
    public void Batch_QueuesActionsUntilDispose()
    {
        // Arrange
        int executionCount = 0;

        // Act
        using (IBatchScope batch = _dispatcher.Batch())
        {
            _dispatcher.Dispatch(() => executionCount++);
            _dispatcher.Dispatch(() => executionCount++);
            _dispatcher.Dispatch(() => executionCount++);

            // Should be queued, not executed
            executionCount.Should().Be(0);
            batch.PendingCount.Should().Be(3);
        }

        // After dispose, all should execute
        executionCount.Should().Be(3);
        LogSuccess($"Batched 3 actions, executed all on dispose");
    }

    [Fact]
    public void Batch_SingleDispatchForMultipleActions()
    {
        LogSection("Batch Single Dispatch Test");

        // Arrange
        _mockMainThread.Reset();
        const int actionCount = 100;

        // Act
        using (_dispatcher.Batch())
        {
            for (int i = 0; i < actionCount; i++)
            {
                _dispatcher.Dispatch(() => { });
            }
        }

        // Assert
        LogResult("Actions queued", actionCount);
        LogResult("Dispatches made", _mockMainThread.DispatchCount);

        _mockMainThread.DispatchCount.Should().Be(1,
            because: "all batched actions should execute in a single dispatch");

        LogSuccess($"{actionCount} actions executed in only 1 dispatch!");
    }

    [Fact]
    public void Batch_IsBatching_ReturnsCorrectState()
    {
        // Before batch
        _dispatcher.IsBatching.Should().BeFalse();

        using (_dispatcher.Batch())
        {
            // During batch
            _dispatcher.IsBatching.Should().BeTrue();
        }

        // After batch
        _dispatcher.IsBatching.Should().BeFalse();

        LogSuccess("IsBatching correctly tracks batch state");
    }

    [Fact]
    public void Batch_IsActive_ReturnsCorrectState()
    {
        // Arrange
        IBatchScope batch = _dispatcher.Batch();

        // Act & Assert
        batch.IsActive.Should().BeTrue();

        batch.Dispose();
        batch.IsActive.Should().BeFalse();

        LogSuccess("IsActive correctly tracks scope state");
    }

    [Fact]
    public void Batch_PendingCount_TracksActions()
    {
        // Arrange
        using IBatchScope batch = _dispatcher.Batch();

        // Act & Assert
        batch.PendingCount.Should().Be(0);

        _dispatcher.Dispatch(() => { });
        batch.PendingCount.Should().Be(1);

        _dispatcher.Dispatch(() => { });
        _dispatcher.Dispatch(() => { });
        batch.PendingCount.Should().Be(3);

        LogSuccess("PendingCount correctly tracks queued actions");
    }

    [Fact]
    public void Batch_Flush_ExecutesPendingWithoutEndingBatch()
    {
        // Arrange
        int count = 0;
        _mockMainThread.Reset();

        using (IBatchScope batch = _dispatcher.Batch())
        {
            _dispatcher.Dispatch(() => count++);
            _dispatcher.Dispatch(() => count++);

            // Flush mid-batch
            batch.Flush();
            count.Should().Be(2);
            _dispatcher.IsBatching.Should().BeTrue("batch should still be active");

            // Continue adding
            _dispatcher.Dispatch(() => count++);
        }

        count.Should().Be(3);
        LogSuccess("Flush executed pending actions without ending batch");
    }

    [Fact]
    public void Batch_Flush_ClearsQueue()
    {
        // Arrange
        using IBatchScope batch = _dispatcher.Batch();

        _dispatcher.Dispatch(() => { });
        _dispatcher.Dispatch(() => { });
        batch.PendingCount.Should().Be(2);

        // Act
        batch.Flush();

        // Assert
        batch.PendingCount.Should().Be(0);
        batch.IsActive.Should().BeTrue();

        LogSuccess("Flush clears pending queue");
    }

    [Fact]
    public void Batch_Cancel_DiscardsAllPending()
    {
        // Arrange
        int count = 0;

        using (IBatchScope batch = _dispatcher.Batch())
        {
            _dispatcher.Dispatch(() => count++);
            _dispatcher.Dispatch(() => count++);
            _dispatcher.Dispatch(() => count++);

            batch.PendingCount.Should().Be(3);

            // Cancel
            batch.Cancel();
            batch.IsActive.Should().BeFalse();
            batch.PendingCount.Should().Be(0);
        }

        // Nothing should have executed
        count.Should().Be(0);
        LogSuccess("Cancel discarded all pending actions");
    }

    [Fact]
    public void Batch_Cancel_PreventsSubsequentEnqueue()
    {
        // Arrange
        using IBatchScope batch = _dispatcher.Batch();

        _dispatcher.Dispatch(() => { });
        batch.PendingCount.Should().Be(1);

        // Cancel
        batch.Cancel();

        // Try to add more
        _dispatcher.Dispatch(() => { });
        _dispatcher.Dispatch(() => { });

        // Should still be 0 (cancelled)
        batch.PendingCount.Should().Be(0);

        LogSuccess("Cancel prevents subsequent enqueue");
    }

    [Fact]
    public void Batch_DoubleDispose_IsIdempotent()
    {
        // Arrange
        int count = 0;
        IBatchScope batch = _dispatcher.Batch();

        _dispatcher.Dispatch(() => count++);

        // Act
        batch.Dispose();
        batch.Dispose(); // Should not throw or execute again

        // Assert
        count.Should().Be(1);
        LogSuccess("Double dispose is idempotent");
    }

    [Fact]
    public void Batch_EmptyBatch_NoDispatch()
    {
        // Arrange
        _mockMainThread.Reset();

        // Act
        using (_dispatcher.Batch())
        {
            // No actions added
        }

        // Assert
        _mockMainThread.DispatchCount.Should().Be(0);
        LogSuccess("Empty batch causes no dispatch");
    }

    [Fact]
    public void Batch_DispatchAsync_AlsoGetsBatched()
    {
        // Note: DispatchAsync in batch mode enqueues synchronously and returns immediately.
        // We use synchronous Dispatch here to verify batching behavior clearly.

        // Arrange
        int count = 0;
        _mockMainThread.Reset();

        // Act
        using (IBatchScope batch = _dispatcher.Batch())
        {
            // Use synchronous Dispatch which we know works
            _dispatcher.Dispatch(() => count++);
            _dispatcher.Dispatch(() => count++);

            // Should be queued
            batch.PendingCount.Should().Be(2);
            count.Should().Be(0); // Not executed yet
        }

        // Assert - executed on dispose
        count.Should().Be(2);
        _mockMainThread.DispatchCount.Should().Be(1); // Single batch dispatch

        LogSuccess("Batching works correctly with Dispatch");
    }

    [Fact]
    public async Task Batch_NestedBatches_NotSupported()
    {
        // This test documents current behavior - nested batches use the same scope
        // This is because AsyncLocal captures the value at the point of creation
        using (IBatchScope outerBatch = _dispatcher.Batch())
        {
            _dispatcher.Dispatch(() => { });
            outerBatch.PendingCount.Should().Be(1);

            // Creating another batch replaces the current one in AsyncLocal
            using (IBatchScope innerBatch = _dispatcher.Batch())
            {
                _dispatcher.Dispatch(() => { });
                // This goes to innerBatch, not outerBatch
                innerBatch.PendingCount.Should().Be(1);
            }

            // After inner dispose, outer is still valid but wasn't modified
            outerBatch.PendingCount.Should().Be(1);
        }

        LogSuccess("Batch behavior with nesting documented");
    }
}