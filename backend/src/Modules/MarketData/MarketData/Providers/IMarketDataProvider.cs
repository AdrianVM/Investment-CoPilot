using InvestmentCoPilot.MarketData.Stocks;

namespace InvestmentCoPilot.MarketData.Providers;

/// <summary>
/// An adapter to one market data source. Adding a data vendor means adding one implementation, registering it in
/// <see cref="MarketDataModule"/>, and selecting it with the <c>MarketData:Provider</c> setting.
/// </summary>
internal interface IMarketDataProvider
{
    /// <summary>Name used in the <c>MarketData:Provider</c> setting, for example <c>Fake</c>.</summary>
    string Name { get; }

    /// <summary>Name reported to users as the data source.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Fetches the current fundamentals, or returns <c>null</c> if the source does not know the symbol.
    /// Throws (for example <see cref="MarketDataProviderException"/>) when the source cannot answer.
    /// Metrics must follow the units and definitions documented on
    /// <see cref="Contracts.StockFundamentals"/>; a metric that cannot be supplied in that form must be <c>null</c>.
    /// </summary>
    Task<ProviderFundamentals?> FetchAsync(TickerSymbol symbol, CancellationToken cancellationToken);
}
