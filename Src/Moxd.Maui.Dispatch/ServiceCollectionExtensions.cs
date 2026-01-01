using Moxd.Services;
using Moxd.Interfaces;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Moxd;

/// <summary>
/// Extension methods for registering Moxd.Maui.Dispatch services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Moxd.Maui.Dispatch services to the service collection.
    /// Also registers Moxd.Maui.Core services if not already registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMoxdDispatch(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        // Add Core services (includes IMainThreadService)
        services.AddMoxdCore();
        // Register dispatcher service as singleton
        services.TryAddSingleton<IDispatcherService, DispatcherService>();
        return services;
    }
}