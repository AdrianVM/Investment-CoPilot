using InvestmentCoPilot.MarketData.Contracts;
using InvestmentCoPilot.MarketData.ExchangeRates;
using InvestmentCoPilot.MarketData.ExchangeRates.Fake;
using InvestmentCoPilot.MarketData.Persistence;
using InvestmentCoPilot.MarketData.Providers;
using InvestmentCoPilot.MarketData.Providers.Fake;
using InvestmentCoPilot.MarketData.Stocks;
using InvestmentCoPilot.SharedKernel;
using InvestmentCoPilot.SharedKernel.Persistence;
using InvestmentCoPilot.SharedKernel.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace InvestmentCoPilot.MarketData;

public static class MarketDataModule
{
    /// <summary>Registers the MarketData module's services. Called once by the host.</summary>
    public static IServiceCollection AddMarketDataModule(this IServiceCollection services)
    {
        services.AddModuleDbContext<MarketDataDbContext>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddOptions<MarketDataOptions>().BindConfiguration(MarketDataOptions.SectionName).ValidateOnStart();
        services.AddSingleton<IValidateOptions<MarketDataOptions>, MarketDataOptionsValidator>();

        // Every available provider; MarketData:Provider selects one.
        services.AddSingleton<IMarketDataProvider, FakeMarketDataProvider>();

        // Every available exchange rate provider; MarketData:ExchangeRateProvider selects one.
        services.AddSingleton<IExchangeRateProvider, FakeExchangeRateProvider>();
        services.AddScoped<CurrencyConverter>();

        services.AddScoped<StockLookupService>();
        services.AddScoped<IMarketDataModule, MarketDataModuleFacade>();
        services.AddSingleton<IEndpointModule, MarketDataEndpoints>();
        return services;
    }
}

internal sealed class MarketDataModuleFacade(StockLookupService lookup, CurrencyConverter converter) : IMarketDataModule
{
    public Task<StockLookupResult> GetStockFundamentalsAsync(string symbol, CancellationToken cancellationToken = default) =>
        lookup.LookupAsync(symbol, cancellationToken);

    public Task<CurrencyConversionResult> ConvertAsync(Money amount, string toCurrency, CancellationToken cancellationToken = default) =>
        converter.ConvertAsync(amount, toCurrency, cancellationToken);
}
