using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.MarketData.Contracts;

/// <summary>
/// Public interface of the MarketData module: current fundamentals of stocks, looked up by ticker symbol.
/// Other modules may depend only on the types in this project, never on the module's implementation.
/// </summary>
public interface IMarketDataModule
{
    /// <summary>
    /// Looks up a stock's current fundamentals. The symbol is case-insensitive and surrounding whitespace is ignored.
    /// Data is served from a cache shared by all users while it is fresh; if the provider fails, the last cached data
    /// is returned with <see cref="StockFundamentals.IsStale"/> set.
    /// </summary>
    Task<StockLookupResult> GetStockFundamentalsAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts an amount to another currency at the current exchange rate. Converting to the same currency returns
    /// the amount unchanged with rate 1. When no rate is available the result says so; no estimate is used.
    /// </summary>
    Task<CurrencyConversionResult> ConvertAsync(Money amount, string toCurrency, CancellationToken cancellationToken = default);
}
