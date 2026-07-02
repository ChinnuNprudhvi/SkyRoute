using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SkyRoute.Application.Interfaces;
using SkyRoute.Infrastructure.Pricing;
using SkyRoute.Infrastructure.Providers;
using SkyRoute.Infrastructure.Repositories;

namespace SkyRoute.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSkyRouteInfrastructure(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddSingleton<ISearchResultRepository, InMemorySearchResultRepository>();
        services.AddSingleton<IBookingRepository, InMemoryBookingRepository>();

        // Each provider owns its own IPricingStrategy instance, since pricing rules
        // are provider-specific rather than a single shared implementation.
        services.AddSingleton<IFlightProvider>(_ => new GlobalAirProvider(new GlobalAirPricingStrategy()));
        services.AddSingleton<IFlightProvider>(_ => new BudgetWingsProvider(new BudgetWingsPricingStrategy()));

        return services;
    }
}
