using System.Collections.Frozen;

namespace InvestmentCoPilot.MarketData.ExchangeRates.Fake;

/// <summary>
/// Fixed sample rates for development and tests, for a handful of major currencies. Every pair is derived from US
/// dollars per unit, so reverse and cross rates are consistent. Any other currency has no rate.
/// </summary>
internal sealed class FakeExchangeRateProvider(TimeProvider clock) : IExchangeRateProvider
{
    public const string ProviderName = "Fake";

    /// <summary>Decimal places of every rate this provider returns.</summary>
    public const int RateDecimals = 6;

    private static readonly FrozenDictionary<string, decimal> UsdPerUnit = new Dictionary<string, decimal>
    {
        ["USD"] = 1m,
        ["EUR"] = 1.0850m,
        ["GBP"] = 1.2700m,
        ["CHF"] = 1.1300m,
        ["JPY"] = 0.006700m,
        ["CAD"] = 0.7300m,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public string Name => ProviderName;

    public string DisplayName => "Sample rates (fake provider)";

    /// <summary>The currencies this provider has rates for.</summary>
    public static IReadOnlyCollection<string> Currencies => UsdPerUnit.Keys;

    public Task<ExchangeRate?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        ExchangeRate? rate = UsdPerUnit.TryGetValue(fromCurrency, out var from) && UsdPerUnit.TryGetValue(toCurrency, out var to)
            ? new ExchangeRate(Math.Round(from / to, RateDecimals, MidpointRounding.ToEven), clock.GetUtcNow())
            : null;
        return Task.FromResult(rate);
    }
}
