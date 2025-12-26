# Getting Started

This guide will help you get started with Moxd packages in your .NET MAUI application.

## Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022/2025 with MAUI workload
- A .NET MAUI project

## Installation

### Using .NET CLI

```bash
dotnet add package Moxd.Dispatch
```

### Using Package Manager Console

```powershell
Install-Package Moxd.Dispatch
```

### Using Visual Studio

1. Right-click on your project → **Manage NuGet Packages**
2. Search for `Moxd.Dispatch`
3. Click **Install**

## Configuration

Add Moxd services to your `MauiProgram.cs`:

```csharp
using Moxd.Dispatch;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMoxdDispatch();

        return builder.Build();
    }
}
```

## Your First Usage

Inject `IDispatcherService` into your ViewModel:

```csharp
public class MyViewModel
{
    private readonly IDispatcherService _dispatcher;

    public MyViewModel(IDispatcherService dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task LoadDataAsync()
    {
        // Run work on background thread, dispatch result to UI
        var result = await _dispatcher.RunAsync(
            () => FetchDataFromApi(),
            data => Users.AddRange(data)
        );
    }
}
```

## Next Steps

- [Moxd.Dispatch Full Guide](dispatch/index.md)
- [API Reference](api/index.md)