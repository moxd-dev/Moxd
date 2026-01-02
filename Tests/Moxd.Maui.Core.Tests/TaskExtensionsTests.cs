using System.Diagnostics;

using FluentAssertions;

using Moxd.Extensions;
using Moxd.Threading;

using Xunit.Abstractions;

using TaskExtensions = Moxd.Extensions.TaskExtensions;

namespace Moxd.Maui.Core.Tests;

public class TaskExtensionsTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task SafeFireAndForget_Task_CompletesSuccessfully()
    {
        // Arrange
        bool completed = false;
        TaskCompletionSource tcs = new();

        // Act
        Task.Run(async () =>
        {
            await Task.Delay(50);
            completed = true;
            tcs.SetResult();
        }).SafeFireAndForget();

        await tcs.Task;

        // Assert
        completed.Should().BeTrue();
        LogSuccess("SafeFireAndForget completed successfully");
    }

    [Fact]
    public async Task SafeFireAndForget_Task_CatchesException()
    {
        // Arrange
        Exception? caughtException = null;
        TaskCompletionSource tcs = new();

        // Act
        Task.Run(async () =>
        {
            await Task.Delay(10);
            throw new InvalidOperationException("Test exception");
        }).SafeFireAndForget(onException: ex =>
        {
            caughtException = ex;
            tcs.SetResult();
        });

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<InvalidOperationException>();
        caughtException!.Message.Should().Be("Test exception");
        LogSuccess("Exception was caught and handled");
    }

    [Fact]
    public async Task SafeFireAndForget_Task_IgnoresCancellation()
    {
        // Arrange
        Exception? caughtException = null;
        using CancellationTokenSource cts = new();

        // Act
        Task.Run(async () => await Task.Delay(100, cts.Token), cts.Token)
            .SafeFireAndForget(onException: ex => caughtException = ex);

        // Cancel after a short delay
        await Task.Delay(20);
        cts.Cancel();
        await Task.Delay(100); // Wait for any exception handling

        // Assert - OperationCanceledException should NOT be passed to handler
        caughtException.Should().BeNull("cancellation exceptions should be ignored");
        LogSuccess("Cancellation was properly ignored");
    }

    [Fact]
    public async Task SafeFireAndForget_Task_WithoutHandler_DoesNotThrow()
    {
        // Arrange
        bool completed = false;
        TaskCompletionSource tcs = new();

        // Act - Exception with no handler should not crash
        Task.Run(() =>
        {
            completed = true;
            tcs.SetResult();
            throw new Exception("Unhandled");
        }).SafeFireAndForget(); // No onException handler

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50); // Give time for exception to be swallowed

        // Assert
        completed.Should().BeTrue();
        LogSuccess("Exception without handler was silently ignored");
    }

    [Fact]
    public async Task SafeFireAndForget_Task_WithContinueOnCapturedContext()
    {
        // Arrange
        bool completed = false;
        TaskCompletionSource tcs = new();

        // Act
        Task.Run(async () =>
        {
            await Task.Delay(50);
            completed = true;
            tcs.SetResult();
        }).SafeFireAndForget(continueOnCapturedContext: true);

        await tcs.Task;

        // Assert
        completed.Should().BeTrue();
        LogSuccess("SafeFireAndForget with continueOnCapturedContext works");
    }

    [Fact]
    public async Task SafeFireAndForget_ValueTask_CompletesSuccessfully()
    {
        // Arrange
        bool completed = false;
        TaskCompletionSource tcs = new();

        // Act
        async ValueTask DoWorkAsync()
        {
            await Task.Delay(50);
            completed = true;
            tcs.SetResult();
        }

        DoWorkAsync().SafeFireAndForget();

        await tcs.Task;

        // Assert
        completed.Should().BeTrue();
        LogSuccess("SafeFireAndForget (ValueTask) completed successfully");
    }

    [Fact]
    public async Task SafeFireAndForget_ValueTask_CatchesException()
    {
        // Arrange
        Exception? caughtException = null;
        TaskCompletionSource tcs = new();

        static async ValueTask ThrowAsync()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("ValueTask exception");
        }

        // Act
        ThrowAsync().SafeFireAndForget(onException: ex =>
        {
            caughtException = ex;
            tcs.SetResult();
        });

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<InvalidOperationException>();
        LogSuccess("ValueTask exception was caught");
    }

    [Fact]
    public async Task SafeFireAndForget_ValueTask_IgnoresCancellation()
    {
        // Arrange
        Exception? caughtException = null;
        using CancellationTokenSource cts = new();

        async ValueTask DelayAsync()
        {
            await Task.Delay(100, cts.Token);
        }

        // Act
        DelayAsync().SafeFireAndForget(onException: ex => caughtException = ex);

        await Task.Delay(20);
        cts.Cancel();
        await Task.Delay(100);

        // Assert
        caughtException.Should().BeNull("cancellation should be ignored");
        LogSuccess("ValueTask cancellation was properly ignored");
    }

    [Fact]
    public async Task SafeFireAndForget_Generic_CatchesSpecificException()
    {
        // Arrange
        InvalidOperationException? caughtException = null;
        TaskCompletionSource tcs = new();

        // Act
        Task.Run(() => throw new InvalidOperationException("Specific exception"))
            .SafeFireAndForget<InvalidOperationException>(onException: ex =>
        {
            caughtException = ex;
            tcs.SetResult();
        });

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        caughtException.Should().NotBeNull();
        caughtException!.Message.Should().Be("Specific exception");
        LogSuccess("Typed exception handler worked correctly");
    }

    [Fact]
    public async Task SafeFireAndForget_Generic_IgnoresNonMatchingExceptions()
    {
        // Arrange
        ArgumentException? caughtException = null;
        TaskCompletionSource taskCompleted = new();

        // Act - Throw InvalidOperationException but only handle ArgumentException
        Task.Run(async () =>
        {
            await Task.Delay(10);
            taskCompleted.SetResult();
            throw new InvalidOperationException("Wrong type");
        }).SafeFireAndForget<ArgumentException>(onException: ex => caughtException = ex);

        await taskCompleted.Task;
        await Task.Delay(100); // Wait for exception processing

        // Assert - Handler should NOT be called for wrong exception type
        caughtException.Should().BeNull("handler only catches ArgumentException, not InvalidOperationException");
        LogSuccess("Generic handler correctly ignores non-matching exception types");
    }

    [Fact]
    public async Task SafeFireAndForget_Generic_IgnoresCancellation()
    {
        // Arrange
        Exception? caughtException = null;
        using CancellationTokenSource cts = new();

        // Act
        Task.Run(async () => await Task.Delay(100, cts.Token), cts.Token)
            .SafeFireAndForget<InvalidOperationException>(onException: ex => caughtException = ex);

        await Task.Delay(20);
        cts.Cancel();
        await Task.Delay(100);

        // Assert
        caughtException.Should().BeNull("cancellation should be ignored even in generic version");
        LogSuccess("Generic SafeFireAndForget ignores cancellation");
    }

    [Fact]
    public async Task SafeFireAndForget_Performance_ManyTasks()
    {
        LogSection("SafeFireAndForget Performance Test");

        const int taskCount = 1000;
        int completedCount = 0;
        TaskCompletionSource tcs = new();

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < taskCount; i++)
        {
            Task.Run(async () =>
            {
                await Task.Delay(1);
                if (Interlocked.Increment(ref completedCount) == taskCount)
                {
                    tcs.SetResult();
                }
            }).SafeFireAndForget();
        }

        await tcs.Task;
        sw.Stop();

        LogPerformance($"Fired {taskCount} tasks", sw.Elapsed, taskCount);
        LogResult("All tasks completed", completedCount);

        completedCount.Should().Be(taskCount);
        LogSuccess($"All {taskCount} fire-and-forget tasks completed!");
    }

    [Fact]
    public async Task SafeFireAndForget_Performance_WithExceptionHandling()
    {
        LogSection("SafeFireAndForget with Exception Handling Performance");

        const int taskCount = 500;
        int exceptionCount = 0;
        int successCount = 0;
        TaskCompletionSource tcs = new();
        int totalProcessed = 0;

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < taskCount; i++)
        {
            bool shouldThrow = i % 2 == 0; // Half will throw

            Task.Run(async () =>
            {
                await Task.Delay(1);
                if (shouldThrow)
                {
                    throw new InvalidOperationException("Test");
                }
            }).SafeFireAndForget(onException: _ =>
            {
                Interlocked.Increment(ref exceptionCount);
                if (Interlocked.Increment(ref totalProcessed) == taskCount)
                    tcs.TrySetResult();
            });

            if (!shouldThrow)
            {
                // For successful tasks, we need another way to track completion
                Task.Run(async () =>
                {
                    await Task.Delay(50); // Wait for the task to complete
                    Interlocked.Increment(ref successCount);
                    if (Interlocked.Increment(ref totalProcessed) == taskCount)
                        tcs.TrySetResult();
                }).SafeFireAndForget();
            }
        }

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        sw.Stop();

        LogPerformance($"Processed {taskCount} tasks", sw.Elapsed, taskCount);
        LogResult("Exceptions caught", exceptionCount);
        LogResult("Successful tasks", successCount);

        exceptionCount.Should().BeGreaterThan(0);
        LogSuccess("Exception handling works correctly under load");
    }

    [Fact]
    public async Task RunAsync_WithAsyncBackgroundWork_ExecutesAndReturnsResult()
    {
        // Arrange
        int result = 0;
        IDispatcher dispatcher = DispatcherHelper.CreateTestDispatcher();

        // Act
        await TaskExtensions.RunAsync(dispatcher, async () =>
        {
            await Task.Delay(10);
            return 42;
        },
        value => result = value);

        // Assert
        result.Should().Be(42);
        LogSuccess("RunAsync with async background work completed");
    }

    [Fact]
    public async Task RunAsync_WithSyncBackgroundWork_ExecutesAndReturnsResult()
    {
        // Arrange
        string result = "";
        IDispatcher dispatcher = DispatcherHelper.CreateTestDispatcher();

        // Act
        await TaskExtensions.RunAsync(
            dispatcher,
            () => "Hello World",
            value => result = value);

        // Assert
        result.Should().Be("Hello World");
        LogSuccess("RunAsync with sync background work completed");
    }

    [Fact]
    public async Task RunAsync_VoidVersion_ExecutesBackgroundThenUI()
    {
        // Arrange
        bool backgroundExecuted = false;
        bool uiExecuted = false;
        IDispatcher dispatcher = DispatcherHelper.CreateTestDispatcher();

        // Act
        await TaskExtensions.RunAsync(
            dispatcher,
            async () =>
            {
                await Task.Delay(10);
                backgroundExecuted = true;
            },
            () => uiExecuted = true);

        // Assert
        backgroundExecuted.Should().BeTrue();
        uiExecuted.Should().BeTrue();
        LogSuccess("RunAsync void version completed both phases");
    }

    [Fact]
    public async Task RunAsync_SyncVoidVersion_ExecutesBackgroundThenUI()
    {
        // Arrange
        bool backgroundExecuted = false;
        bool uiExecuted = false;
        IDispatcher dispatcher = DispatcherHelper.CreateTestDispatcher();

        // Act
        await TaskExtensions.RunAsync(
            dispatcher,
            () => backgroundExecuted = true,
            () => uiExecuted = true);

        // Assert
        backgroundExecuted.Should().BeTrue();
        uiExecuted.Should().BeTrue();
        LogSuccess("RunAsync sync void version completed both phases");
    }

    [Fact]
    public async Task RunAsync_WithCancellation_ThrowsTaskCanceledException()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        cts.Cancel();
        IDispatcher dispatcher = DispatcherHelper.CreateTestDispatcher();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => 
            await TaskExtensions.RunAsync<int>(dispatcher, async () =>
            {
                await Task.Delay(100, cts.Token);
                return 42;
            }, _ => { },
        cts.Token));

        LogSuccess("RunAsync properly throws on cancellation");
    }

    [Fact]
    public async Task RunAsync_NullDispatcher_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await TaskExtensions.RunAsync<int>(null!, async () => 42, _ => { }));

        LogSuccess("RunAsync throws on null dispatcher");
    }

    [Fact]
    public async Task RunAsync_NullBackgroundWork_ThrowsArgumentNullException()
    {
        // Arrange
        IDispatcher dispatcher = DispatcherHelper.CreateTestDispatcher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await TaskExtensions.RunAsync(dispatcher, null!, () => { }));

        LogSuccess("RunAsync throws on null background work");
    }

    [Fact]
    public async Task RunAsync_NullUIWork_ThrowsArgumentNullException()
    {
        // Arrange
        IDispatcher dispatcher = DispatcherHelper.CreateTestDispatcher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await TaskExtensions.RunAsync(dispatcher, () => 42, null!));

        LogSuccess("RunAsync throws on null UI work");
    }
}