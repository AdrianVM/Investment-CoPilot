using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.MarketData.Contracts;

/// <summary>The outcome of converting an amount between currencies. Exactly one of the nested types.</summary>
public abstract record CurrencyConversionResult
{
    private CurrencyConversionResult()
    {
    }

    /// <param name="Amount">The converted amount, in the target currency.</param>
    /// <param name="Rate">Units of the target currency per unit of the source currency.</param>
    /// <param name="Source">Display name of the exchange rate source.</param>
    /// <param name="AsOf">When the rate was valid, according to the source.</param>
    public sealed record Converted(Money Amount, decimal Rate, string Source, DateTimeOffset AsOf) : CurrencyConversionResult;

    /// <summary>No rate is available for this currency pair. No estimate is substituted.</summary>
    public sealed record RateUnavailable(string FromCurrency, string ToCurrency) : CurrencyConversionResult;
}
