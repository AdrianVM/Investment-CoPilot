using InvestmentCoPilot.MarketData.Contracts;
using InvestmentCoPilot.SharedKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvestmentCoPilot.MarketData.ExchangeRates;

/// <summary>Converts amounts with the configured exchange rate provider. A missing or failing rate is reported, never guessed.</summary>
internal sealed partial class CurrencyConverter(
    IEnumerable<IExchangeRateProvider> providers,
    IOptions<MarketDataOptions> options,
    TimeProvider clock,
    ILogger<CurrencyConverter> logger)
{
    private readonly IExchangeRateProvider provider = providers.Single(
        p => string.Equals(p.Name, options.Value.ExchangeRateProvider, StringComparison.OrdinalIgnoreCase));

    public async Task<CurrencyConversionResult> ConvertAsync(Money amount, string toCurrency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(toCurrency);

        if (amount.Currency == toCurrency)
        {
            return new CurrencyConversionResult.Converted(amount, 1m, provider.DisplayName, clock.GetUtcNow());
        }

        ExchangeRate? rate;
        try
        {
            rate = await provider.GetRateAsync(amount.Currency, toCurrency, cancellationToken);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            LogRateFailed(logger, ex, provider.Name, amount.Currency, toCurrency);
            rate = null;
        }

        return rate is null
            ? new CurrencyConversionResult.RateUnavailable(amount.Currency, toCurrency)
            : new CurrencyConversionResult.Converted(
                new Money(amount.Amount * rate.Rate, toCurrency), rate.Rate, provider.DisplayName, rate.AsOf);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exchange rate provider {Provider} failed for {From} to {To}")]
    private static partial void LogRateFailed(ILogger logger, Exception exception, string provider, string from, string to);
}
