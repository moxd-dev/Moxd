# Moxd Documentation

**High-performance .NET MAUI libraries for building production-grade applications.**

[![NuGet](https://img.shields.io/nuget/v/Moxd.Dispatch.svg)](https://www.nuget.org/packages/Moxd.Dispatch/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Build](https://github.com/moxd-dev/Moxd/actions/workflows/build.yml/badge.svg)](https://github.com/moxd-dev/Moxd/actions/workflows/build.yml)

---

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| [Moxd.Dispatch](dispatch/index.md) | High-performance dispatcher with batched UI updates | [![NuGet](https://img.shields.io/nuget/v/Moxd.Dispatch.svg)](https://www.nuget.org/packages/Moxd.Dispatch/) |
| Moxd.Core | Shared utilities | *Coming soon* |

---

## Quick Start

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
    .UseMoxdDispatch();
```

### Usage

```csharp
// Batch thousands of property changes into a single UI dispatch
using (dispatcher.Batch())
{
    foreach (var item in items)
    {
        item.Name = newName;
        item.Score = newScore;
    }
}
```

---

## Links

- [GitHub Repository](https://github.com/moxd-dev/Moxd)
- [NuGet Packages](https://www.nuget.org/profiles/moxd-dev)
- [Release Notes](https://github.com/moxd-dev/Moxd/releases)
- [API Reference](api/index.md)

---

## License

MIT License - see [LICENSE](https://github.com/moxd-dev/Moxd/blob/master/LICENSE) for details.