using Moxd.Services;
using System.Diagnostics;
using FluentAssertions;
using Xunit.Abstractions;
using Moxd.Models;

namespace Moxd.Maui.Dispatch.Tests;

public class DispatcherServiceTests : TestBase
{
    private readonly MockMainThreadService _mockMainThread;
    private readonly DispatcherService _dispatcher;

    public DispatcherServiceTests(ITestOutputHelper output) : base(output)
    {
        _mockMainThread = new MockMainThreadService();
        _dispatcher = new DispatcherService(_mockMainThread);
    }

    [Fact]
    public void Dispatch_ExecutesAction()
    {
        // Arrange
        bool executed = false;

        // Act
        _dispatcher.Dispatch(() => executed = true);

        // Assert
        executed.Should().BeTrue();
        _mockMainThread.DispatchCount.Should().Be(1);
        LogSuccess("Dispatch executed action correctly");
    }

    [Fact]
    public async Task DispatchAsync_ExecutesAndAwaits()
    {
        // Arrange
        bool executed = false;

        // Act
        await _dispatcher.DispatchAsync(() => executed = true);

        // Assert
        executed.Should().BeTrue();
        _mockMainThread.DispatchCount.Should().Be(1);
        LogSuccess("DispatchAsync executed and awaited correctly");
    }

    [Fact]
    public async Task DispatchAsync_ReturnsValue()
    {
        // Act
        int result = await _dispatcher.DispatchAsync(() => 42);

        // Assert
        result.Should().Be(42);
        LogSuccess($"DispatchAsync returned: {result}");
    }

    [Fact]
    public void IsMainThread_ReturnsCorrectValue()
    {
        // Arrange
        _mockMainThread.SimulateIsMainThread = false;

        // Act & Assert
        _dispatcher.IsMainThread.Should().BeFalse();

        _mockMainThread.SimulateIsMainThread = true;
        _dispatcher.IsMainThread.Should().BeTrue();

        LogSuccess("IsMainThread correctly reflects mock state");
    }

    [Fact]
    public async Task RunAsync_Sync_ExecutesBackgroundThenUI()
    {
        LogSection("RunAsync: Background + UI Pattern Test");

        // Arrange
        bool backgroundExecuted = false;
        bool uiExecuted = false;
        List<string> executionOrder = [];

        // Act
        DispatchResult<int> result = await _dispatcher.RunAsync(() =>
        {
            executionOrder.Add("background");
            backgroundExecuted = true;
            Thread.Sleep(50); // Simulate work
            return 42;
        }, value =>
        {
            executionOrder.Add("ui");
            uiExecuted = true;
        });

        // Assert
        backgroundExecuted.Should().BeTrue();
        uiExecuted.Should().BeTrue();
        executionOrder.Should().Equal("background", "ui");
        result.Value.Should().Be(42);
        result.IsSuccess.Should().BeTrue();

        LogResult("Background executed", backgroundExecuted);
        LogResult("UI executed", uiExecuted);
        LogResult("Execution order", string.Join(" → ", executionOrder));
        LogResult("Result value", result.Value);
        LogPerformance("Background time", result.BackgroundTime);
        LogPerformance("Dispatch time", result.DispatchTime);
        LogPerformance("Total time", result.TotalTime);

        LogSuccess("Background + UI pattern executed correctly!");
    }

    [Fact]
    public async Task RunAsync_Async_ExecutesBackgroundThenUI()
    {
        // Arrange & Act
        DispatchResult<string> result = await _dispatcher.RunAsync<string>(async () =>
        {
            await Task.Delay(50);
            return "async result";
        },
        value => 
        {
            /* UI work */ 
        });

        // Assert
        result.Value.Should().Be("async result");
        result.IsSuccess.Should().BeTrue();
        LogSuccess($"Async background returned: {result.Value}");
    }

    [Fact]
    public async Task RunAsync_VoidVersion_ExecutesCorrectly()
    {
        // Arrange
        bool backgroundExecuted = false;
        bool uiExecuted = false;

        // Act
        DispatchResult result = await _dispatcher.RunAsync(
            () => backgroundExecuted = true,
            () => uiExecuted = true);

        // Assert
        backgroundExecuted.Should().BeTrue();
        uiExecuted.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        LogSuccess("Void RunAsync executed correctly");
    }

    [Fact]
    public async Task RunAsync_AsyncVoidVersion_ExecutesCorrectly()
    {
        // Arrange
        bool backgroundExecuted = false;
        bool uiExecuted = false;

        // Act
        DispatchResult result = await _dispatcher.RunAsync(async () =>
        {
            await Task.Delay(10);
            backgroundExecuted = true;
        },
        () => uiExecuted = true);

        // Assert
        backgroundExecuted.Should().BeTrue();
        uiExecuted.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        LogSuccess("Async void RunAsync executed correctly");
    }

    [Fact]
    public async Task RunAsync_CapturesBackgroundException()
    {
        // Act
        DispatchResult result = await _dispatcher.RunAsync(
            () => throw new InvalidOperationException("Background error"),
            () => { });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Exception.Should().BeOfType<InvalidOperationException>();
        result.Exception!.Message.Should().Be("Background error");
        LogSuccess($"Background exception captured: {result.Exception.Message}");
    }

    [Fact]
    public async Task RunAsync_CapturesUIException()
    {
        // Act
        DispatchResult<int> result = await _dispatcher.RunAsync(
            () => 42,
            value => throw new InvalidOperationException("UI error"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Exception.Should().BeOfType<InvalidOperationException>();
        result.Exception!.Message.Should().Be("UI error");
        LogSuccess($"UI exception captured: {result.Exception.Message}");
    }

    [Fact]
    public async Task RunAsync_ProvidesAccurateTimingMetrics()
    {
        LogSection("RunAsync Timing Metrics Test");

        // Arrange
        const int backgroundWorkMs = 100;
        const int uiWorkMs = 50;

        // Act
        DispatchResult<string> result = await _dispatcher.RunAsync(() =>
        {
            Thread.Sleep(backgroundWorkMs);
            return "data";
        },
        value => Thread.Sleep(uiWorkMs));

        // Assert
        LogPerformance("Expected background", TimeSpan.FromMilliseconds(backgroundWorkMs));
        LogPerformance("Actual background", result.BackgroundTime);
        LogPerformance("Expected UI", TimeSpan.FromMilliseconds(uiWorkMs));
        LogPerformance("Actual dispatch", result.DispatchTime);
        LogPerformance("Total", result.TotalTime);

        result.BackgroundTime.TotalMilliseconds.Should().BeGreaterOrEqualTo(backgroundWorkMs * 0.9);
        result.DispatchTime.TotalMilliseconds.Should().BeGreaterOrEqualTo(uiWorkMs * 0.9);

        LogSuccess("Timing metrics are accurate!");
    }

    [Fact]
    public async Task DispatchDelayedAsync_ExecutesAfterDelay()
    {
        // Arrange
        bool executed = false;
        Stopwatch sw = Stopwatch.StartNew();

        // Act
        await _dispatcher.DispatchDelayedAsync(
            () => executed = true,
            TimeSpan.FromMilliseconds(100));

        sw.Stop();

        // Assert
        executed.Should().BeTrue();
        sw.Elapsed.TotalMilliseconds.Should().BeGreaterOrEqualTo(90);
        LogPerformance("Delay executed after", sw.Elapsed);
        LogSuccess("Delayed dispatch works correctly");
    }

    [Fact]
    public async Task DispatchDelayedAsync_RespectsZeroDelay()
    {
        // Arrange
        bool executed = false;

        // Act
        await _dispatcher.DispatchDelayedAsync(
            () => executed = true,
            TimeSpan.Zero);

        // Assert
        executed.Should().BeTrue();
        LogSuccess("Zero delay executes immediately");
    }
}