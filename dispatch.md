# Moxd.Dispatch

**High-performance dispatcher for .NET MAUI with batched UI updates, priority queuing, and background processing.**

[![NuGet](https://img.shields.io/nuget/v/Moxd.Dispatch.svg)](https://www.nuget.org/packages/Moxd.Dispatch/)

## The Problem

When updating large datasets in MAUI, each property change triggers a UI dispatch:

```csharp
// ❌ BAD: 1000 items × 20 properties = 20,000 UI dispatches!
foreach (var item in items)
{
    item.Name = name;       // Dispatch #1
    item.Status = status;   // Dispatch #2
    item.Score = score;     // Dispatch #3...
}
// Result: UI freezes for 2-5 seconds
```

## The Solution

```csharp
// ✅ GOOD: 20,000 property changes = 1 UI dispatch!
using (dispatcher.Batch())
{
    foreach (var item in items)
    {
        item.Name = name;       // Queued
        item.Status = status;   // Queued
        item.Score = score;     // Queued
    }
} // All changes dispatched HERE in one call
// Result: Instant, smooth UI
```

## Features

- **Batched Property Updates** - Thousands of changes → 1 dispatch
- **Background + UI Pattern** - Clean async workflow
- **Priority Dispatch** - Critical updates first
- **Debounce & Throttle** - Control update frequency
- **Progress Reporting** - Track long operations
- **Timing Metrics** - Know exactly how long operations take

## Installation

```bash
dotnet add package Moxd.Dispatch
```

## Quick Setup

```csharp
// MauiProgram.cs
builder.UseMoxdDispatch();
```

## API Overview

### Background + UI Dispatch

```csharp
var result = await _dispatcher.RunAsync(
    () => HeavyCalculation(),
    data => DisplayResults(data)
);

Console.WriteLine($"Background: {result.BackgroundTime}");
Console.WriteLine($"UI Dispatch: {result.DispatchTime}");
```

### Batched Updates

```csharp
using (_dispatcher.Batch())
{
    foreach (var item in items)
    {
        item.Name = "Updated";
        item.Score = 100;
    }
}
```

### Chunked Processing

```csharp
await _dispatcher.ProcessAsync(
    items,
    item => TransformItem(item),
    result => Collection.Add(result),
    BatchOptions.Balanced,
    progress
);
```

### Priority Dispatch

```csharp
await _dispatcher.ToUIAsync(
    () => ShowError(message),
    DispatchPriority.Critical
);
```

### Debounce & Throttle

```csharp
// Debounce: Wait for user to stop typing
var search = _dispatcher.Debounce<string>(
    query => SearchAsync(query),
    TimeSpan.FromMilliseconds(300)
);

// Throttle: Max once per 100ms
var update = _dispatcher.Throttle<Point>(
    pos => UpdatePosition(pos),
    TimeSpan.FromMilliseconds(100)
);
```

## Batch Options

| Preset | ChunkSize | ChunkDelay | Use Case |
|--------|-----------|------------|----------|
| `Fast` | 200 | 8ms | Maximum throughput |
| `Balanced` | 50 | 16ms | Default, good balance |
| `Smooth` | 20 | 32ms | Maximum UI responsiveness |

## Learn More

- [API Reference](../api/Moxd.Dispatch.html)