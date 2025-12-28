using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moxd.Threading;

namespace Moxd;

/// <summary>
/// Extension methods for registering Moxd.Maui.Core services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Moxd.Maui.Core services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMoxdCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        // Register main thread service as singleton (stateless)
        services.TryAddSingleton<IMainThreadService, MainThreadService>();
        return services;
    }
}