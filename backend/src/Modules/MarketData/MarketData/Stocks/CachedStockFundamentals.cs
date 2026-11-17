using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.MarketData.Stocks;

/// <summary>
/// The last fundamentals fetched for a symbol, shared by all users (market data is not user-owned).
/// One row per symbol, overwritten on every successful fetch.
/// </summary>
internal sealed class CachedStockFundamentals
{
    public const int MaxCompanyNameLength = 200;
    public const int MaxSourceLength = 100;

    public required string Symbol { get; init; }

    public required string CompanyName { get; set; }

    public required string Currency { get; set; }

    public Money? Price { get; set; }

    public Money? MarketCap { get; set; }

    public decimal? TrailingPe { get; set; }

    public decimal? ForwardPe { get; set; }

    public decimal? EpsGrowth { get; set; }

    public decimal? InstitutionalOwnership { get; set; }

    public decimal? InsiderOwnership { get; set; }

    public decimal? DebtToEquity { get; set; }

    /// <summary>Display name of the provider the data came from.</summary>
    public required string Source { get; set; }

    /// <summary>When the data was valid, according to the provider.</summary>
    public DateTimeOffset AsOf { get; set; }

    /// <summary>When this server fetched the data; decides freshness.</summary>
    public DateTimeOffset FetchedAt { get; set; }
}
