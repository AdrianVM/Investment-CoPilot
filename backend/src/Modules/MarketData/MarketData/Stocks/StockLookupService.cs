using InvestmentCoPilot.MarketData.Contracts;
using InvestmentCoPilot.MarketData.Persistence;
using InvestmentCoPilot.MarketData.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace InvestmentCoPilot.MarketData.Stocks;

/// <summary>
/// Looks up a stock's fundamentals: from the shared cache while fresh, otherwise from the configured provider,
/// falling back to the last cached data (marked stale) when the provider fails.
/// </summary>
internal sealed partial class StockLookupService(
    MarketDataDbContext db,
    IEnumerable<IMarketDataProvider> providers,
    IOptions<MarketDataOptions> options,
    TimeProvider clock,
    ILogger<StockLookupService> logger)
{
    private readonly IMarketDataProvider provider = providers.Single(
        p => string.Equals(p.Name, options.Value.Provider, StringComparison.OrdinalIgnoreCase));

    public async Task<StockLookupResult> LookupAsync(string symbolInput, CancellationToken cancellationToken)
    {
        if (!TickerSymbol.TryParse(symbolInput, out var symbol))
        {
            return new StockLookupResult.InvalidSymbol(
                $"A ticker symbol has 1 to {TickerSymbol.MaxLength} letters, digits, '.' or '-', and starts with a letter or digit.");
        }

        var cached = await db.StockFundamentals.SingleOrDefaultAsync(s => s.Symbol == symbol.Value, cancellationToken);
        if (cached is not null && IsFresh(cached))
        {
            return Found(cached, isStale: false);
        }

        ProviderFundamentals? fetched;
        try
        {
            fetched = await provider.FetchAsync(symbol, cancellationToken);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            LogProviderFailed(logger, ex, provider.Name, symbol.Value, cached is not null);
            return cached is null ? new StockLookupResult.Unavailable() : Found(cached, isStale: true);
        }

        if (fetched is null)
        {
            return new StockLookupResult.NotFound();
        }

        var row = cached ?? new CachedStockFundamentals
        {
            Symbol = symbol.Value,
            CompanyName = fetched.CompanyName,
            Currency = fetched.Currency,
            Source = provider.DisplayName,
        };
        Apply(row, fetched);
        if (cached is null)
        {
            db.StockFundamentals.Add(row);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another request cached this symbol at the same moment. Its copy is equally fresh; ours is still valid to return.
            db.Entry(row).State = EntityState.Detached;
        }

        return Found(row, isStale: false);
    }

    /// <summary>
    /// Fresh while younger than the maximum age and from the configured provider, so that switching provider
    /// does not keep serving the old provider's data.
    /// </summary>
    private bool IsFresh(CachedStockFundamentals cached) =>
        clock.GetUtcNow() - cached.FetchedAt < options.Value.CacheMaxAge
        && cached.Source == provider.DisplayName;

    private void Apply(CachedStockFundamentals row, ProviderFundamentals fetched)
    {
        row.CompanyName = fetched.CompanyName;
        row.Currency = fetched.Currency;
        row.Price = fetched.Price;
        row.MarketCap = fetched.MarketCap;
        row.TrailingPe = fetched.TrailingPe;
        row.ForwardPe = fetched.ForwardPe;
        row.EpsGrowth = fetched.EpsGrowth;
        row.InstitutionalOwnership = fetched.InstitutionalOwnership;
        row.InsiderOwnership = fetched.InsiderOwnership;
        row.DebtToEquity = fetched.DebtToEquity;
        row.Source = provider.DisplayName;
        row.AsOf = ToStoredPrecision(fetched.AsOf);
        row.FetchedAt = ToStoredPrecision(clock.GetUtcNow());
    }

    /// <summary>
    /// Postgres stores timestamps to the microsecond. Truncating before saving makes the response to the request that
    /// fetched the data identical to every later response served from the cache.
    /// </summary>
    private static DateTimeOffset ToStoredPrecision(DateTimeOffset value) =>
        value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMicrosecond));

    private static StockLookupResult.Found Found(CachedStockFundamentals row, bool isStale) =>
        new(new StockFundamentals(
            row.Symbol,
            row.CompanyName,
            row.Currency,
            row.Price,
            row.MarketCap,
            row.TrailingPe,
            row.ForwardPe,
            row.EpsGrowth,
            row.InstitutionalOwnership,
            row.InsiderOwnership,
            row.DebtToEquity,
            row.Source,
            row.AsOf,
            isStale));

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Market data provider {Provider} failed for {Symbol}; serving cached data: {HasCachedData}")]
    private static partial void LogProviderFailed(ILogger logger, Exception exception, string provider, string symbol, bool hasCachedData);
}
