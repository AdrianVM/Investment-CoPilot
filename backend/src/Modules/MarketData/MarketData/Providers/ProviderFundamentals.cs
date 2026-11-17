using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.MarketData.Providers;

/// <summary>
/// Fundamentals as returned by a provider. Same fields and units as <see cref="Contracts.StockFundamentals"/>,
/// without the source name (taken from the provider) and the stale marker (decided by the cache).
/// </summary>
internal sealed record ProviderFundamentals(
    string CompanyName,
    string Currency,
    Money? Price,
    Money? MarketCap,
    decimal? TrailingPe,
    decimal? ForwardPe,
    decimal? EpsGrowth,
    decimal? InstitutionalOwnership,
    decimal? InsiderOwnership,
    decimal? DebtToEquity,
    DateTimeOffset AsOf);
