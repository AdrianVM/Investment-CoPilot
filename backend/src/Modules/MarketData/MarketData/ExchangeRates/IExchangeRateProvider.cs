namespace InvestmentCoPilot.MarketData.ExchangeRates;

/// <summary>
/// An adapter to one exchange rate source. Adding a vendor means adding one implementation, registering it in
/// <see cref="MarketDataModule"/>, and selecting it with the <c>MarketData:ExchangeRateProvider</c> setting.
/// </summary>
internal interface IExchangeRateProvider
{
    /// <summary>Name used in the <c>MarketData:ExchangeRateProvider</c> setting, for example <c>Fake</c>.</summary>
    string Name { get; }

    /// <summary>Name reported to users as the rate source.</summary>
    string DisplayName { get; }

    /// <summary>
    /// The rate from <paramref name="fromCurrency"/> to <paramref name="toCurrency"/> (units of <paramref name="toCurrency"/>
    /// per unit of <paramref name="fromCurrency"/>), or <c>null</c> if the source has none. Throws when the source cannot answer.
    /// </summary>
    Task<ExchangeRate?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken);
}

/// <param name="Rate">Units of the target currency per unit of the source currency.</param>
/// <param name="AsOf">When the rate was valid, according to the source.</param>
internal sealed record ExchangeRate(decimal Rate, DateTimeOffset AsOf);
