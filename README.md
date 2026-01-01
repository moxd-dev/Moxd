# Moxd Development

<p align="center">
  <img src="./Images/Logo.png" alt="Moxd Logo" width="200"/>
</p>

<p align="center">
  <strong>High-quality .NET MAUI libraries for building exceptional cross-platform applications</strong>
</p>

<p align="center">
  <a href="#packages">Packages</a> •
  <a href="#getting-started">Getting Started</a> •
  <a href="#documentation">Documentation</a> •
  <a href="#contributing">Contributing</a> •
  <a href="#license">License</a>
</p>

---

## About Moxd Development

Moxd Development provides production-ready libraries, controls, and utilities for .NET MAUI developers. Our goal is to solve common pain points in cross-platform development with clean, performant, and well-documented solutions.

### What We Offer

- **Performance Utilities** — Thread-safe collections, async helpers, and optimized data handling
- **UI Controls** — Custom layouts, controls, and behaviors for MAUI applications
- **Platform Extensions** — Platform-specific functionality made easy
- **Best Practices** — Clean architecture patterns and reusable components

---

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| **Moxd.Maui.Core** | Essential utilities: reactive collections, threading primitives, guards, and extensions | [![NuGet](https://img.shields.io/nuget/v/Moxd.Maui.Core.svg)](https://www.nuget.org/packages/Moxd.Maui.Core) |

*More packages coming soon!*

---

## Getting Started

### Installation

```bash
dotnet add package Moxd.Maui.Core
```

### Quick Example

```csharp
using Moxd.Collections;

public class ProductsViewModel
{
    public ReactiveCollection<Product> Products { get; } = new();

    public async Task LoadAsync()
    {
        var items = await _service.GetProductsAsync();
        Products.Load(items);
    }

    public void Search(string text) => Products.Filter(p => p.Name.Contains(text));
    public void SortByPrice() => Products.Sort(p => p.Price);
}
```

---

## Documentation

Detailed documentation for each package:

- [**Moxd.Maui.Core**](docs/Core.md) — Collections, threading, guards, and extensions

---

## Requirements

- .NET 9.0 or later
- .NET MAUI workload

---

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<p align="center">
  Made with ❤️ by <a href="https://github.com/moxd-development">Moxd Development</a>
</p>