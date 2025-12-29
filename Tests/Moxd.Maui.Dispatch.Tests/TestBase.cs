using Xunit.Abstractions;

namespace Moxd.Maui.Dispatch.Tests;

/// <summary>
/// Base class for tests that need output logging.
/// </summary>
public abstract class TestBase(ITestOutputHelper output)
{
    protected ITestOutputHelper Output { get; } = output;

    protected void LogSection(string title)
    {
        Output.WriteLine("");
        Output.WriteLine(new string('=', 60));
        Output.WriteLine($"  {title}");
        Output.WriteLine(new string('=', 60));
    }

    protected void LogResult(string label, object value)
    {
        Output.WriteLine($"  {label,-30}: {value}");
    }

    protected void LogPerformance(string operation, TimeSpan elapsed, int? itemCount = null)
    {
        string msg = $"  {operation,-30}: {elapsed.TotalMilliseconds:F2}ms";
        if (itemCount.HasValue)
        {
            double perItem = elapsed.TotalMilliseconds / itemCount.Value;
            msg += $" ({perItem:F4}ms/item)";
        }
        Output.WriteLine(msg);
    }

    protected void LogSuccess(string message)
    {
        Output.WriteLine($"  ✅ {message}");
    }

    protected void LogInfo(string message)
    {
        Output.WriteLine($"  ℹ️  {message}");
    }
}