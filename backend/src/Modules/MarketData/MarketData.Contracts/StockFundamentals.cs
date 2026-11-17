using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.MarketData.Contracts;

/// <summary>
/// A stock's fundamentals at a point in time. Any metric may be <c>null</c> when the provider does not supply it;
/// a missing metric is never replaced by zero or an estimate.
/// Units: money is in the stock's listing currency (never converted); ratios are plain numbers;
/// percentages are fractions, where 0.15 means 15 %.
/// </summary>
/// <param name="Symbol">Ticker symbol, upper case, for example <c>LYNCH</c> or <c>SAP.DE</c>.</param>
/// <param name="CompanyName">Company name.</param>
/// <param name="Currency">ISO 4217 code of the listing currency.</param>
/// <param name="Price">Last price per share.</param>
/// <param name="MarketCap">Market capitalisation.</param>
/// <param name="TrailingPe">Price divided by the last twelve months' EPS. Null when earnings are zero or negative.</param>
/// <param name="ForwardPe">Price divided by the expected next twelve months' EPS. Null when expected earnings are zero or negative.</param>
/// <param name="EpsGrowth">Year-over-year growth of trailing-twelve-month EPS, as a fraction.</param>
/// <param name="InstitutionalOwnership">Share of outstanding shares held by institutions, as a fraction.</param>
/// <param name="InsiderOwnership">Share of outstanding shares held by insiders, as a fraction.</param>
/// <param name="DebtToEquity">Total debt divided by shareholders' equity.</param>
/// <param name="Source">Display name of the data source.</param>
/// <param name="AsOf">When the data was valid, according to the source.</param>
/// <param name="IsStale">True when the data could not be refreshed and is older than the configured maximum age.</param>
public sealed record StockFundamentals(
    string Symbol,
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
    string Source,
    DateTimeOffset AsOf,
    bool IsStale);
