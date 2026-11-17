using InvestmentCoPilot.MarketData.Contracts;
using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.UnitTests.Assessment;

/// <summary>An in-memory <see cref="IMarketDataModule"/>: stocks the test sets up, and a fixed EUR to USD rate.</summary>
internal sealed class FakeMarketData : IMarketDataModule
{
    public const decimal EurUsd = 1.085m;

    public Dictionary<string, StockLookupResult> Stocks { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Task<StockLookupResult> GetStockFundamentalsAsync(string symbol, CancellationToken cancellationToken = default) =>
        Task.FromResult(Stocks.TryGetValue(symbol, out var result) ? result : new StockLookupResult.NotFound());

    public Task<CurrencyConversionResult> ConvertAsync(Money amount, string toCurrency, CancellationToken cancellationToken = default) =>
        Task.FromResult<CurrencyConversionResult>(
            (amount.Currency, toCurrency) switch
            {
                ("EUR", "USD") => new CurrencyConversionResult.Converted(
                    new Money(amount.Amount * EurUsd, "USD"), EurUsd, "Test rates", Stock.AsOf),
                _ => new CurrencyConversionResult.RateUnavailable(amount.Currency, toCurrency),
            });
}

/// <summary>Builds fundamentals for tests; every metric defaults to a value that meets the Lynch criteria.</summary>
internal static class Stock
{
    public static readonly DateTimeOffset AsOf = new(2026, 11, 17, 9, 30, 0, TimeSpan.Zero);

    public static StockFundamentals Passing(string symbol = "TEST") => new(
        symbol, "Test Inc.", "USD", new Money(10m, "USD"), new Money(1_000_000_000m, "USD"),
        TrailingPe: 14m, ForwardPe: 11m, EpsGrowth: 0.2m, InstitutionalOwnership: 0.05m, InsiderOwnership: 0.3m,
        DebtToEquity: 0.1m, Source: "Test data", AsOf: AsOf, IsStale: false);
}
