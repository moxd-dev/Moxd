# Moxd

[![Build](https://github.com/moxd-dev/Moxd/actions/workflows/build.yml/badge.svg)](https://github.com/moxd-dev/Moxd/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Moxd.Dispatch.svg)](https://www.nuget.org/packages/Moxd.Dispatch/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**High-performance .NET MAUI libraries for building production-grade mobile and desktop applications.**

---

## 📦 Packages

| Package | Description | NuGet | Downloads |
|---------|-------------|-------|-----------|
| [Moxd.Dispatch](Docs/dispatch/index.md) | High-performance dispatcher with batched UI updates | [![NuGet](https://img.shields.io/nuget/v/Moxd.Dispatch.svg)](https://www.nuget.org/packages/Moxd.Dispatch/) | [![Downloads](https://img.shields.io/nuget/dt/Moxd.Dispatch.svg)](https://www.nuget.org/packages/Moxd.Dispatch/) |
| Moxd.Core | Shared utilities and base classes | *Coming soon* | - |

---

## 🚀 Quick Start

### Installation

```bash
dotnet add package Moxd.Dispatch
```

### Setup

```csharp
// MauiProgram.cs
var builder = MauiApp.CreateBuilder();
builder
    .UseMauiApp<App>()
    .UseMoxdDispatch();  // Add this line
```

### Usage

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
        // Batch thousands of property changes into a single UI dispatch
        using (_dispatcher.Batch())
        {
            foreach (var item in items)
            {
                item.Name = newName;
                item.Score = newScore;
            }
        }
    }
}
```

---

## ⚡ Why Moxd.Dispatch?

### The Problem

When updating large datasets in MAUI, each property change triggers a UI dispatch:

```csharp
// ❌ BAD: 1000 items × 20 properties = 20,000 UI dispatches!
foreach (var item in items)
{
    item.Name = name;       // Dispatch #1
    item.Status = status;   // Dispatch #2
    item.Score = score;     // Dispatch #3...
}
// Result: UI freezes for 2-5 seconds 😱
```

### The Solution

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
} // All changes dispatched HERE in one call ⚡
// Result: Instant, smooth UI 🎉
```

### Performance Comparison

| Scenario | Without Moxd.Dispatch | With Moxd.Dispatch |
|----------|----------------------|-------------------|
| 1000 items × 20 properties | 20,000 dispatches (~2-5s freeze) | 1 dispatch (~0.5ms) |
| UI Responsiveness | ❌ Frozen | ✅ Smooth 60fps |
| User Experience | ❌ Laggy | ✅ Instant |

---

## ✨ Features

- **🔄 Batched Property Updates** - Thousands of changes → 1 dispatch
- **⚡ Background + UI Pattern** - Clean async workflow with timing metrics
- **🎯 Priority Dispatch** - Critical updates processed first
- **⏱️ Debounce & Throttle** - Control update frequency
- **📊 Progress Reporting** - Track long-running operations
- **📐 Timing Metrics** - Know exactly how long operations take

---

## 📖 Documentation

- [📘 Full Documentation](https://moxd-dev.github.io/Moxd/)
- [🚀 Getting Started](Docs/getting-started.md)
- [📦 Moxd.Dispatch Guide](Docs/dispatch/index.md)
- [📚 API Reference](https://moxd-dev.github.io/Moxd/api/)

---

## 🛠️ Supported Platforms

| Platform | Version |
|----------|---------|
| .NET | 8.0+ |
| Android | API 21+ |
| iOS | 14.0+ |
| macOS (Catalyst) | 14.0+ |
| Windows | 10.0.17763+ |

---

## 📋 Requirements

- .NET 8.0 SDK or later
- Visual Studio 2022/2025 with MAUI workload
- Or JetBrains Rider with MAUI support

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Built with ❤️ for the .NET MAUI community
- Inspired by best practices from [Nalu](https://github.com/nalu-development/nalu) and [CommunityToolkit.Maui](https://github.com/CommunityToolkit/Maui)

---

## 📞 Contact

- **GitHub**: [@moxd-dev](https://github.com/moxd-dev)
- **Issues**: [GitHub Issues](https://github.com/moxd-dev/Moxd/issues)

---

<p align="center">
  <sub>Built with ❤️ by <a href="https://github.com/moxd-dev">Moxd Development</a></sub>
</p>