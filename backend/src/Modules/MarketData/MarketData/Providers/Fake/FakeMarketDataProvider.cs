using System.Collections.Frozen;
using InvestmentCoPilot.MarketData.Stocks;
using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.MarketData.Providers.Fake;

/// <summary>
/// Deterministic sample data for development and tests: a fixed set of fictional companies that cover the cases the
/// assessment needs (meets every Peter Lynch criterion, fails several, missing metrics, losses, non-USD listing).
/// Any other symbol is unknown. No network access.
/// </summary>
internal sealed class FakeMarketDataProvider(TimeProvider clock) : IMarketDataProvider
{
    public const string ProviderName = "Fake";

    private static readonly FrozenDictionary<string, Func<DateTimeOffset, ProviderFundamentals>> Companies =
        new Dictionary<string, Func<DateTimeOffset, ProviderFundamentals>>
        {
            // Meets every criterion: small cap, low P/E, low institutional and high insider ownership, fast EPS growth, low debt.
            ["LYNCH"] = asOf => new(
                "Lynchpin Tools Inc. (sample)", "USD", Usd(42.50m), Usd(1_800_000_000m),
                TrailingPe: 14.2m, ForwardPe: 11.8m, EpsGrowth: 0.21m,
                InstitutionalOwnership: 0.07m, InsiderOwnership: 0.26m, DebtToEquity: 0.18m, asOf),

            // Fails every criterion: mega cap, expensive, owned by institutions, slow growth, heavily indebted.
            ["BIGCO"] = asOf => new(
                "Bigco Global Holdings (sample)", "USD", Usd(312.40m), Usd(410_000_000_000m),
                TrailingPe: 38.5m, ForwardPe: 29.1m, EpsGrowth: 0.06m,
                InstitutionalOwnership: 0.72m, InsiderOwnership: 0.01m, DebtToEquity: 1.45m, asOf),

            // Several metrics are not supplied.
            ["SPARSE"] = asOf => new(
                "Sparse Data Corp. (sample)", "USD", Usd(8.15m), Usd(420_000_000m),
                TrailingPe: 19.7m, ForwardPe: null, EpsGrowth: 0.12m,
                InstitutionalOwnership: null, InsiderOwnership: 0.31m, DebtToEquity: null, asOf),

            // Loss-making: P/E and EPS growth are not meaningful, so they are null rather than negative.
            ["LOSSCO"] = asOf => new(
                "Lossco Biotech (sample)", "USD", Usd(3.20m), Usd(150_000_000m),
                TrailingPe: null, ForwardPe: null, EpsGrowth: null,
                InstitutionalOwnership: 0.04m, InsiderOwnership: 0.35m, DebtToEquity: 0.10m, asOf),

            // Listed in euros; values close to the thresholds.
            ["EUROCO"] = asOf => new(
                "Euroco Maschinenbau AG (sample)", "EUR", Eur(56.80m), Eur(3_200_000_000m),
                TrailingPe: 24.6m, ForwardPe: 14.9m, EpsGrowth: 0.16m,
                InstitutionalOwnership: 0.09m, InsiderOwnership: 0.21m, DebtToEquity: 0.34m, asOf),
        }.ToFrozenDictionary(StringComparer.Ordinal);

    public string Name => ProviderName;

    public string DisplayName => "Sample data (fake provider)";

    /// <summary>The symbols this provider knows.</summary>
    public static IReadOnlyCollection<string> Symbols => Companies.Keys;

    public Task<ProviderFundamentals?> FetchAsync(TickerSymbol symbol, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        var fundamentals = Companies.TryGetValue(symbol.Value, out var create) ? create(clock.GetUtcNow()) : null;
        return Task.FromResult(fundamentals);
    }

    private static Money Usd(decimal amount) => new(amount, "USD");

    private static Money Eur(decimal amount) => new(amount, "EUR");
}
