using InvestmentCoPilot.MarketData.Contracts;
using InvestmentCoPilot.MarketData.Persistence;
using InvestmentCoPilot.SharedKernel.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentCoPilot.MarketData;

public static class MarketDataModule
{
    /// <summary>Registers the MarketData module's services. Called once by the host.</summary>
    public static IServiceCollection AddMarketDataModule(this IServiceCollection services)
    {
        services.AddModuleDbContext<MarketDataDbContext>();
        services.AddScoped<IMarketDataModule, MarketDataModuleFacade>();
        return services;
    }
}

internal sealed class MarketDataModuleFacade : IMarketDataModule
{
}
